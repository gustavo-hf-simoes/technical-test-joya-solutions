using System.Net;
using Joya.Pedidos.IntegrationTests.Infra;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Api;

/// <summary>Health checks de liveness e readiness (CA-36).</summary>
[Collection(SqlServerCollection.Nome)]
public sealed class HealthTests(SqlServerFixture banco)
{
    [Fact] // CA-36
    public async Task HealthLive_ProcessoNoAr_Retorna200()
    {
        // Banco inalcançável de propósito: liveness não depende dele.
        using var fabrica = new ApiFactory(ApiFactory.ConnectionStringInalcancavel);
        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/health/live");

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact] // CA-36
    public async Task HealthReady_BancoDisponivel_Retorna200()
    {
        using var fabrica = new ApiFactory(banco.ConnectionStringApp);
        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/health/ready");

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact] // CA-36
    public async Task HealthReady_BancoIndisponivel_Retorna503()
    {
        using var fabrica = new ApiFactory(ApiFactory.ConnectionStringInalcancavel);
        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/health/ready");

        resposta.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }
}
