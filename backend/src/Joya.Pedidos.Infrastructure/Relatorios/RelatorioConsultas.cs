using Joya.Pedidos.Core.Relatorios;
using Joya.Pedidos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Joya.Pedidos.Infrastructure.Relatorios;

/// <summary>
/// Ranking de clientes (spec 5.4, Q5 da arquitetura). Uma única consulta sem tracking, resolvida no banco:
/// filtro semiaberto sobre <c>DataPedido</c> (RN-15, sem função sobre a coluna), agrupamento por <c>ClienteId</c> e nunca
/// por nome (RN-17), <c>TOP</c> com desempate por <c>ClienteId</c> e só então o join com <c>Clientes</c> para o nome.
/// Soma o <c>ValorTotal</c> do pedido, sem ler <c>ItensPedido</c>: não há join que multiplique linhas (spec 6.4).
/// </summary>
public sealed class RelatorioConsultas(PedidosDbContext contexto) : IRelatorioConsultas
{
    public async Task<IReadOnlyList<TopClienteResponse>> TopClientesAsync(
        DateTime inicio, DateTime fimExclusivo, int quantidade, CancellationToken ct)
    {
        var totais = contexto.Pedidos.AsNoTracking()
            .Where(p => p.DataPedido >= inicio && p.DataPedido < fimExclusivo)
            .GroupBy(p => p.ClienteId)
            .Select(g => new { ClienteId = g.Key, ValorTotal = g.Sum(p => p.ValorTotal), QuantidadePedidos = g.Count() })
            .OrderByDescending(t => t.ValorTotal)
            .ThenBy(t => t.ClienteId)
            .Take(quantidade);

        var linhas = await (
                from t in totais
                join c in contexto.Clientes.AsNoTracking() on t.ClienteId equals c.Id
                orderby t.ValorTotal descending, t.ClienteId
                select new { t.ClienteId, c.Nome, t.ValorTotal, t.QuantidadePedidos })
            .ToListAsync(ct);

        // A posição é só a ordem das no máximo 'quantidade' linhas já ordenadas pelo banco.
        return [.. linhas.Select((l, i) => new TopClienteResponse(i + 1, l.ClienteId, l.Nome, l.ValorTotal, l.QuantidadePedidos))];
    }
}
