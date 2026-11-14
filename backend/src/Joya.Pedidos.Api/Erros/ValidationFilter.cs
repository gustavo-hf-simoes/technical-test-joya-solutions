using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Joya.Pedidos.Api.Erros;

/// <summary>
/// Roda o <see cref="IValidator{T}"/> de cada argumento da action antes dela (spec 5.1, D9). Falha vira 400
/// <c>VALIDACAO</c> e a action não executa: nenhum acesso ao banco acontece (CA-14). Os nomes das propriedades
/// saem em <c>camelCase</c>, como no JSON (<c>itens[1].quantidade</c>).
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var erros = new Dictionary<string, string[]>();

        foreach (var argumento in context.ActionArguments.Values)
        {
            if (argumento is null)
            {
                continue;
            }

            var tipoDoValidator = typeof(IValidator<>).MakeGenericType(argumento.GetType());
            if (context.HttpContext.RequestServices.GetService(tipoDoValidator) is not IValidator validator)
            {
                continue;
            }

            var resultado = await validator.ValidateAsync(
                new ValidationContext<object>(argumento),
                context.HttpContext.RequestAborted);

            foreach (var grupo in resultado.Errors.GroupBy(e => Camel(e.PropertyName)))
            {
                erros[grupo.Key] = [.. grupo.Select(e => e.ErrorMessage).Distinct()];
            }
        }

        if (erros.Count > 0)
        {
            context.Result = ProblemasHttp.Validacao(erros, context.HttpContext);
            return;
        }

        await next();
    }

    /// <summary><c>Itens[1].Quantidade</c> vira <c>itens[1].quantidade</c>.</summary>
    private static string Camel(string caminho) =>
        string.Join('.', caminho.Split('.').Select(segmento =>
            segmento.Length == 0 ? segmento : char.ToLowerInvariant(segmento[0]) + segmento[1..]));
}
