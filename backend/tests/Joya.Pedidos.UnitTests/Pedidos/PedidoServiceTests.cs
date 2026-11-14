using Joya.Pedidos.Core.Clientes;
using Joya.Pedidos.Core.Common;
using Joya.Pedidos.Core.Estoque;
using Joya.Pedidos.Core.Pedidos;
using Joya.Pedidos.Core.Produtos;
using Joya.Pedidos.UnitTests.Apoio;
using NSubstitute;
using Shouldly;

namespace Joya.Pedidos.UnitTests.Pedidos;

public sealed class PedidoServiceTests
{
    private static readonly DateTimeOffset _agora = new(2026, 11, 13, 14, 32, 7, TimeSpan.Zero);

    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IProdutoRepository _produtos = Substitute.For<IProdutoRepository>();
    private readonly IEstoqueRepository _estoque = Substitute.For<IEstoqueRepository>();
    private readonly IPedidoRepository _pedidos = Substitute.For<IPedidoRepository>();
    private readonly UnidadeDeTrabalhoFalsa _unitOfWork = new();
    private TimeProvider _relogio = new RelogioFalso(_agora);

    public PedidoServiceTests()
    {
        _clientes.ObterAsync(7, Arg.Any<CancellationToken>()).Returns(new ClienteResumo(7, "Mercado Bom Preço Ltda", true));
        _produtos.ObterAtivosAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ProdutosConhecidos.Where(p => ci.Arg<IReadOnlyCollection<int>>().Contains(p.Id)).ToList());
        _estoque.TentarBaixarAsync(12, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(149.90m);
        _estoque.TentarBaixarAsync(31, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(29.95m);
        _pedidos.AdicionarAsync(Arg.Any<Pedido>(), Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1042));
    }

    private static IReadOnlyList<ProdutoResumo> ProdutosConhecidos { get; } =
        [new(12, "Teclado Mecânico ABNT2"), new(31, "Mouse Pad Grande")];

    private PedidoService CriarSut() => new(_clientes, _produtos, _estoque, _pedidos, _unitOfWork, _relogio);

    private static CriarPedidoRequest Pedido12e31() =>
        new(7, [new CriarPedidoItemRequest(12, 2), new CriarPedidoItemRequest(31, 2)]);

    private Task<Result<PedidoResponse>> CriarAsync(CriarPedidoRequest request) =>
        CriarSut().CriarAsync(request, CancellationToken.None);

    [Fact] // CA-01, RN-09, RN-10, RN-11
    public async Task CriarAsync_PedidoValido_RetornaPedidoComPrecoDoBancoETotal()
    {
        var resultado = await CriarAsync(Pedido12e31());

        resultado.IsSuccess.ShouldBeTrue();
        var pedido = resultado.Value;
        pedido.Id.ShouldBe(1042);
        pedido.ClienteId.ShouldBe(7);
        pedido.ClienteNome.ShouldBe("Mercado Bom Preço Ltda");
        pedido.ValorTotal.ShouldBe(359.70m);
        pedido.Itens.ShouldBe(
        [
            new PedidoItemResponse(12, "Teclado Mecânico ABNT2", 2, 149.90m, 299.80m),
            new PedidoItemResponse(31, "Mouse Pad Grande", 2, 29.95m, 59.90m),
        ]);
        _unitOfWork.Commits.ShouldBe(1);
        _unitOfWork.Rollbacks.ShouldBe(0);
        await _pedidos.Received(1).AdicionarAsync(
            Arg.Is<Pedido>(p => p.ValorTotal == 359.70m && p.Itens.Count == 2), Arg.Any<CancellationToken>());
    }

    [Fact] // CA-05, RN-05
    public async Task CriarAsync_ClienteInexistente_RetornaClienteNaoEncontrado()
    {
        _clientes.ObterAsync(9999, Arg.Any<CancellationToken>()).Returns((ClienteResumo?)null);

        var resultado = await CriarAsync(new CriarPedidoRequest(9999, [new CriarPedidoItemRequest(12, 1)]));

        resultado.IsFailure.ShouldBeTrue();
        resultado.Error.Code.ShouldBe("CLIENTE_NAO_ENCONTRADO");
        resultado.Error.Tipo.ShouldBe(TipoErro.RegraDeNegocio);
        await NadaFoiBaixadoNemGravadoAsync();
    }

