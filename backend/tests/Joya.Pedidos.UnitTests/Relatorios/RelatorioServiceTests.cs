using Joya.Pedidos.Core.Relatorios;
using NSubstitute;
using Shouldly;

namespace Joya.Pedidos.UnitTests.Relatorios;

public sealed class RelatorioServiceTests
{
    private static readonly DateTime _inicio2026 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _inicio2027 = new(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IRelatorioConsultas _consultas = Substitute.For<IRelatorioConsultas>();

    private RelatorioService CriarSut() => new(_consultas);

    [Fact] // CA-21, CA-22, RN-15, RN-17
    public async Task TopClientesAsync_PeriodoValido_ConsultaCincoComFimExclusivoNoDiaSeguinte()
    {
        IReadOnlyList<TopClienteResponse> ranking =
        [
            new(1, 3, "Atacado Central S.A.", 184_250.40m, 61),
            new(2, 7, "Mercado Bom Preço Ltda", 97_310.00m, 44),
        ];
        _consultas.TopClientesAsync(_inicio2026, _inicio2027, 5, Arg.Any<CancellationToken>()).Returns(ranking);

        var resultado = await CriarSut().TopClientesAsync(new TopClientesQuery("2026-01-01", "2026-12-31"), CancellationToken.None);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.ShouldBe(ranking);
        RelatorioService.TamanhoRanking.ShouldBe(5);
        await _consultas.Received(1).TopClientesAsync(_inicio2026, _inicio2027, RelatorioService.TamanhoRanking, Arg.Any<CancellationToken>());
    }

    [Fact] // CA-25: menos clientes que o tamanho do ranking é sucesso, não erro
    public async Task TopClientesAsync_ConsultaVazia_RetornaListaVazia()
    {
        _consultas.TopClientesAsync(default, default, default, default).ReturnsForAnyArgs([]);

        var resultado = await CriarSut().TopClientesAsync(new TopClientesQuery("2026-01-01", "2026-01-31"), CancellationToken.None);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.ShouldBeEmpty();
    }

    [Theory] // CA-26, RN-17: defesa do caso de uso, mesmo que o validator não rode
    [InlineData(null, "2026-12-31")]
    [InlineData("2026-01-01", null)]
    [InlineData(null, null)]
    [InlineData("01/01/2026", "2026-12-31")]
    public async Task TopClientesAsync_SemPeriodoCompleto_RetornaValidacaoSemConsultar(string? dataInicio, string? dataFim)
    {
        var resultado = await CriarSut().TopClientesAsync(new TopClientesQuery(dataInicio, dataFim), CancellationToken.None);

        resultado.IsFailure.ShouldBeTrue();
        resultado.Error.Code.ShouldBe("VALIDACAO");
        resultado.Error.ShouldBe(RelatorioErrors.PeriodoObrigatorio());
        await _consultas.DidNotReceiveWithAnyArgs().TopClientesAsync(default, default, default, default);
    }

    [Fact] // RN-15
    public async Task TopClientesAsync_InicioMaiorQueFim_RetornaValidacaoSemConsultar()
    {
        var resultado = await CriarSut().TopClientesAsync(new TopClientesQuery("2026-04-01", "2026-03-31"), CancellationToken.None);

        resultado.IsFailure.ShouldBeTrue();
        resultado.Error.Code.ShouldBe("VALIDACAO");
        await _consultas.DidNotReceiveWithAnyArgs().TopClientesAsync(default, default, default, default);
    }

    [Fact] // regra de ouro 3
    public async Task TopClientesAsync_Cancelamento_RepassaOTokenParaAConsulta()
    {
        using var cts = new CancellationTokenSource();
        _consultas.TopClientesAsync(_inicio2026, _inicio2027, 5, cts.Token).Returns([]);

        await CriarSut().TopClientesAsync(new TopClientesQuery("2026-01-01", "2026-12-31"), cts.Token);

        await _consultas.Received(1).TopClientesAsync(_inicio2026, _inicio2027, 5, cts.Token);
    }
}
