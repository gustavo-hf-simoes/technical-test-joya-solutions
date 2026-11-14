using Joya.Pedidos.Core.Common;
using Shouldly;

namespace Joya.Pedidos.UnitTests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Failure_ErroInformado_CarregaCodigoSemExcecao()
    {
        var erro = Error.Unprocessable("CLIENTE_NAO_ENCONTRADO", "O cliente 9 não existe.");

        var resultado = Should.NotThrow(() => Result<int>.Failure(erro));

        resultado.IsFailure.ShouldBeTrue();
        resultado.IsSuccess.ShouldBeFalse();
        resultado.Error.Code.ShouldBe("CLIENTE_NAO_ENCONTRADO");
        resultado.Error.Tipo.ShouldBe(TipoErro.RegraDeNegocio);
        Should.Throw<InvalidOperationException>(() => resultado.Value);
    }

    [Fact]
    public void ConversaoImplicita_Valor_RetornaSucesso()
    {
        Result<string> resultado = "ok";

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.ShouldBe("ok");
        Should.Throw<InvalidOperationException>(() => resultado.Error);
    }

    [Fact]
    public void ConversaoImplicita_Erro_RetornaFalha()
    {
        Result<string> resultado = Error.Conflict("ESTOQUE_INSUFICIENTE", "Sem estoque.");

        resultado.IsFailure.ShouldBeTrue();
        resultado.Error.Tipo.ShouldBe(TipoErro.Conflito);
    }

    [Fact]
    public void Match_Falha_ExecutaRamoDoErro()
    {
        Result<int> resultado = Error.NotFound("PEDIDO_NAO_ENCONTRADO", "Pedido inexistente.");

        var texto = resultado.Match(v => $"ok {v}", e => e.Code);

        texto.ShouldBe("PEDIDO_NAO_ENCONTRADO");
    }

    [Fact]
    public void With_ExtensaoInformada_PreservaOriginalEAcrescentaCopia()
    {
        var original = Error.Conflict("ESTOQUE_INSUFICIENTE", "Sem estoque.");

        var comExtensao = original.With("itens", new[] { 1, 2 });

        original.Extensoes.ShouldBeEmpty();
        comExtensao.Extensoes.ContainsKey("itens").ShouldBeTrue();
        comExtensao.Code.ShouldBe(original.Code);
    }

    [Theory]
    [InlineData(75, 20, 4)]
    [InlineData(80, 20, 4)]
    [InlineData(81, 20, 5)]
    [InlineData(0, 20, 0)]
    [InlineData(1, 100, 1)]
    public void PaginaResponse_TotalItems_CalculaTotalPages(int totalItems, int pageSize, int esperado)
    {
        var pagina = new PaginaResponse<int>([], 1, pageSize, totalItems);

        pagina.TotalPages.ShouldBe(esperado);
    }
}
