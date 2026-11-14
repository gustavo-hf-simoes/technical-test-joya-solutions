namespace Joya.Pedidos.Core.Produtos;

public interface IProdutoService
{
    /// <summary>Lista produtos por nome. <paramref name="ativo"/> nulo traz todos (spec 5.5, HL-18).</summary>
    Task<IReadOnlyList<ProdutoResponse>> ListarAsync(bool? ativo, CancellationToken ct);
}

public sealed class ProdutoService(IProdutoRepository produtos) : IProdutoService
{
    public Task<IReadOnlyList<ProdutoResponse>> ListarAsync(bool? ativo, CancellationToken ct) =>
        produtos.ListarAsync(ativo, ct);
}
