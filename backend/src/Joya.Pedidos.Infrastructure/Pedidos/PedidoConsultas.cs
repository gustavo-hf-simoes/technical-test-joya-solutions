using Joya.Pedidos.Core.Common;
using Joya.Pedidos.Core.Pedidos;
using Joya.Pedidos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Joya.Pedidos.Infrastructure.Pedidos;

/// <summary>
/// Leitura de pedidos (spec 5.3). Duas consultas sem tracking: o total e a página, esta com <c>OFFSET/FETCH</c> sobre
/// o índice e projeção direta para o DTO (a contagem de itens é uma subconsulta, sem <c>Include</c> nem N+1).
/// O filtro de período é semiaberto sobre <c>DataPedido</c>, sem função sobre a coluna (RN-15).
/// </summary>
public sealed class PedidoConsultas(PedidosDbContext contexto) : IPedidoConsultas
{
    public async Task<PaginaResponse<PedidoResumoResponse>> ListarAsync(
        int? clienteId, Periodo periodo, int page, int pageSize, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(periodo);

        var filtrados = contexto.Pedidos.AsNoTracking();

        if (clienteId is { } cliente)
        {
            filtrados = filtrados.Where(p => p.ClienteId == cliente);
        }

        if (periodo.Inicio is { } inicio)
        {
            filtrados = filtrados.Where(p => p.DataPedido >= inicio);
        }

        if (periodo.FimExclusivo is { } fimExclusivo)
        {
            filtrados = filtrados.Where(p => p.DataPedido < fimExclusivo);
        }

        var total = await filtrados.CountAsync(ct);

        // Página além do fim (inclusive um deslocamento que estouraria o int do Skip) é 200 com lista vazia (spec 5.3).
        var deslocamento = ((long)page - 1) * pageSize;
        if (deslocamento >= total)
        {
            return new PaginaResponse<PedidoResumoResponse>([], page, pageSize, total);
        }

        var itens = await (
                from p in filtrados.OrderByDescending(p => p.DataPedido).ThenByDescending(p => p.Id)
                    .Skip((int)deslocamento).Take(pageSize)
                join c in contexto.Clientes.AsNoTracking() on p.ClienteId equals c.Id
                orderby p.DataPedido descending, p.Id descending
                select new PedidoResumoResponse(p.Id, p.ClienteId, c.Nome, p.DataPedido, p.ValorTotal, p.Itens.Count()))
            .ToListAsync(ct);

        return new PaginaResponse<PedidoResumoResponse>(itens, page, pageSize, total);
    }
}
