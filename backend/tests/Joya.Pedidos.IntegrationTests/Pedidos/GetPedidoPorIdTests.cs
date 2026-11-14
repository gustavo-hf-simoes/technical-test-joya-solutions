using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Joya.Pedidos.IntegrationTests.Api;
using Joya.Pedidos.IntegrationTests.Infra;
using Joya.Pedidos.IntegrationTests.Persistencia;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Pedidos;

/// <summary><c>GET /pedidos/{id}</c> (spec 5.2, RN-09, RN-13) contra o SQL Server real. Os dados são criados em cada teste.</summary>
[Collection(SqlServerCollection.Nome)]
public sealed class GetPedidoPorIdTests(SqlServerFixture banco) : IAsyncLifetime
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
    [Trait("CA", "CA-11")]
    public async Task GetPedidoPorId_PrecoDeTabelaAlterado_MantemPrecoPraticado()
    {
        var cliente = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Mercado Bom Preço", ativo: true, _ct);
        var produto = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 149.90m, 10, ativo: true, _ct);
        var outro = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Mouse Pad", 29.95m, 10, ativo: true, _ct);

        using var http = _factory.CreateClient();
        var criado = await http.PostAsJsonAsync(
            "/pedidos",
            new { clienteId = cliente, itens = new[] { new { produtoId = produto, quantidade = 2 }, new { produtoId = outro, quantidade = 1 } } },
            _ct);
        criado.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await criado.Content.ReadFromJsonAsync<JsonElement>(_ct)).GetProperty("id").GetInt32();

        await Sql.ExecutarAsync(
            banco.ConnectionStringSa, "UPDATE dbo.Produtos SET Preco = 199.90 WHERE Id = @id;", _ct, new SqlParameter("@id", produto));

        var resposta = await http.GetAsync($"/pedidos/{id}", _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        resposta.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(_ct);
        corpo.GetProperty("id").GetInt32().ShouldBe(id);
        corpo.GetProperty("clienteNome").GetString().ShouldBe("Mercado Bom Preço");
        corpo.GetProperty("valorTotal").GetDecimal().ShouldBe(329.75m);
        var itens = corpo.GetProperty("itens").EnumerateArray().ToList();
        itens.Count.ShouldBe(2);
        var item = itens.Single(i => i.GetProperty("produtoId").GetInt32() == produto);
        item.GetProperty("produtoNome").GetString().ShouldBe("Teclado");
        item.GetProperty("quantidade").GetInt32().ShouldBe(2);
        item.GetProperty("precoUnitario").GetDecimal().ShouldBe(149.90m);
        item.GetProperty("subtotal").GetDecimal().ShouldBe(299.80m);
    }

    [Fact]
    [Trait("CA", "CA-34")]
    public async Task GetPedidoPorId_Inexistente_Retorna404()
    {
        using var http = _factory.CreateClient();

        var resposta = await http.GetAsync("/pedidos/999999", _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        resposta.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(_ct);
        corpo.GetProperty("code").GetString().ShouldBe("PEDIDO_NAO_ENCONTRADO");
        corpo.GetProperty("status").GetInt32().ShouldBe(404);
    }

    [Fact]
    [Trait("RN", "RN-13")]
    public async Task GetPedidoPorId_AposCriacao_DataPedidoIgualAoRetornoDoPost()
    {
        var cliente = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Cliente", ativo: true, _ct);
        var produto = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Produto", 10m, 5, ativo: true, _ct);

        using var http = _factory.CreateClient();
        var criado = await http.PostAsJsonAsync(
            "/pedidos", new { clienteId = cliente, itens = new[] { new { produtoId = produto, quantidade = 1 } } }, _ct);
        var corpoPost = await criado.Content.ReadFromJsonAsync<JsonElement>(_ct);
        var id = corpoPost.GetProperty("id").GetInt32();

        var corpoGet = await http.GetFromJsonAsync<JsonElement>($"/pedidos/{id}", _ct);

        var dataPost = corpoPost.GetProperty("dataPedido").GetDateTime();
        var dataGet = corpoGet.GetProperty("dataPedido").GetDateTime();
        dataGet.ShouldBe(dataPost);
        corpoGet.GetProperty("dataPedido").GetString().ShouldBe(corpoPost.GetProperty("dataPedido").GetString());
    }
}