    [Fact] // CA-06, RN-06
    public async Task CriarAsync_ClienteInativo_RetornaClienteInativo()
    {
        _clientes.ObterAsync(8, Arg.Any<CancellationToken>()).Returns(new ClienteResumo(8, "Inativo", false));

        var resultado = await CriarAsync(new CriarPedidoRequest(8, [new CriarPedidoItemRequest(12, 1)]));

        resultado.Error.Code.ShouldBe("CLIENTE_INATIVO");
        resultado.Error.Tipo.ShouldBe(TipoErro.RegraDeNegocio);
        await NadaFoiBaixadoNemGravadoAsync();
    }

    [Fact] // CA-07, RN-07: lista todos os ids inválidos de uma vez
    public async Task CriarAsync_ProdutosInvalidos_RetornaTodosOsIdsInvalidos()
    {
        var request = new CriarPedidoRequest(7, [new CriarPedidoItemRequest(99, 1), new CriarPedidoItemRequest(12, 1), new CriarPedidoItemRequest(98, 1)]);

        var resultado = await CriarAsync(request);

        resultado.Error.Code.ShouldBe("PRODUTO_NAO_ENCONTRADO");
        resultado.Error.Tipo.ShouldBe(TipoErro.RegraDeNegocio);
        resultado.Error.Extensoes["produtoIds"].ShouldBe(new[] { 98, 99 });
        resultado.Error.Message.ShouldBe("Os produtos 98 e 99 não existem ou estão inativos.");
        await NadaFoiBaixadoNemGravadoAsync();
    }

    [Fact] // CA-08, RN-08, RN-10
    public async Task CriarAsync_EstoqueInsuficiente_RetornaConflitoSemGravar()
    {
        _estoque.TentarBaixarAsync(31, 6, Arg.Any<CancellationToken>()).Returns((decimal?)null);
        _produtos.ObterEstoquesAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, int> { [31] = 5 });
        var request = new CriarPedidoRequest(7, [new CriarPedidoItemRequest(12, 3), new CriarPedidoItemRequest(31, 6)]);

        var resultado = await CriarAsync(request);

