using FluentValidation;

namespace Joya.Pedidos.Core.Common;

/// <summary>Regras de FluentValidation do período (RN-15) compartilhadas pela listagem e pelo ranking.</summary>
public static class PeriodoRegras
{
    /// <summary>
    /// Formato <c>yyyy-MM-dd</c> em cada data informada, <c>dataInicio ≤ dataFim</c> e, se <paramref name="obrigatorio"/>,
    /// presença das duas (RN-17). Os campos de erro seguem o JSON (<c>DataInicio</c>, <c>DataFim</c>).
    /// </summary>
    public static void RegrasDePeriodo<T>(
        this AbstractValidator<T> validator,
        Func<T, string?> dataInicio,
        Func<T, string?> dataFim,
        bool obrigatorio)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(dataInicio);
        ArgumentNullException.ThrowIfNull(dataFim);

        validator.RuleFor(q => q).Custom((consulta, contexto) =>
        {
            var inicio = Ler(dataInicio(consulta), obrigatorio, "DataInicio", "inicial", contexto);
            var fim = Ler(dataFim(consulta), obrigatorio, "DataFim", "final", contexto);

            if (inicio is { } i && fim is { } f && i > f)
            {
                contexto.AddFailure("DataFim", "A data final não pode ser anterior à data inicial.");
            }
        });
    }

    private static DateOnly? Ler<T>(string? texto, bool obrigatorio, string campo, string rotulo, ValidationContext<T> contexto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            if (obrigatorio)
            {
                contexto.AddFailure(campo, $"A data {rotulo} é obrigatória.");
            }

            return null;
        }

        if (Periodo.TentarLerData(texto, out var data))
        {
            return data;
        }

        contexto.AddFailure(campo, $"A data {rotulo} deve estar no formato {Periodo.FormatoData}.");
        return null;
    }
}
