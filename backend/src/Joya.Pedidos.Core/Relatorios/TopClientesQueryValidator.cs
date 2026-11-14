using FluentValidation;
using Joya.Pedidos.Core.Common;

namespace Joya.Pedidos.Core.Relatorios;

/// <summary>Validação estrutural do ranking: período obrigatório, formato <c>yyyy-MM-dd</c>, início ≤ fim (RN-15, RN-17).</summary>
public sealed class TopClientesQueryValidator : AbstractValidator<TopClientesQuery>
{
    public TopClientesQueryValidator()
    {
        this.RegrasDePeriodo(q => q.DataInicio, q => q.DataFim, obrigatorio: true);
    }
}
