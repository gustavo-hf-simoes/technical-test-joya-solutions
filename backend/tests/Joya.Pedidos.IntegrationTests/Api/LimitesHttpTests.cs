using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Joya.Pedidos.Api.Limites;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Api;

/// <summary>
/// Limites contra abuso (RNF-06, HL-12, HL-19, ADR-0002): <c>413</c> para corpo acima de 32 KiB e
/// <c>429 LIMITE_DE_TAXA</c> por IP no <c>POST /pedidos</c>. Não usam o banco.
/// </summary>
public sealed class LimitesHttpTests : IDisposable
{
    /// <summary>
    /// Rota que leva as proteções do <c>POST /pedidos</c>. Até a T-06.4 é o espelho só de teste em
    /// <see cref="RotasDeTesteController"/>; a T-06.4 troca para <c>/pedidos</c> e remove o espelho.
    /// </summary>
    private const string _rotaCriarPedido = "/_teste/pedidos";

    private const string _rotaSemPolitica = "/_teste/eco";

    /// <summary>Janela longa nos testes de 429: a cota não renova no meio do teste.</summary>
    private const int _janelaDeTesteSegundos = 60;

    private readonly ApiFactory _factory = new(ApiFactory.ConnectionStringInalcancavel);

    public void Dispose() => _factory.Dispose();

    [Fact] // RNF-06, ADR-0002 item 1
    public async Task PostPedidos_CorpoAcimaDoLimite_Retorna413()
    {
        using var cliente = _factory.CreateClient();
        using var conteudo = CorpoJson(LimitesHttp.CorpoMaximoBytes + 1);
        conteudo.Headers.ContentLength.ShouldBe(LimitesHttp.CorpoMaximoBytes + 1);

        var resposta = await cliente.PostAsync(_rotaCriarPedido, conteudo);

        resposta.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
        resposta.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var texto = await resposta.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(texto);
        json.RootElement.GetProperty("status").GetInt32().ShouldBe(413);
        json.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        texto.ShouldNotContain("Exception");
        texto.ShouldNotContain(" at ");
    }

    [Fact] // RNF-06, ADR-0002 item 1: corpo sem Content-Length (chunked) é barrado pelo Kestrel
    public void Kestrel_LimiteDoCorpo_IgualAoCorpoMaximo()
    {
        // O TestServer não passa pelo Kestrel; o teste prova que a proteção contra chunked está configurada.
        var kestrel = _factory.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value;

        kestrel.Limits.MaxRequestBodySize.ShouldBe(LimitesHttp.CorpoMaximoBytes);
    }

    [Fact] // RNF-06: controle do 413, o limite exato ainda é aceito
    public async Task PostPedidos_CorpoNoLimite_Retorna200()
    {
        using var cliente = _factory.CreateClient();
        using var conteudo = CorpoJson(LimitesHttp.CorpoMaximoBytes);

        var resposta = await cliente.PostAsync(_rotaCriarPedido, conteudo);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact] // RNF-06, HL-19, ADR-0002 item 2
    public async Task PostPedidos_RajadaAcimaDoLimite_Retorna429()
    {
        using var fabrica = ComLimiteDeTaxa(permissoes: 2);
        using var cliente = fabrica.CreateClient();

        var primeira = await cliente.PostAsJsonAsync(_rotaCriarPedido, new CorpoDeTeste("1"));
        var segunda = await cliente.PostAsJsonAsync(_rotaCriarPedido, new CorpoDeTeste("2"));
        var terceira = await cliente.PostAsJsonAsync(_rotaCriarPedido, new CorpoDeTeste("3"));

        primeira.StatusCode.ShouldBe(HttpStatusCode.OK);
        segunda.StatusCode.ShouldBe(HttpStatusCode.OK);
        terceira.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        terceira.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var problema = await terceira.Content.ReadFromJsonAsync<JsonElement>();
        problema.GetProperty("status").GetInt32().ShouldBe(429);
        problema.GetProperty("code").GetString().ShouldBe(LimiteCriarPedidoPolicy.Codigo);
        problema.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        var retryAfter = terceira.Headers.RetryAfter?.Delta;
        retryAfter.ShouldNotBeNull();
        retryAfter.Value.TotalSeconds.ShouldBeInRange(1, _janelaDeTesteSegundos);
    }

    [Fact] // ADR-0002 itens 2 e 3: a cota é por IP, lido do X-Forwarded-For deixado pelo proxy
    public async Task PostPedidos_IpsDistintos_TemCotasSeparadas()
    {
        using var fabrica = ComLimiteDeTaxa(permissoes: 1);
        using var cliente = fabrica.CreateClient();

        var ipA = await PostDoIpAsync(cliente, "203.0.113.10");
        var ipB = await PostDoIpAsync(cliente, "203.0.113.20");
        var ipADeNovo = await PostDoIpAsync(cliente, "203.0.113.10");

        ipA.StatusCode.ShouldBe(HttpStatusCode.OK);
        ipB.StatusCode.ShouldBe(HttpStatusCode.OK);
        ipADeNovo.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact] // ADR-0002 item 2: a política vale só onde é aplicada
    public async Task Post_RotaSemPolitica_NaoSofreLimiteDeTaxa()
    {
        using var fabrica = ComLimiteDeTaxa(permissoes: 1);
        using var cliente = fabrica.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var resposta = await cliente.PostAsJsonAsync(_rotaSemPolitica, new CorpoDeTeste("x"));
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    [Fact] // HL-19: o ambiente de teste eleva o limite; uma rajada como a do CA-10 não recebe 429
    public async Task PostPedidos_AmbienteDeTesteComRajadaParalela_NaoRetorna429()
    {
        using var cliente = _factory.CreateClient();

        var respostas = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(i => cliente.PostAsJsonAsync(_rotaCriarPedido, new CorpoDeTeste(i.ToString(CultureInfo.InvariantCulture)))));

        respostas.ShouldAllBe(resposta => resposta.StatusCode == HttpStatusCode.OK);
    }

    private WebApplicationFactory<Program> ComLimiteDeTaxa(int permissoes) =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuracao) =>
            configuracao.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{LimiteDeTaxaOptions.Secao}:Permissoes"] = permissoes.ToString(CultureInfo.InvariantCulture),
                [$"{LimiteDeTaxaOptions.Secao}:JanelaSegundos"] = _janelaDeTesteSegundos.ToString(CultureInfo.InvariantCulture),
            })));

    private static async Task<HttpResponseMessage> PostDoIpAsync(HttpClient cliente, string ip)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, _rotaCriarPedido)
        {
            Content = JsonContent.Create(new CorpoDeTeste(ip)),
        };
        requisicao.Headers.Add("X-Forwarded-For", ip);
        return await cliente.SendAsync(requisicao);
    }

    /// <summary>JSON válido de <c>CorpoDeTeste</c> com exatamente <paramref name="tamanhoEmBytes"/> bytes (ASCII).</summary>
    private static string Json(int tamanhoEmBytes)
    {
        const string Moldura = "{\"nome\":\"\"}";
        return $"{{\"nome\":\"{new string('a', tamanhoEmBytes - Moldura.Length)}\"}}";
    }

    private static StringContent CorpoJson(int tamanhoEmBytes) =>
        new(Json(tamanhoEmBytes), Encoding.UTF8, "application/json");
}
