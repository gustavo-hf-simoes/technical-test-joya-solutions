using Joya.Pedidos.Core.Common;
using Joya.Pedidos.Core.Pedidos;
using NSubstitute;
using Shouldly;

namespace Joya.Pedidos.UnitTests.Pedidos;

public sealed class PedidoConsultaServiceTests
{
    private readonly IPedidoConsultas _consultas = Substitute.For<IPedidoConsultas>();

    private PedidoConsultaService CriarSut() => new(_consultas);

    [Fact] // CA-17, RN-15
    public async Task ListarAsync_PeriodoInformado_ConsultaComFimExclusivoNoDiaSeguinte()
    {
        var pagina = new PaginaResponse<PedidoResumoResponse>([], 2, 10, 0);
        _consultas.ListarAsync(7, Arg.Any<Periodo>(), 2, 10, Arg.Any<CancellationToken>()).Returns(pagina);

        var resultado = await CriarSut().ListarAsync(
            new ListarPedidosQuery(7, "2026-03-01", "2026-03-31", 2, 10), CancellationToken.None);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.ShouldBe(pagina);
        await _consultas.Received(1).ListarAsync(
            7,
            new Periodo(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)),
            2,
            10,
            Arg.Any<CancellationToken>());
    }

    [Fact] // CA-15, RN-16: sem filtro
    public async Task ListarAsync_SemFiltros_ConsultaComPeriodoAberto()
    {
        _consultas.ListarAsync(null, Arg.Any<Periodo>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PaginaResponse<PedidoResumoResponse>([], 1, 20, 0));

        var resultado = await CriarSut().ListarAsync(new ListarPedidosQuery(), CancellationToken.None);

        resultado.IsSuccess.ShouldBeTrue();
        await _consultas.Received(1).ListarAsync(null, new Periodo(null, null), 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact] // CA-19, RN-15
    public async Task ListarAsync_InicioMaiorQueFim_RetornaValidacaoSemConsultar()
    {
        var resultado = await CriarSut().ListarAsync(
            new ListarPedidosQuery(DataInicio: "2026-04-01", DataFim: "2026-03-31"), CancellationToken.None);

        resultado.IsFailure.ShouldBeTrue();
        resultado.Error.Code.ShouldBe("VALIDACAO");
        await _consultas.DidNotReceiveWithAnyArgs().ListarAsync(default, Arg.Any<Periodo>(), default, default, default);
    }

    [Fact] // regra de ouro 3
    public async Task ListarAsync_Cancelamento_RepassaOTokenParaAConsulta()
    {
        using var cts = new CancellationTokenSource();
        _consultas.ListarAsync(null, Arg.Any<Periodo>(), 1, 20, cts.Token)
            .Returns(new PaginaResponse<PedidoResumoResponse>([], 1, 20, 0));

        await CriarSut().ListarAsync(new ListarPedidosQuery(), cts.Token);

        await _consultas.Received(1).ListarAsync(null, Arg.Any<Periodo>(), 1, 20, cts.Token);
    }

    [Fact] // CA-34
    public async Task ObterPorIdAsync_Inexistente_RetornaPedidoNaoEncontrado()
    {
        _consultas.ObterPorIdAsync(999999, Arg.Any<CancellationToken>()).Returns((PedidoResponse?)null);

        var resultado = await CriarSut().ObterPorIdAsync(999999, CancellationToken.None);

        resultado.IsFailure.ShouldBeTrue();
        resultado.Error.Code.ShouldBe("PEDIDO_NAO_ENCONTRADO");
        resultado.Error.Tipo.ShouldBe(TipoErro.NaoEncontrado);
    }

    [Fact] // CA-34
    public async Task ObterPorIdAsync_Existente_RetornaOPedidoComItens()
    {
        var pedido = new PedidoResponse(
            1042, 7, "Cliente", new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc), 299.80m,
            [new PedidoItemResponse(12, "Teclado", 2, 149.90m, 299.80m)]);
        _consultas.ObterPorIdAsync(1042, Arg.Any<CancellationToken>()).Returns(pedido);

        var resultado = await CriarSut().ObterPorIdAsync(1042, CancellationToken.None);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.ShouldBe(pedido);
    }
}
