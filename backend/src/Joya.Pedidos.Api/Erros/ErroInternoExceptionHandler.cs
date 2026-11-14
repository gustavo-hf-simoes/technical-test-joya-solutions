using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Joya.Pedidos.Api.Erros;

/// <summary>
/// Exceção não prevista vira <c>500</c> Problem Details com <c>code = ERRO_INTERNO</c> e <c>traceId</c> (spec 5.1, CA-38).
/// A exceção só vai para o log: nunca para o corpo da resposta.
/// </summary>
public sealed class ErroInternoExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<ErroInternoExceptionHandler> logger) : IExceptionHandler
{
    public const string Codigo = "ERRO_INTERNO";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Falha não tratada em {Metodo} {Caminho} (traceId {TraceId})",
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            Activity.Current?.Id ?? httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problema = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Erro interno.",
            Detail = "Ocorreu uma falha inesperada. Informe o traceId ao suporte.",
        };
        problema.Extensions["code"] = Codigo;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problema,
        });
    }
}
