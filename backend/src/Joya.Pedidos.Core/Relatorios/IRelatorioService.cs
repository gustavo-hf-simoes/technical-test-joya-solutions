using Joya.Pedidos.Core.Common;

namespace Joya.Pedidos.Core.Relatorios;

public interface IRelatorioService
{
    /// <summary>Ranking dos clientes por valor no período (spec 5.4, RN-15, RN-17). O formato já foi validado antes.</summary>
    Task<Result<IReadOnlyList<TopClienteResponse>>> TopClientesAsync(TopClientesQuery query, CancellationToken ct);
}
