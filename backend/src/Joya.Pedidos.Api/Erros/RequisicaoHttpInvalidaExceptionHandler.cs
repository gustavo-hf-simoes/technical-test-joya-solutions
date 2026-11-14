using System.Diagnostics;
using Joya.Pedidos.Api.Limites;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Joya.Pedidos.Api.Erros;

/// <summary>
/// Requisição rejeitada pelo servidor HTTP ao ler o corpo (<see cref="BadHttpRequestException"/>): corpo acima de
/// <see cref="LimitesHttp.CorpoMaximoBytes"/> (<c>413</c>) ou malformado no nível HTTP (<c>400</c>). Responde com o
/// <c>StatusCode</c> da exceção em Problem Details, antes do <see cref="ErroInternoExceptionHandler"/> (ADR-0002, item 1).
/// A mensagem da exceção só vai para o log.
/// </summary>
public sealed class RequisicaoHttpInvalidaExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<RequisicaoHttpInvalidaExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>Catálogo da spec 5.1: todo <c>400</c> leva <c>code = VALIDACAO</c>.</summary>
    public const string CodigoValidacao = "VALIDACAO";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException requisicaoInvalida)
            return false;

        var status = requisicaoInvalida.StatusCode;

        logger.LogWarning(
            exception,
            "Requisição rejeitada com {Status} em {Metodo} {Caminho} (traceId {TraceId})",
            status,
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            Activity.Current?.Id ?? httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = status;

        var problema = status == StatusCodes.Status413PayloadTooLarge
            ? new ProblemDetails
            {
                Status = status,
                Title = "Corpo da requisição grande demais.",
                Detail = $"O corpo da requisição excede o limite de {LimitesHttp.CorpoMaximoBytes} bytes.",
            }
            : new ProblemDetails
            {
                Status = status,
                Title = "Requisição inválida.",
                Detail = "A requisição não pôde ser lida.",
            };
        if (status == StatusCodes.Status400BadRequest)
            problema.Extensions["code"] = CodigoValidacao;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problema,
        });
    }
}
