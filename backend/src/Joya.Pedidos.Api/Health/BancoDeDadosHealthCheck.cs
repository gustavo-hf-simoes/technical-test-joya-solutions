using Joya.Pedidos.Core.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Joya.Pedidos.Api.Health;

/// <summary>Readiness: saudável só se o SQL Server aceita conexões.</summary>
public sealed class BancoDeDadosHealthCheck(IVerificadorDeBanco verificador) : IHealthCheck
{
    public const string Tag = "ready";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await verificador.EstaDisponivelAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Banco de dados indisponível.");
}
