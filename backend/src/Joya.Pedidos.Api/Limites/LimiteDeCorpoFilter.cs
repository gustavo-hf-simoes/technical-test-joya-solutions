using Microsoft.AspNetCore.Mvc.Filters;

namespace Joya.Pedidos.Api.Limites;

/// <summary>
/// Rejeita, antes da leitura do corpo, a requisição cujo <c>Content-Length</c> passa de
/// <see cref="LimitesHttp.CorpoMaximoBytes"/> (ADR-0002, item 1, opção 2). Lança a mesma
/// <see cref="BadHttpRequestException"/> que o Kestrel lança, para que os dois caminhos saiam iguais pelo
/// <c>RequisicaoHttpInvalidaExceptionHandler</c>. Corpo sem <c>Content-Length</c> (<i>chunked</i>) fica com o
/// limite global do Kestrel, que é a proteção real; o <c>TestServer</c> não aplica nem um nem outro.
/// </summary>
public sealed class LimiteDeCorpoFilter : IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        if (context.HttpContext.Request.ContentLength > LimitesHttp.CorpoMaximoBytes)
            throw new BadHttpRequestException("Corpo acima do limite.", StatusCodes.Status413PayloadTooLarge);
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
