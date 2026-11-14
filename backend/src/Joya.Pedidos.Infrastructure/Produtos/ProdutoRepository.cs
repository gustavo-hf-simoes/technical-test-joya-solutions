using Joya.Pedidos.Core.Produtos;
using Joya.Pedidos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Joya.Pedidos.Infrastructure.Produtos;

/// <summary>
/// Leituras de produto para validar o pedido. Os ids viram um único parâmetro JSON (<c>OPENJSON</c>, EF 8), então o
/// plano é o mesmo para qualquer quantidade de itens. Nenhum método aqui decide estoque (spec 4.3).
/// </summary>
public sealed class ProdutoRepository(PedidosDbContext contexto) : IProdutoRepository
{
    public async Task<IReadOnlyList<ProdutoResumo>> ObterAtivosAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(ids);
        int[] lista = [.. ids];

        return await contexto.Produtos
            .AsNoTracking()
            .Where(p => lista.Contains(p.Id) && p.Ativo)
            .OrderBy(p => p.Id)
            .Select(p => new ProdutoResumo(p.Id, p.Nome))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<int, int>> ObterEstoquesAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(ids);
        int[] lista = [.. ids];

        // Só informativo, para a mensagem do 409 (spec 5.2): lido depois do rollback, fora da transação.
        return await contexto.Produtos
            .AsNoTracking()
            .Where(p => lista.Contains(p.Id))
            .Select(p => new { p.Id, p.Estoque })
            .ToDictionaryAsync(p => p.Id, p => p.Estoque, ct);
    }

    public async Task<IReadOnlyList<ProdutoResponse>> ListarAsync(bool? ativo, CancellationToken ct) =>
        await contexto.Produtos
            .AsNoTracking()
            .Where(p => ativo == null || p.Ativo == ativo)
            .OrderBy(p => p.Nome).ThenBy(p => p.Id)
            .Select(p => new ProdutoResponse(p.Id, p.Sku, p.Nome, p.Preco, p.Estoque))
            .ToListAsync(ct);
}
