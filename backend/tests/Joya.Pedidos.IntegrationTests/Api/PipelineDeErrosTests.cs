using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Api;

/// <summary>Problem Details, 500 e 415 (spec 5.1; CA-38). Não usam o banco.</summary>
public sealed class PipelineDeErrosTests : IDisposable
{
    private readonly ApiFactory _factory = new(ApiFactory.ConnectionStringInalcancavel);

    public void Dispose() => _factory.Dispose();

    [Fact] // CA-38
    public async Task Excecao_NaoTratada_Retorna500ProblemDetailsSemStackTrace()
    {
        using var cliente = _factory.CreateClient();

        var resposta = await cliente.GetAsync("/_teste/excecao");

        resposta.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        resposta.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var texto = await resposta.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(texto);
        json.RootElement.GetProperty("status").GetInt32().ShouldBe(500);
        json.RootElement.GetProperty("code").GetString().ShouldBe("ERRO_INTERNO");
        json.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        texto.ShouldNotContain("segredo-que-nao-pode-vazar");
        texto.ShouldNotContain("InvalidOperationException");
        texto.ShouldNotContain(" at ");
        texto.ShouldNotContain("stackTrace", Case.Insensitive);
    }

    [Fact] // spec 5.1: Content-Type inválido no POST
    public async Task Post_ContentTypeTexto_Retorna415()
    {
        using var cliente = _factory.CreateClient();
        using var conteudo = new StringContent("{\"nome\":\"x\"}", Encoding.UTF8, "text/plain");

        var resposta = await cliente.PostAsync("/_teste/eco", conteudo);

        resposta.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        resposta.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var problema = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        problema.GetProperty("status").GetInt32().ShouldBe(415);
        problema.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Post_ContentTypeJson_Retorna200()
    {
        using var cliente = _factory.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/_teste/eco", new CorpoDeTeste("x"));

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Swagger_AmbienteDeTeste_PublicaODocumento()
    {
        using var cliente = _factory.CreateClient();

        var resposta = await cliente.GetAsync("/swagger/v1/swagger.json");

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
