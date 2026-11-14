using FluentValidation;

namespace Joya.Pedidos.Core.Pedidos;

/// <summary>Validação estrutural do pedido (RN-01 a RN-04, spec 4.1). Não consulta o banco.</summary>
public sealed class CriarPedidoValidator : AbstractValidator<CriarPedidoRequest>
{
    public const int MaximoItens = Pedido.MaximoItens;
    public const int QuantidadeMinima = ItemPedido.QuantidadeMinima;
    public const int QuantidadeMaxima = ItemPedido.QuantidadeMaxima;

    public CriarPedidoValidator()
    {
        // RN-04
        RuleFor(r => r.ClienteId)
            .GreaterThan(0).WithMessage("O cliente deve ser um inteiro positivo.");

        // RN-01
        RuleFor(r => r.Itens)
            .NotEmpty().WithMessage("O pedido deve ter ao menos um item.")
            .Must(itens => itens is null || itens.Count <= MaximoItens)
            .WithMessage($"O pedido pode ter no máximo {MaximoItens} itens.");

        RuleForEach(r => r.Itens).ChildRules(item =>
        {
            // RN-04
            item.RuleFor(i => i.ProdutoId)
                .GreaterThan(0).WithMessage("O produto deve ser um inteiro positivo.");

            // RN-02
            item.RuleFor(i => i.Quantidade)
                .InclusiveBetween(QuantidadeMinima, QuantidadeMaxima)
                .WithMessage($"A quantidade deve estar entre {QuantidadeMinima} e {QuantidadeMaxima}.");
        });

        // RN-03
        RuleFor(r => r.Itens)
            .Must(itens => itens is null || itens.Select(i => i.ProdutoId).Distinct().Count() == itens.Count)
            .WithMessage("Um produto não pode aparecer em mais de um item do pedido.");
    }
}
