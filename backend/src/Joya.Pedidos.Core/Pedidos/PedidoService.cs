using Joya.Pedidos.Core.Clientes;
using Joya.Pedidos.Core.Common;
using Joya.Pedidos.Core.Estoque;
using Joya.Pedidos.Core.Produtos;

namespace Joya.Pedidos.Core.Pedidos;

public sealed class PedidoService(
    IClienteRepository clientes,
    IProdutoRepository produtos,
    IEstoqueRepository estoque,
    IPedidoRepository pedidos,
    IUnitOfWork unitOfWork,
    TimeProvider relogio) : IPedidoService
{
    public async Task<Result<PedidoResponse>> CriarAsync(CriarPedidoRequest request, CancellationToken ct)
    {
        // A validação estrutural (RN-01 a RN-04) já aconteceu no ValidationFilter.
        var itens = (request.Itens ?? [])
            .OrderBy(i => i.ProdutoId) // spec 4.3 item 3: ordem de lock determinística
            .ToList();

        var cliente = await clientes.ObterAsync(request.ClienteId, ct);
        if (cliente is null)
        {
            return PedidoErrors.ClienteNaoEncontrado(request.ClienteId); // RN-05
        }

        if (!cliente.Ativo)
        {
            return PedidoErrors.ClienteInativo(request.ClienteId); // RN-06
        }

        var ids = itens.Select(i => i.ProdutoId).ToList();
        var ativos = (await produtos.ObterAtivosAsync(ids, ct)).ToDictionary(p => p.Id);
        var invalidos = ids.Where(id => !ativos.ContainsKey(id)).ToList();
        if (invalidos.Count > 0)
        {
            return PedidoErrors.ProdutoNaoEncontrado(invalidos); // RN-07
        }

        var agora = relogio.GetUtcNow();
        var semEstoque = new List<CriarPedidoItemRequest>();

        var gravacao = await unitOfWork.ExecutarEmTransacaoAsync(
            token => GravarAsync(cliente, itens, ativos, agora, semEstoque, token),
            ct);

        if (gravacao.IsSuccess)
        {
            return gravacao;
        }

        // RN-08: o rollback já aconteceu. O saldo é lido só para a mensagem (spec 5.2).
        return semEstoque.Count > 0
            ? await EstoqueInsuficienteAsync(semEstoque, ativos, ct)
            : gravacao;
    }

    private static PedidoResponse Montar(
        int id, Pedido pedido, string clienteNome, IReadOnlyDictionary<int, ProdutoResumo> produtos) =>
        new(
            id,
            pedido.ClienteId,
            clienteNome,
            pedido.DataPedido,
            pedido.ValorTotal,
            [.. pedido.Itens.Select(i => new PedidoItemResponse(
                i.ProdutoId, produtos[i.ProdutoId].Nome, i.Quantidade, i.PrecoUnitario, i.Subtotal))]);

    private async Task<Result<PedidoResponse>> GravarAsync(
        ClienteResumo cliente,
        IReadOnlyList<CriarPedidoItemRequest> ordenados,
        IReadOnlyDictionary<int, ProdutoResumo> ativos,
        DateTimeOffset instante,
        List<CriarPedidoItemRequest> falhas,
        CancellationToken ct)
    {
        var itensPedido = new List<ItemPedido>(ordenados.Count);

        // spec 4.3 item 4: tenta todos antes de desfazer, para listar todos os itens sem estoque.
        foreach (var item in ordenados)
        {
            var preco = await estoque.TentarBaixarAsync(item.ProdutoId, item.Quantidade, ct);
            if (preco is null)
            {
                falhas.Add(item);
                continue;
            }

            itensPedido.Add(ItemPedido.Criar(item.ProdutoId, item.Quantidade, preco.Value)); // RN-09
        }

        if (falhas.Count > 0)
        {
            return PedidoErrors.EstoqueInsuficiente([]); // sinaliza o rollback; a lista real é montada depois dele
        }

        var pedido = Pedido.Criar(cliente.Id, instante, itensPedido); // RN-11, RN-13
        var adicao = await pedidos.AdicionarAsync(pedido, ct);

        return adicao.IsFailure
            ? adicao.Error
            : Montar(adicao.Value, pedido, cliente.Nome, ativos);
    }

    private async Task<Result<PedidoResponse>> EstoqueInsuficienteAsync(
        List<CriarPedidoItemRequest> falhas, IReadOnlyDictionary<int, ProdutoResumo> ativos, CancellationToken ct)
    {
        var saldos = await produtos.ObterEstoquesAsync([.. falhas.Select(f => f.ProdutoId)], ct);

        return PedidoErrors.EstoqueInsuficiente([.. falhas.Select(f => new ItemSemEstoque(
            f.ProdutoId,
            ativos[f.ProdutoId].Nome,
            f.Quantidade,
            saldos.GetValueOrDefault(f.ProdutoId)))]);
    }
}
