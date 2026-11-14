using FluentValidation;
using Joya.Pedidos.Core.Common;

namespace Joya.Pedidos.Core.Pedidos;

/// <summary>Validação estrutural da listagem (RN-15, RN-16). Filtros opcionais; valor presente e inválido é 400 (HL-18).</summary>
public sealed class ListarPedidosQueryValidator : AbstractValidator<ListarPedidosQuery>
{
    public const int PaginaPadrao = 1;
    public const int TamanhoPadrao = 20;
    public const int TamanhoMinimo = 1;
    public const int TamanhoMaximo = 100;

    public ListarPedidosQueryValidator()
    {
        // RN-16
        RuleFor(q => q.ClienteId)
            .GreaterThanOrEqualTo(1).When(q => q.ClienteId.HasValue)
            .WithMessage("O cliente deve ser um inteiro positivo.");

        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(PaginaPadrao).WithMessage($"A página deve ser maior ou igual a {PaginaPadrao}.");

        RuleFor(q => q.PageSize)
            .InclusiveBetween(TamanhoMinimo, TamanhoMaximo)
            .WithMessage($"O tamanho da página deve estar entre {TamanhoMinimo} e {TamanhoMaximo}.");

        // RN-15
        this.RegrasDePeriodo(q => q.DataInicio, q => q.DataFim, obrigatorio: false);
    }
}