        resultado.Error.Code.ShouldBe("ESTOQUE_INSUFICIENTE");
        resultado.Error.Tipo.ShouldBe(TipoErro.Conflito);
        resultado.Error.Extensoes["itens"].ShouldBe(new[] { new ItemSemEstoque(31, "Mouse Pad Grande", 6, 5) });
        _unitOfWork.Rollbacks.ShouldBe(1);
        _unitOfWork.Commits.ShouldBe(0);
        await _pedidos.DidNotReceive().AdicionarAsync(Arg.Any<Pedido>(), Arg.Any<CancellationToken>());
    }

    [Fact] // spec 4.3 item 4: não para na primeira falha
    public async Task CriarAsync_VariosItensSemEstoque_ListaTodosOsItens()
    {
        _estoque.TentarBaixarAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((decimal?)null);
        _produtos.ObterEstoquesAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, int> { [12] = 1, [31] = 0 });

        var resultado = await CriarAsync(Pedido12e31());

        resultado.Error.Extensoes["itens"].ShouldBe(new[]
        {
            new ItemSemEstoque(12, "Teclado Mecânico ABNT2", 2, 1),
            new ItemSemEstoque(31, "Mouse Pad Grande", 2, 0),
        });
        await _estoque.Received(2).TentarBaixarAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _pedidos.DidNotReceive().AdicionarAsync(Arg.Any<Pedido>(), Arg.Any<CancellationToken>());
    }

    [Fact] // spec 4.3 item 3
    public async Task CriarAsync_ItensForaDeOrdem_BaixaEmOrdemCrescenteDeProdutoId()
    {
        var request = new CriarPedidoRequest(7, [new CriarPedidoItemRequest(31, 1), new CriarPedidoItemRequest(12, 1)]);

        var resultado = await CriarAsync(request);

        resultado.IsSuccess.ShouldBeTrue();
        Received.InOrder(() =>
        {
            _estoque.TentarBaixarAsync(12, 1, Arg.Any<CancellationToken>());
            _estoque.TentarBaixarAsync(31, 1, Arg.Any<CancellationToken>());
        });
    }

    [Fact] // RN-13
    public async Task CriarAsync_PedidoValido_UsaDataDoTimeProviderEmUtc()
    {
        var resultado = await CriarAsync(Pedido12e31());

        resultado.Value.DataPedido.ShouldBe(new DateTime(2026, 11, 13, 14, 32, 7, DateTimeKind.Utc));
        resultado.Value.DataPedido.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact] // RN-13, HL-20
    public async Task CriarAsync_RelogioComTicks_TruncaDataPedidoEmMilissegundos()
    {
        _relogio = new RelogioFalso(_agora.AddTicks(1_234_567));

        var resultado = await CriarAsync(Pedido12e31());

        resultado.Value.DataPedido.ShouldBe(new DateTime(2026, 11, 13, 14, 32, 7, 123, DateTimeKind.Utc));
        await _pedidos.Received(1).AdicionarAsync(
            Arg.Is<Pedido>(p => p.DataPedido == new DateTime(2026, 11, 13, 14, 32, 7, 123, DateTimeKind.Utc)),
            Arg.Any<CancellationToken>());
    }

    [Fact] // ADR-0004: falha do repositório desfaz tudo e é repassada sem virar exceção
    public async Task CriarAsync_AdicionarFalha_RepassaOErroEFazRollback()
    {
        _pedidos.AdicionarAsync(Arg.Any<Pedido>(), Arg.Any<CancellationToken>())
            .Returns(Result<int>.Failure(PedidoErrors.ChaveIdempotenciaDuplicada()));

        var resultado = await CriarAsync(Pedido12e31());

        resultado.Error.Code.ShouldBe("CHAVE_IDEMPOTENCIA_DUPLICADA");
        _unitOfWork.Rollbacks.ShouldBe(1);
        await _produtos.DidNotReceive().ObterEstoquesAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact] // CA-14 / spec 4.2: a ordem é cliente → produtos → estoque
    public async Task CriarAsync_ClienteInexistenteEProdutoInvalido_RetornaClienteNaoEncontradoPrimeiro()
    {
        _clientes.ObterAsync(9999, Arg.Any<CancellationToken>()).Returns((ClienteResumo?)null);

        var resultado = await CriarAsync(new CriarPedidoRequest(9999, [new CriarPedidoItemRequest(99, 1)]));

        resultado.Error.Code.ShouldBe("CLIENTE_NAO_ENCONTRADO");
        await _produtos.DidNotReceive().ObterAtivosAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact] // spec 4.2: RN-06 antes de RN-07
    public async Task CriarAsync_ClienteInativoEProdutoInvalido_RetornaClienteInativoPrimeiro()
    {
        _clientes.ObterAsync(8, Arg.Any<CancellationToken>()).Returns(new ClienteResumo(8, "Inativo", false));

        var resultado = await CriarAsync(new CriarPedidoRequest(8, [new CriarPedidoItemRequest(99, 1)]));

        resultado.Error.Code.ShouldBe("CLIENTE_INATIVO");
        await _produtos.DidNotReceive().ObterAtivosAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact] // spec 4.2: RN-07 antes de RN-08
    public async Task CriarAsync_ProdutoInvalido_NaoTentaBaixarEstoque()
    {
        var resultado = await CriarAsync(new CriarPedidoRequest(7, [new CriarPedidoItemRequest(12, 1), new CriarPedidoItemRequest(99, 1)]));

        resultado.Error.Code.ShouldBe("PRODUTO_NAO_ENCONTRADO");
        await _estoque.DidNotReceive().TentarBaixarAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact] // CancellationToken repassado até as portas
    public async Task CriarAsync_TokenInformado_RepassaParaAsPortas()
    {
        using var cts = new CancellationTokenSource();

        await CriarSut().CriarAsync(Pedido12e31(), cts.Token);

        await _clientes.Received(1).ObterAsync(7, cts.Token);
        await _estoque.Received(2).TentarBaixarAsync(Arg.Any<int>(), Arg.Any<int>(), cts.Token);
        await _pedidos.Received(1).AdicionarAsync(Arg.Any<Pedido>(), cts.Token);
    }

    private async Task NadaFoiBaixadoNemGravadoAsync()
    {
        await _estoque.DidNotReceive().TentarBaixarAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _pedidos.DidNotReceive().AdicionarAsync(Arg.Any<Pedido>(), Arg.Any<CancellationToken>());
        _unitOfWork.Commits.ShouldBe(0);
    }
}
