using FluentValidation.Results;
using Joya.Pedidos.Core.Pedidos;
using Shouldly;

namespace Joya.Pedidos.UnitTests.Pedidos;

public sealed class CriarPedidoValidatorTests
{
    private readonly CriarPedidoValidator _validator = new();

    private static CriarPedidoRequest Valido() => new(7, [new CriarPedidoItemRequest(12, 2)]);

    private static List<string> Campos(ValidationResult resultado) =>
        [.. resultado.Errors.Select(e => e.PropertyName)];

    [Fact]
    public void Validate_PedidoValido_NaoTemErros()
    {
        _validator.Validate(Valido()).IsValid.ShouldBeTrue();
    }

    [Fact] // CA-02, RN-01
    public void Validate_ItensVazios_RetornaErroNoCampoItens()
    {
        var resultado = _validator.Validate(new CriarPedidoRequest(7, []));

        Campos(resultado).ShouldContain("Itens");
        resultado.Errors.ShouldContain(e => e.ErrorMessage == "O pedido deve ter ao menos um item.");
    }

    [Fact] // CA-02, RN-01: lista ausente
    public void Validate_ItensAusentes_RetornaErroNoCampoItens()
    {
        var resultado = _validator.Validate(new CriarPedidoRequest(7, null));

        Campos(resultado).ShouldContain("Itens");
    }

    [Fact] // RN-01: 50 é o máximo
    public void Validate_CinquentaItens_NaoTemErros()
    {
        var itens = Enumerable.Range(1, CriarPedidoValidator.MaximoItens)
            .Select(i => new CriarPedidoItemRequest(i, 1))
            .ToList();

        _validator.Validate(new CriarPedidoRequest(7, itens)).IsValid.ShouldBeTrue();
    }

    [Fact] // RN-01
    public void Validate_MaisDeCinquentaItens_RetornaErroNoCampoItens()
    {
        var itens = Enumerable.Range(1, CriarPedidoValidator.MaximoItens + 1)
            .Select(i => new CriarPedidoItemRequest(i, 1))
            .ToList();

        var resultado = _validator.Validate(new CriarPedidoRequest(7, itens));

        Campos(resultado).ShouldBe(["Itens"]);
    }

    [Theory] // CA-03, RN-02
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(CriarPedidoValidator.QuantidadeMaxima + 1)]
    public void Validate_QuantidadeForaDoLimite_RetornaErroNaQuantidade(int quantidade)
    {
        var resultado = _validator.Validate(new CriarPedidoRequest(7, [new CriarPedidoItemRequest(12, 1), new CriarPedidoItemRequest(13, quantidade)]));

        Campos(resultado).ShouldBe(["Itens[1].Quantidade"]);
        resultado.Errors[0].ErrorMessage.ShouldBe($"A quantidade deve estar entre 1 e {CriarPedidoValidator.QuantidadeMaxima}.");
    }

    [Theory] // RN-02: limites aceitos
    [InlineData(CriarPedidoValidator.QuantidadeMinima)]
    [InlineData(CriarPedidoValidator.QuantidadeMaxima)]
    public void Validate_QuantidadeNosLimites_NaoTemErros(int quantidade)
    {
        _validator.Validate(new CriarPedidoRequest(7, [new CriarPedidoItemRequest(12, quantidade)])).IsValid.ShouldBeTrue();
    }

    [Fact] // CA-04, RN-03
    public void Validate_ProdutoRepetido_RetornaErroDeDuplicidade()
    {
        var resultado = _validator.Validate(
            new CriarPedidoRequest(7, [new CriarPedidoItemRequest(12, 1), new CriarPedidoItemRequest(12, 2)]));

        Campos(resultado).ShouldBe(["Itens"]);
        resultado.Errors[0].ErrorMessage.ShouldBe("Um produto não pode aparecer em mais de um item do pedido.");
    }

    [Theory] // RN-04
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ClienteNaoPositivo_RetornaErroNoClienteId(int clienteId)
    {
        var resultado = _validator.Validate(new CriarPedidoRequest(clienteId, [new CriarPedidoItemRequest(12, 1)]));

        Campos(resultado).ShouldBe(["ClienteId"]);
    }

    [Theory] // RN-04
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ProdutoNaoPositivo_RetornaErroNoProdutoId(int produtoId)
    {
        var resultado = _validator.Validate(new CriarPedidoRequest(7, [new CriarPedidoItemRequest(produtoId, 1)]));

        Campos(resultado).ShouldBe(["Itens[0].ProdutoId"]);
    }
}
