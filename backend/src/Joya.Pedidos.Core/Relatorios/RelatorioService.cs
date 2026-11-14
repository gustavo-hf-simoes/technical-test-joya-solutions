using Joya.Pedidos.Core.Common;

namespace Joya.Pedidos.Core.Relatorios;

public interface IRelatorioService
{
    /// <summary>Ranking dos clientes por valor no período (spec 5.4, RN-15, RN-17). O formato já foi validado antes.</summary>
    Task<Result<IReadOnlyList<TopClienteResponse>>> TopClientesAsync(TopClientesQuery query, CancellationToken ct);
}

public sealed class RelatorioService(IRelatorioConsultas consultas) : IRelatorioService
{
    /// <summary>Tamanho do ranking (RN-17: "até 5").</summary>
    public const int TamanhoRanking = 5;

    public async Task<Result<IReadOnlyList<TopClienteResponse>>> TopClientesAsync(TopClientesQuery query, CancellationToken ct)
    {
        var periodo = Periodo.Criar(LerData(query.DataInicio), LerData(query.DataFim)); // RN-15
        if (periodo.IsFailure)
        {
            return periodo.Error;
        }

        if (periodo.Value is not { Inicio: { } inicio, FimExclusivo: { } fimExclusivo })
        {
            return RelatorioErrors.PeriodoObrigatorio(); // RN-17: nunca um ranking sem limite de período
        }

        var ranking = await consultas.TopClientesAsync(inicio, fimExclusivo, TamanhoRanking, ct);
        return Result<IReadOnlyList<TopClienteResponse>>.Success(ranking);
    }

    private static DateOnly? LerData(string? texto) =>
        Periodo.TentarLerData(texto, out var data) ? data : null;
}
