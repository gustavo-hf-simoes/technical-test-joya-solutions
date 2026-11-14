using Joya.Pedidos.Core.Common;
using Shouldly;

namespace Joya.Pedidos.UnitTests.Common;

public sealed class PeriodoTests
{
    [Fact] // CA-17, RN-15
    public void Criar_DataFimInclusiva_GeraFimExclusivoNoDiaSeguinte()
    {
        var resultado = Periodo.Criar(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Inicio.ShouldBe(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        resultado.Value.FimExclusivo.ShouldBe(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        resultado.Value.FimExclusivo?.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact] // RN-15: fim de ano
    public void Criar_FimEmTrintaEUmDeDezembro_VaiParaPrimeiroDeJaneiro()
    {
        var resultado = Periodo.Criar(null, new DateOnly(2026, 12, 31));

        resultado.Value.Inicio.ShouldBeNull();
        resultado.Value.FimExclusivo.ShouldBe(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact] // RN-16: filtros opcionais
    public void Criar_SemDatas_RetornaPeriodoAberto()
    {
        var resultado = Periodo.Criar(null, null);

        resultado.Value.ShouldBe(new Periodo(null, null));
    }

    [Fact] // RN-15: um dia só
    public void Criar_MesmoDia_CobreVinteEQuatroHoras()
    {
        var dia = new DateOnly(2026, 3, 15);

        var resultado = Periodo.Criar(dia, dia);

        (resultado.Value.FimExclusivo - resultado.Value.Inicio).ShouldBe(TimeSpan.FromDays(1));
    }

    [Fact] // CA-19, RN-15
    public void Criar_InicioMaiorQueFim_RetornaValidacao()
    {
        var resultado = Periodo.Criar(new DateOnly(2026, 4, 1), new DateOnly(2026, 3, 31));

        resultado.IsFailure.ShouldBeTrue();
        resultado.Error.Code.ShouldBe("VALIDACAO");
        resultado.Error.Tipo.ShouldBe(TipoErro.Validacao);
    }

    [Theory]
    [InlineData("2026-03-01", true)]
    [InlineData("11/13/2026", false)]
    [InlineData("2026-3-1", false)]
    [InlineData("2026-02-30", false)]
    [InlineData("20260301", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TentarLerData_Formato_SoAceitaAnoMesDia(string? texto, bool esperado)
    {
        Periodo.TentarLerData(texto, out _).ShouldBe(esperado);
    }
}
