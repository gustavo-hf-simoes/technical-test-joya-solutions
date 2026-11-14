using System.Globalization;

namespace Joya.Pedidos.Core.Common;

/// <summary>
/// Período de datas inclusivas em UTC (RN-15) convertido no intervalo semiaberto <c>[Inicio, FimExclusivo)</c> que o filtro
/// aplica direto sobre a coluna. Qualquer ponta pode faltar quando o filtro é opcional.
/// </summary>
public sealed record Periodo(DateTime? Inicio, DateTime? FimExclusivo)
{
    public const string FormatoData = "yyyy-MM-dd";

    public const string CodigoInvalido = "VALIDACAO";

    /// <summary>Lê só <c>yyyy-MM-dd</c>, com cultura invariante: <c>11/13/2026</c> não passa (spec 5.1).</summary>
    public static bool TentarLerData(string? texto, out DateOnly data) =>
        DateOnly.TryParseExact(texto, FormatoData, CultureInfo.InvariantCulture, DateTimeStyles.None, out data);

    /// <summary>
    /// A data final é inclusiva: o fim exclusivo é o dia seguinte à meia-noite UTC. Falha com <c>VALIDACAO</c> se
    /// <paramref name="dataInicio"/> for maior que <paramref name="dataFim"/>.
    /// </summary>
    public static Result<Periodo> Criar(DateOnly? dataInicio, DateOnly? dataFim)
    {
        if (dataInicio is { } inicio && dataFim is { } fim && inicio > fim)
        {
            return Error.Validation(CodigoInvalido, "A data inicial não pode ser maior que a data final.");
        }

        return new Periodo(
            dataInicio?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            dataFim?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
    }
}
