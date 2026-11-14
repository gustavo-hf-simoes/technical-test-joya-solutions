using System.Text;
using Joya.Pedidos.Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace Joya.Pedidos.Api.Erros;

/// <summary>
/// Único ponto que traduz <see cref="Error"/> e falhas de validação em Problem Details (RFC 9457) com as
/// extensões <c>code</c> e <c>traceId</c> (spec 5.1). O tipo do erro define o status HTTP aqui e em nenhum outro lugar.
/// </summary>
public static class ProblemasHttp
{
    public const string CodigoValidacao = "VALIDACAO";

    /// <summary>Títulos fixados pelos exemplos da spec 5.2. Código fora do mapa usa o título genérico do tipo.</summary>
    private static readonly Dictionary<string, string> _titulos = new()
    {
        ["CLIENTE_NAO_ENCONTRADO"] = "Cliente inexistente",
        ["CLIENTE_INATIVO"] = "Cliente inativo",
        ["PRODUTO_NAO_ENCONTRADO"] = "Produto inexistente ou inativo",
        ["ESTOQUE_INSUFICIENTE"] = "Estoque insuficiente",
        ["CHAVE_IDEMPOTENCIA_DUPLICADA"] = "Chave de idempotência já utilizada",
    };

    /// <summary>Converte a falha de negócio em resposta HTTP: 400, 404, 409 ou 422 conforme o <see cref="TipoErro"/>.</summary>
    public static IActionResult ToProblem(this Error erro, HttpContext httpContext)
    {
        var (status, tituloGenerico) = erro.Tipo switch
        {
            TipoErro.Validacao => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            TipoErro.NaoEncontrado => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            TipoErro.Conflito => (StatusCodes.Status409Conflict, "Conflito com o estado atual"),
            TipoErro.RegraDeNegocio => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno"),
        };

        var titulo = _titulos.GetValueOrDefault(erro.Code, tituloGenerico);

        var problema = new ProblemDetails
        {
            Type = $"/erros/{Kebab(erro.Code)}",
            Title = titulo,
            Status = status,
            Detail = erro.Message,
        };
        problema.Extensions["code"] = erro.Code;
        foreach (var extensao in erro.Extensoes)
        {
            problema.Extensions[extensao.Key] = extensao.Value;
        }

        return Resposta(problema);
    }

    /// <summary>400 <c>VALIDACAO</c> com o mapa <c>errors</c> (propriedade → mensagens).</summary>
    public static IActionResult Validacao(IDictionary<string, string[]> erros, HttpContext httpContext)
    {
        var problema = new ProblemDetails
        {
            Type = "/erros/validacao",
            Title = "Requisição inválida",
            Status = StatusCodes.Status400BadRequest,
        };
        problema.Extensions["code"] = CodigoValidacao;
        problema.Extensions["errors"] = erros; // ProblemDetails simples: o serviço de escrita não serializa a subclasse

        return Resposta(problema);
    }

    private static ProblemaResult Resposta(ProblemDetails problema) => new(problema);

    /// <summary>
    /// Escreve pelo <see cref="IProblemDetailsService"/>, o mesmo caminho dos handlers de exceção: sai como
    /// <c>application/problem+json</c> (o <c>[Produces]</c> do controller não interfere) e com o <c>traceId</c>.
    /// </summary>
    private sealed class ProblemaResult(ProblemDetails problema) : IActionResult
    {
        public async Task ExecuteResultAsync(ActionContext context)
        {
            var httpContext = context.HttpContext;
            httpContext.Response.StatusCode = problema.Status ?? StatusCodes.Status500InternalServerError;

            await httpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problema,
            });
        }
    }

    /// <summary><c>CLIENTE_NAO_ENCONTRADO</c> vira <c>cliente-nao-encontrado</c>.</summary>
    private static string Kebab(string codigo)
    {
        var texto = new StringBuilder(codigo.Length);
        foreach (var letra in codigo)
        {
            texto.Append(letra == '_' ? '-' : char.ToLowerInvariant(letra));
        }

        return texto.ToString();
    }
}
