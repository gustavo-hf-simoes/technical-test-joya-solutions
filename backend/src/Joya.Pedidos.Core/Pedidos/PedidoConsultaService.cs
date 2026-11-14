using Joya.Pedidos.Core.Common;

namespace Joya.Pedidos.Core.Pedidos;

public interface IPedidoConsultaService
{
    /// <summary>Lista pedidos com filtros opcionais e paginação (RN-15, RN-16). O formato já foi validado antes.</summary>
    Task<Result<PaginaResponse<PedidoResumoResponse>>> ListarAsync(ListarPedidosQuery query, CancellationToken ct);
}

public sealed class PedidoConsultaService(IPedidoConsultas consultas) : IPedidoConsultaService
{
    public async Task<Result<PaginaResponse<PedidoResumoResponse>>> ListarAsync(ListarPedidosQuery query, CancellationToken ct)
    {
        var periodo = Periodo.Criar(LerData(query.DataInicio), LerData(query.DataFim)); // RN-15
        if (periodo.IsFailure)
        {
            return periodo.Error;
        }

        return await consultas.ListarAsync(query.ClienteId, periodo.Value, query.Page, query.PageSize, ct);
    }

    private static DateOnly? LerData(string? texto) =>
        Periodo.TentarLerData(texto, out var data) ? data : null;
}
