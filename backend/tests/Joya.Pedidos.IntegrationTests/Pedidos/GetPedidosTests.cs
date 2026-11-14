using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Joya.Pedidos.IntegrationTests.Api;
using Joya.Pedidos.IntegrationTests.Infra;
using Joya.Pedidos.IntegrationTests.Persistencia;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Pedidos;

/// <summary><c>GET /pedidos</c> (spec 5.3, RN-15, RN-16) contra o SQL Server real. Os dados são criados em cada teste.</summary>
[Collection(SqlServerCollection.Nome)]
public sealed class GetPedidosTests(SqlServerFixture banco) : IAsyncLifetime
{
    private static readonly CancellationToken _ct = CancellationToken.None;

    private readonly ApiFactory _factory = new(banco.ConnectionStringApp);

    public Task InitializeAsync() => banco.ResetAsync(_ct);

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static DateTime Utc(int ano, int mes, int dia, int hora = 0, int minuto = 0, int segundo = 0, int ms = 0) =>
        new(ano, mes, dia, hora, minuto, segundo, ms, DateTimeKind.Utc);

    private async Task<int> ClienteAsync(string nome) =>
        await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, nome, ativo: true, _ct);

    private async Task<int> ProdutoAsync(string nome = "Produto", decimal preco = 10m) =>
        await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, nome, preco, 100, ativo: true, _ct);

    private async Task<int> PedidoAsync(int clienteId, DateTime data, int produtoId, decimal preco = 10m, int quantidade = 1) =>
        await Arranjo.InserirPedidoAsync(banco.ConnectionStringSa, clienteId, data, [(produtoId, quantidade, preco)], _ct);

    private async Task<(HttpStatusCode Status, JsonElement Corpo)> ObterAsync(string rota)
    {
        using var http = _factory.CreateClient();
        var resposta = await http.GetAsync(rota, _ct);
        return (resposta.StatusCode, await resposta.Content.ReadFromJsonAsync<JsonElement>(_ct));
    }

    private static List<int> Ids(JsonElement corpo) =>
        [.. corpo.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32())];

    [Fact]
    [Trait("CA", "CA-15")]
    public async Task GetPedidos_SemFiltro_RetornaPrimeiraPaginaOrdenada()
    {
        var cliente = await ClienteAsync("Mercado Bom Preço Ltda");
        var produto = await ProdutoAsync();
        var criados = new List<int>();
        for (var dia = 1; dia <= 25; dia++)
        {
            criados.Add(await PedidoAsync(cliente, Utc(2026, 5, dia, 10), produto));
        }

        // Mesmo instante que o dia 25: o desempate é o Id decrescente (RN-16).
        var empatado = await PedidoAsync(cliente, Utc(2026, 5, 25, 10), produto);

        var (status, corpo) = await ObterAsync("/pedidos");

        status.ShouldBe(HttpStatusCode.OK);
        corpo.GetProperty("page").GetInt32().ShouldBe(1);
        corpo.GetProperty("pageSize").GetInt32().ShouldBe(20);
        corpo.GetProperty("totalItems").GetInt32().ShouldBe(26);
        corpo.GetProperty("totalPages").GetInt32().ShouldBe(2);
        var ids = Ids(corpo);
        ids.Count.ShouldBe(20);
        ids[0].ShouldBe(empatado);
        ids[1].ShouldBe(criados[24]);
        ids.ShouldBe([empatado, .. Enumerable.Range(0, 25).Reverse().Select(i => criados[i]).Take(19)]);
        var primeiro = corpo.GetProperty("items")[0];
        primeiro.EnumerateObject().Select(p => p.Name)
            .ShouldBe(["id", "clienteId", "clienteNome", "dataPedido", "valorTotal", "quantidadeItens"]);
        primeiro.GetProperty("clienteNome").GetString().ShouldBe("Mercado Bom Preço Ltda");
        primeiro.GetProperty("dataPedido").GetString().ShouldBe("2026-05-25T10:00:00Z");
    }

    [Fact]
    [Trait("CA", "CA-16")]
    public async Task GetPedidos_FiltroCliente_RetornaSoDoCliente()
    {
        var alvo = await ClienteAsync("Alvo");
        var outro = await ClienteAsync("Outro");
        var produto = await ProdutoAsync();
        var p1 = await PedidoAsync(alvo, Utc(2026, 3, 1), produto);
        var p2 = await PedidoAsync(alvo, Utc(2026, 3, 2), produto);
        await PedidoAsync(outro, Utc(2026, 3, 3), produto);

        var (status, corpo) = await ObterAsync($"/pedidos?clienteId={alvo}");

        status.ShouldBe(HttpStatusCode.OK);
        Ids(corpo).ShouldBe([p2, p1]);
        corpo.GetProperty("totalItems").GetInt32().ShouldBe(2);
        corpo.GetProperty("items").EnumerateArray().ShouldAllBe(i => i.GetProperty("clienteId").GetInt32() == alvo);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    public async Task GetPedidos_FiltroPeriodo_IncluiAsDuasPontas()
    {
        var cliente = await ClienteAsync("Cliente");
        var produto = await ProdutoAsync();
        await PedidoAsync(cliente, Utc(2026, 2, 28, 23, 59, 59, 999), produto);
        var inicio = await PedidoAsync(cliente, Utc(2026, 3, 1), produto);
        var meio = await PedidoAsync(cliente, Utc(2026, 3, 15, 12), produto);
        var fim = await PedidoAsync(cliente, Utc(2026, 3, 31, 23, 59, 59, 999), produto);
        await PedidoAsync(cliente, Utc(2026, 4, 1), produto);

        var (status, corpo) = await ObterAsync("/pedidos?dataInicio=2026-03-01&dataFim=2026-03-31");

        status.ShouldBe(HttpStatusCode.OK);
        Ids(corpo).ShouldBe([fim, meio, inicio]);
        corpo.GetProperty("totalItems").GetInt32().ShouldBe(3);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    public async Task GetPedidos_SoUmaPontaDoPeriodo_FiltraSoNaquelaPonta()
    {
        var cliente = await ClienteAsync("Cliente");
        var produto = await ProdutoAsync();
        var antes = await PedidoAsync(cliente, Utc(2026, 1, 10), produto);
        var depois = await PedidoAsync(cliente, Utc(2026, 6, 10), produto);

        Ids((await ObterAsync("/pedidos?dataInicio=2026-03-01")).Corpo).ShouldBe([depois]);
        Ids((await ObterAsync("/pedidos?dataFim=2026-03-01")).Corpo).ShouldBe([antes]);
    }

    [Fact]
    [Trait("CA", "CA-18")]
    public async Task GetPedidos_PercorreTodasAsPaginas_NaoRepeteNemOmite()
    {
        var cliente = await ClienteAsync("Cliente");
        var produto = await ProdutoAsync();
        var esperados = new List<int>();
        for (var i = 0; i < 23; i++)
        {
            // Datas repetidas de propósito (3 por instante): só o desempate por Id mantém a paginação estável.
            esperados.Add(await PedidoAsync(cliente, Utc(2026, 7, 1 + (i / 3)), produto));
        }

        var vistos = new List<int>();
        for (var pagina = 1; pagina <= 3; pagina++)
        {
            var (_, corpo) = await ObterAsync($"/pedidos?page={pagina}&pageSize=10");
            corpo.GetProperty("totalPages").GetInt32().ShouldBe(3);
            vistos.AddRange(Ids(corpo));
        }

        vistos.Count.ShouldBe(23);
        vistos.Distinct().Count().ShouldBe(23);
        vistos.ShouldBe([.. esperados.OrderByDescending(id => id)]);
    }

    [Fact]
    [Trait("CA", "CA-18")]
    public async Task GetPedidos_PaginaAlemDoFim_Retorna200ComItensVaziosETotaisCorretos()
    {
        var cliente = await ClienteAsync("Cliente");
        var produto = await ProdutoAsync();
        await PedidoAsync(cliente, Utc(2026, 3, 1), produto);

        var (status, corpo) = await ObterAsync("/pedidos?page=5&pageSize=10");
        var (statusEnorme, corpoEnorme) = await ObterAsync($"/pedidos?page={int.MaxValue}&pageSize=100");

        status.ShouldBe(HttpStatusCode.OK);
        corpo.GetProperty("items").GetArrayLength().ShouldBe(0);
        corpo.GetProperty("totalItems").GetInt32().ShouldBe(1);
        corpo.GetProperty("totalPages").GetInt32().ShouldBe(1);
        statusEnorme.ShouldBe(HttpStatusCode.OK);
        corpoEnorme.GetProperty("items").GetArrayLength().ShouldBe(0);
    }

    [Theory]
    [Trait("CA", "CA-19")]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("page=abc")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("clienteId=0")]
    [InlineData("clienteId=abc")]
    [InlineData("dataInicio=2026-13-01")]
    [InlineData("dataInicio=11/13/2026")]
    [InlineData("dataFim=ontem")]
    [InlineData("dataInicio=2026-04-01&dataFim=2026-03-31")]
    public async Task GetPedidos_ParametrosInvalidos_Retorna400(string consulta)
    {
        var (status, corpo) = await ObterAsync($"/pedidos?{consulta}");

        status.ShouldBe(HttpStatusCode.BadRequest);
        corpo.GetProperty("code").GetString().ShouldBe("VALIDACAO");
        corpo.GetProperty("errors").EnumerateObject().ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("CA", "CA-19")]
    public async Task GetPedidos_PageSizeNoLimite_Retorna200()
    {
        var (status, corpo) = await ObterAsync("/pedidos?pageSize=100");

        status.ShouldBe(HttpStatusCode.OK);
        corpo.GetProperty("pageSize").GetInt32().ShouldBe(100);
    }

    [Fact]
    [Trait("CA", "CA-20")]
    public async Task GetPedidos_ClienteSemPedidos_Retorna200Vazio()
    {
        var semPedidos = await ClienteAsync("Sem pedidos");

        var (statusExistente, existente) = await ObterAsync($"/pedidos?clienteId={semPedidos}");
        var (statusInexistente, inexistente) = await ObterAsync("/pedidos?clienteId=999999");

        statusExistente.ShouldBe(HttpStatusCode.OK);
        statusInexistente.ShouldBe(HttpStatusCode.OK);
        foreach (var corpo in new[] { existente, inexistente })
        {
            corpo.GetProperty("items").GetArrayLength().ShouldBe(0);
            corpo.GetProperty("totalItems").GetInt32().ShouldBe(0);
            corpo.GetProperty("totalPages").GetInt32().ShouldBe(0);
        }
    }

    [Fact]
    [Trait("CA", "CA-15")]
    public async Task GetPedidos_QuantidadeItens_ContaLinhasENaoSomaQuantidades()
    {
        var cliente = await ClienteAsync("Cliente");
        var teclado = await ProdutoAsync("Teclado", 149.90m);
        var mouse = await ProdutoAsync("Mouse", 29.95m);
        var id = await Arranjo.InserirPedidoAsync(
            banco.ConnectionStringSa, cliente, Utc(2026, 11, 13, 14, 32, 7, 123), [(teclado, 2, 149.90m), (mouse, 2, 29.95m)], _ct);

        var (_, corpo) = await ObterAsync("/pedidos");

        var item = corpo.GetProperty("items")[0];
        item.GetProperty("id").GetInt32().ShouldBe(id);
        item.GetProperty("quantidadeItens").GetInt32().ShouldBe(2);
        item.GetProperty("valorTotal").GetDecimal().ShouldBe(359.70m);
        item.GetProperty("dataPedido").GetString().ShouldBe("2026-11-13T14:32:07.123Z");
    }
}
