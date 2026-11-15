namespace Joya.Pedidos.Core.Produtos;

public sealed class ProdutoService(IProdutoRepository produtos) : IProdutoService
{
    public Task<IReadOnlyList<ProdutoResponse>> ListarAsync(bool? ativo, CancellationToken ct) =>
        produtos.ListarAsync(ativo, ct);
}
