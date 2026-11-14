using System.Diagnostics;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Joya.Pedidos.Api.Limites;

/// <summary>
/// Política <see cref="LimitesHttp.PoliticaCriarPedido"/>: janela fixa por IP, sem fila (ADR-0002, item 2).
/// O IP é o de <c>Connection.RemoteIpAddress</c> depois do <c>UseForwardedHeaders</c>; sem IP (caso do
/// <c>TestServer</c>) a partição é <see cref="ParticaoSemIp"/>. A rejeição sai como <c>429</c> Problem Details
/// com <c>code = LIMITE_DE_TAXA</c>, <c>traceId</c> e <c>Retry-After</c>.
/// </summary>
public sealed class LimiteCriarPedidoPolicy(
    IOptions<LimiteDeTaxaOptions> opcoes,
    IProblemDetailsService problemDetails,
    ILogger<LimiteCriarPedidoPolicy> logger) : IRateLimiterPolicy<string>
{
    public const string Codigo = "LIMITE_DE_TAXA";
    public const string ParticaoSemIp = "desconhecido";

    private readonly LimiteDeTaxaOptions _limite = opcoes.Value;

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => EscreverRejeicaoAsync;

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? ParticaoSemIp;

        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = _limite.Permissoes,
            Window = _limite.Janela,
            QueueLimit = 0, // excedente rejeitado na hora, sem segurar conexão
            AutoReplenishment = true,
        });
    }

    private async ValueTask EscreverRejeicaoAsync(OnRejectedContext contexto, CancellationToken ct)
    {
        var httpContext = contexto.HttpContext;
        var espera = contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? retryAfter
            : _limite.Janela;
        var segundos = Math.Max(1, (int)Math.Ceiling(espera.TotalSeconds));

        logger.LogWarning(
            "Limite de taxa excedido em {Metodo} {Caminho} (traceId {TraceId})",
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            Activity.Current?.Id ?? httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.Headers.RetryAfter = segundos.ToString(CultureInfo.InvariantCulture);

        var problema = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Muitas requisições.",
            Detail = $"Limite de criação de pedidos excedido. Tente novamente em {segundos} segundo(s).",
        };
        problema.Extensions["code"] = Codigo;

        await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problema,
        });
    }
}
