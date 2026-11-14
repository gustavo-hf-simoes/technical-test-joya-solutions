using FluentValidation.Results;
using Joya.Pedidos.Core.Relatorios;
using Shouldly;

namespace Joya.Pedidos.UnitTests.Relatorios;

public sealed class TopClientesQueryValidatorTests
{
    private readonly TopClientesQueryValidator _validator = new();

    private static List<string> Campos(ValidationResult resultado) => [.. resultado.Errors.Select(e => e.PropertyName)];

    [Fact]
    public void Validate_PeriodoValido_NaoTemErros()
    {
        _validator.Validate(new TopClientesQuery("2026-01-01", "2026-12-31")).IsValid.ShouldBeTrue();
    }

    [Fact] // CA-26, RN-17
    public void Validate_SemDataInicio_RetornaErroDeValidacao()
    {
        var resultado = _validator.Validate(new TopClientesQuery(null, "2026-12-31"));

        Campos(resultado).ShouldBe(["DataInicio"]);
    }

    [Fact] // CA-26, RN-17
    public void Validate_SemDataFim_RetornaErroDeValidacao()
    {
        Campos(_validator.Validate(new TopClientesQuery("2026-01-01", null))).ShouldBe(["DataFim"]);
    }

    [Fact] // CA-26
    public void Validate_SemNenhumaData_RetornaErroNasDuas()
    {
        Campos(_validator.Validate(new TopClientesQuery())).ShouldBe(["DataInicio", "DataFim"], ignoreOrder: true);
    }

    [Fact] // spec 5.4: formato invalido
    public void Validate_FormatoInvalido_RetornaErroNoCampo()
    {
        Campos(_validator.Validate(new TopClientesQuery("01/01/2026", "2026-12-31"))).ShouldBe(["DataInicio"]);
    }

    [Fact] // RN-15
    public void Validate_InicioMaiorQueFim_RetornaErroNoCampoDataFim()
    {
        Campos(_validator.Validate(new TopClientesQuery("2026-12-31", "2026-01-01"))).ShouldBe(["DataFim"]);
    }
}
