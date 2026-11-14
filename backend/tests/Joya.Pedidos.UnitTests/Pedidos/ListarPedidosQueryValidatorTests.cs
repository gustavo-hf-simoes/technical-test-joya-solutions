using FluentValidation.Results;
using Joya.Pedidos.Core.Pedidos;
using Shouldly;

namespace Joya.Pedidos.UnitTests.Pedidos;

public sealed class ListarPedidosQueryValidatorTests
{
    private readonly ListarPedidosQueryValidator _validator = new();

    private static List<string> Campos(ValidationResult resultado) => [.. resultado.Errors.Select(e => e.PropertyName)];

    [Fact]
    public void Validate_SemParametros_NaoTemErros()
    {
        _validator.Validate(new ListarPedidosQuery()).IsValid.ShouldBeTrue();
    }

    [Fact] // CA-15, RN-16
    public void Query_Padrao_EPaginaUmComVinte()
    {
        var consulta = new ListarPedidosQuery();

        consulta.Page.ShouldBe(1);
        consulta.PageSize.ShouldBe(20);
    }

    [Fact] // CA-19, RN-16
    public void Validate_ParametrosInvalidos_RetornaErroDeValidacao()
    {
        var resultado = _validator.Validate(new ListarPedidosQuery(
            ClienteId: 0, DataInicio: "13/11/2026", DataFim: "abc", Page: 0, PageSize: 101));

        Campos(resultado).ShouldBe(["ClienteId", "Page", "PageSize", "DataInicio", "DataFim"], ignoreOrder: true);
    }

    [Theory] // CA-19, RN-16
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_PaginaMenorQueUm_RetornaErroNoCampoPage(int pagina)
    {
        Campos(_validator.Validate(new ListarPedidosQuery(Page: pagina))).ShouldBe(["Page"]);
    }

    [Theory] // RN-16: 1 a 100
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void Validate_TamanhoDePagina_AceitaDeUmACem(int tamanho, bool valido)
    {
        _validator.Validate(new ListarPedidosQuery(PageSize: tamanho)).IsValid.ShouldBe(valido);
    }

    [Theory] // RN-16
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void Validate_ClienteId_ExigeInteiroPositivoQuandoInformado(int clienteId, bool valido)
    {
        _validator.Validate(new ListarPedidosQuery(ClienteId: clienteId)).IsValid.ShouldBe(valido);
    }

    [Fact] // CA-17
    public void Validate_PeriodoValido_NaoTemErros()
    {
        _validator.Validate(new ListarPedidosQuery(DataInicio: "2026-03-01", DataFim: "2026-03-31")).IsValid.ShouldBeTrue();
    }

    [Fact] // HL-18: cada ponta e opcional
    public void Validate_SoUmaDasDatas_NaoTemErros()
    {
        _validator.Validate(new ListarPedidosQuery(DataInicio: "2026-03-01")).IsValid.ShouldBeTrue();
        _validator.Validate(new ListarPedidosQuery(DataFim: "2026-03-31")).IsValid.ShouldBeTrue();
    }

    [Fact] // RN-15
    public void Validate_InicioMaiorQueFim_RetornaErroNoCampoDataFim()
    {
        var resultado = _validator.Validate(new ListarPedidosQuery(DataInicio: "2026-04-01", DataFim: "2026-03-31"));

        Campos(resultado).ShouldBe(["DataFim"]);
    }

    [Fact] // SEC-03, RN-15
    public void Validate_DataFimNoMaximoDoCalendario_RetornaErroNoCampoDataFim()
    {
        Campos(_validator.Validate(new ListarPedidosQuery(DataFim: "9999-12-31"))).ShouldBe(["DataFim"]);
    }

    [Fact] // RN-15: inicio = fim e valido
    public void Validate_InicioIgualAoFim_NaoTemErros()
    {
        _validator.Validate(new ListarPedidosQuery(DataInicio: "2026-03-31", DataFim: "2026-03-31")).IsValid.ShouldBeTrue();
    }
}
