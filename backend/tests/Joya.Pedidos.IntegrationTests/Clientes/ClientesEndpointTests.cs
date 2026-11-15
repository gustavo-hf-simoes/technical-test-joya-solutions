using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Joya.Pedidos.IntegrationTests.Fixtures;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Clientes;

/// <summary><c>GET /clientes</c> (spec 5.5, HL-18) contra o SQL Server real.</summary>
[Collection(SqlServerCollection.Nome)]
public sealed class ClientesEndpointTests(SqlServerFixture banco) : IAsyncLifetime
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
    public async Task GetClientes_FiltroAtivo_RetornaSoAtivosOrdenadosPorNome()
    {
        await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Zeta", ativo: true, _ct);
        await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Inativo", ativo: false, _ct);
        await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Alfa", ativo: true, _ct);

        var (status, itens) = await ObterAsync("/clientes?ativo=true");

        status.ShouldBe(HttpStatusCode.OK);
        itens.Select(i => i.GetProperty("nome").GetString()).ShouldBe(["Alfa", "Zeta"]);
        itens.ShouldAllBe(i => i.GetProperty("ativo").GetBoolean());
        itens[0].EnumerateObject().Select(p => p.Name).ShouldBe(["id", "nome", "ativo"]);
    }

    [Fact]
    public async Task GetClientes_SemFiltroAtivo_RetornaTodos()
    {
        await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "B ativo", ativo: true, _ct);
        await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "A inativo", ativo: false, _ct);

        var (status, itens) = await ObterAsync("/clientes");

        status.ShouldBe(HttpStatusCode.OK);
        itens.Select(i => i.GetProperty("nome").GetString()).ShouldBe(["A inativo", "B ativo"]);
    }

    [Fact]
    public async Task GetClientes_AtivoFalse_RetornaSoInativos()
    {
        await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Ativo", ativo: true, _ct);
        await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Inativo", ativo: false, _ct);

        var (_, itens) = await ObterAsync("/clientes?ativo=false");

        itens.Select(i => i.GetProperty("nome").GetString()).ShouldBe(["Inativo"]);
    }

    [Theory]
    [InlineData("/clientes?ativo=talvez")]
    public async Task GetClientes_AtivoInvalido_Retorna400(string rota)
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
