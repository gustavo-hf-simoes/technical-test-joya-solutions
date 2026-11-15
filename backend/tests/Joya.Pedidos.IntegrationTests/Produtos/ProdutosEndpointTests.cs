using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Joya.Pedidos.IntegrationTests.Fixtures;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Produtos;

/// <summary><c>GET /produtos</c> (spec 5.5, HL-18) contra o SQL Server real.</summary>
[Collection(SqlServerCollection.Nome)]
public sealed class ProdutosEndpointTests(SqlServerFixture banco) : IAsyncLifetime
{
    private static readonly CancellationToken _ct = CancellationToken.None;

    private readonly ApiFactory _factory = new(banco.ConnectionStringApp);

    public Task InitializeAsync() => banco.ResetAsync(_ct);

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("CA", "CA-35")]
    public async Task GetProdutos_FiltroAtivo_RetornaSoAtivosOrdenadosPorNome()
    {
        await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 149.90m, 10, ativo: true, _ct);
        await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Descontinuado", 5m, 0, ativo: false, _ct);
        await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Mouse", 29.95m, 5, ativo: true, _ct);

        var (status, itens) = await ObterAsync("/produtos?ativo=true");

        status.ShouldBe(HttpStatusCode.OK);
        itens.Select(i => i.GetProperty("nome").GetString()).ShouldBe(["Mouse", "Teclado"]);
        itens[0].GetProperty("preco").GetDecimal().ShouldBe(29.95m);
        itens[0].GetProperty("estoque").GetInt32().ShouldBe(5);
        itens[0].EnumerateObject().Select(p => p.Name).ShouldBe(["id", "sku", "nome", "preco", "estoque"]);
    }

    [Theory]
    [InlineData("/produtos?ativo=talvez")]
    public async Task GetProdutos_AtivoInvalido_Retorna400(string rota)
    {
        using var http = _factory.CreateClient();

        var resposta = await http.GetAsync(rota, _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(_ct);
        corpo.GetProperty("code").GetString().ShouldBe("VALIDACAO");
    }

    private async Task<(HttpStatusCode Status, List<JsonElement> Itens)> ObterAsync(string rota)
    {
        using var http = _factory.CreateClient();
        var resposta = await http.GetAsync(rota, _ct);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(_ct);
        return (resposta.StatusCode, [.. corpo.EnumerateArray()]);
    }
}
