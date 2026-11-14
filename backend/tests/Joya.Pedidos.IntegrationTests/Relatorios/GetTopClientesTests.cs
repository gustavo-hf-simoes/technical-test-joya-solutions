using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Joya.Pedidos.IntegrationTests.Api;
using Joya.Pedidos.IntegrationTests.Infra;
using Joya.Pedidos.IntegrationTests.Persistencia;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Relatorios;

/// <summary><c>GET /relatorios/top-clientes</c> (spec 5.4, RN-15, RN-17) contra o SQL Server real. Dados criados em cada teste.</summary>
[Collection(SqlServerCollection.Nome)]
public sealed class GetTopClientesTests(SqlServerFixture banco) : IAsyncLifetime
{
    private const string _rota2026 = "/relatorios/top-clientes?dataInicio=2026-01-01&dataFim=2026-12-31";

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

    private async Task<int> ClienteAsync(string nome, bool ativo = true) =>
        await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, nome, ativo, _ct);

    private async Task<int> ProdutoAsync(decimal preco = 1m, int estoque = 100) =>
        await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Produto", preco, estoque, ativo: true, _ct);

    /// <summary>Pedido de um item só, com o valor dado (quantidade 1 × preço = valor).</summary>
    private async Task<int> PedidoAsync(int clienteId, DateTime data, int produtoId, decimal valor) =>
        await Arranjo.InserirPedidoAsync(banco.ConnectionStringSa, clienteId, data, [(produtoId, 1, valor)], _ct);

    private async Task<(HttpStatusCode Status, JsonElement Corpo)> ObterAsync(string rota)
    {
        using var http = _factory.CreateClient();
        var resposta = await http.GetAsync(rota, _ct);
        return (resposta.StatusCode, await resposta.Content.ReadFromJsonAsync<JsonElement>(_ct));
    }

    private static List<int> ClienteIds(JsonElement corpo) =>
        [.. corpo.EnumerateArray().Select(l => l.GetProperty("clienteId").GetInt32())];

    private static List<decimal> Valores(JsonElement corpo) =>
        [.. corpo.EnumerateArray().Select(l => l.GetProperty("valorTotal").GetDecimal())];

    private static List<int> Posicoes(JsonElement corpo) =>
        [.. corpo.EnumerateArray().Select(l => l.GetProperty("posicao").GetInt32())];

    [Fact]
    [Trait("CA", "CA-21")]
    public async Task GetTopClientes_OitoClientes_RetornaCincoEmOrdemDecrescente()
    {
        var produto = await ProdutoAsync();
        decimal[] valores = [300m, 800m, 100m, 700m, 200m, 600m, 400m, 500m];
        var clientes = new List<int>();
        for (var i = 0; i < valores.Length; i++)
        {
            var cliente = await ClienteAsync($"Cliente {i + 1}");
            clientes.Add(cliente);

            // Dois pedidos por cliente: o ranking soma os pedidos e conta quantos foram.
            await PedidoAsync(cliente, Utc(2026, 2, 1 + i), produto, valores[i] - 50m);
            await PedidoAsync(cliente, Utc(2026, 9, 1 + i), produto, 50m);
        }

        var (status, corpo) = await ObterAsync(_rota2026);

        status.ShouldBe(HttpStatusCode.OK);
        corpo.GetArrayLength().ShouldBe(5);
        Posicoes(corpo).ShouldBe([1, 2, 3, 4, 5]);
        Valores(corpo).ShouldBe([800m, 700m, 600m, 500m, 400m]);
        ClienteIds(corpo).ShouldBe([clientes[1], clientes[3], clientes[5], clientes[7], clientes[6]]);
        corpo.EnumerateArray().ShouldAllBe(l => l.GetProperty("quantidadePedidos").GetInt32() == 2);
        var primeiro = corpo[0];
        primeiro.EnumerateObject().Select(p => p.Name)
            .ShouldBe(["posicao", "clienteId", "clienteNome", "valorTotal", "quantidadePedidos"]);
        primeiro.GetProperty("clienteNome").GetString().ShouldBe("Cliente 2");
    }

    [Fact]
    [Trait("CA", "CA-22")]
    public async Task GetTopClientes_PedidosForaDoPeriodo_NaoContam()
    {
        var produto = await ProdutoAsync();
        var cliente = await ClienteAsync("Atacado Central S.A.");
        await PedidoAsync(cliente, Utc(2025, 6, 1), produto, 99_990m);
        await PedidoAsync(cliente, Utc(2025, 12, 31, 23, 59, 59, 999), produto, 10m);  // um milissegundo antes do início
        await PedidoAsync(cliente, Utc(2026, 1, 1), produto, 4m);                     // dataInicio é inclusiva
        await PedidoAsync(cliente, Utc(2026, 12, 31, 23, 59, 59, 999), produto, 6m);  // dataFim é inclusiva
        await PedidoAsync(cliente, Utc(2027, 1, 1), produto, 50_000m);                // fim exclusivo

        var (status, corpo) = await ObterAsync(_rota2026);

        status.ShouldBe(HttpStatusCode.OK);
        ClienteIds(corpo).ShouldBe([cliente]);
        Valores(corpo).ShouldBe([10.00m]);
        corpo[0].GetProperty("quantidadePedidos").GetInt32().ShouldBe(2);
    }

    [Fact]
    [Trait("CA", "CA-23")]
    public async Task GetTopClientes_Homonimos_NaoSomaNomesIguais()
    {
        var produto = await ProdutoAsync();
        var primeiroJoao = await ClienteAsync("João da Silva");
        var segundoJoao = await ClienteAsync("João da Silva");
        await PedidoAsync(primeiroJoao, Utc(2026, 3, 1), produto, 200m);
        await PedidoAsync(primeiroJoao, Utc(2026, 4, 1), produto, 300m);
        await PedidoAsync(segundoJoao, Utc(2026, 5, 1), produto, 300m);

        var (status, corpo) = await ObterAsync(_rota2026);

        status.ShouldBe(HttpStatusCode.OK);
        corpo.GetArrayLength().ShouldBe(2);
        corpo.EnumerateArray().ShouldAllBe(l => l.GetProperty("clienteNome").GetString() == "João da Silva");
        ClienteIds(corpo).ShouldBe([primeiroJoao, segundoJoao]);
        Valores(corpo).ShouldBe([500m, 300m]);
        Valores(corpo).ShouldNotContain(800m);
    }

    [Fact]
    [Trait("CA", "CA-24")]
    public async Task GetTopClientes_Empate_OrdenaPorMenorClienteId()
    {
        var produto = await ProdutoAsync();
        var clientes = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            clientes.Add(await ClienteAsync($"Empatado {i + 1}"));
        }

        // Seis empatados com 1.000,00, gravados do maior id para o menor: a ordem de inserção não decide nada.
        // O sexto (maior id) fica fora do TOP 5 pelo desempate, de forma determinística.
        foreach (var cliente in Enumerable.Reverse(clientes))
        {
            await PedidoAsync(cliente, Utc(2026, 6, 1), produto, 1_000m);
        }

        var (status, corpo) = await ObterAsync(_rota2026);
        var (_, repetido) = await ObterAsync(_rota2026);

        status.ShouldBe(HttpStatusCode.OK);
        ClienteIds(corpo).ShouldBe(clientes.Take(5));
        Valores(corpo).ShouldAllBe(v => v == 1_000m);
        Posicoes(corpo).ShouldBe([1, 2, 3, 4, 5]);
        ClienteIds(repetido).ShouldBe(ClienteIds(corpo));
    }

    [Fact]
    [Trait("CA", "CA-25")]
    public async Task GetTopClientes_MenosDeCinco_RetornaSoQuemComprou()
    {
        var produto = await ProdutoAsync();
        var a = await ClienteAsync("A");
        var b = await ClienteAsync("B");
        var fevereiro = await ClienteAsync("Só em fevereiro");
        await ClienteAsync("Nunca comprou");
        await PedidoAsync(a, Utc(2026, 1, 10), produto, 50m);
        await PedidoAsync(b, Utc(2026, 1, 31, 23, 59, 59, 999), produto, 70m);
        await PedidoAsync(fevereiro, Utc(2026, 2, 1), produto, 1_000m);

        var (status, corpo) = await ObterAsync("/relatorios/top-clientes?dataInicio=2026-01-01&dataFim=2026-01-31");
        var (statusVazio, vazio) = await ObterAsync("/relatorios/top-clientes?dataInicio=2020-01-01&dataFim=2020-12-31");

        status.ShouldBe(HttpStatusCode.OK);
        ClienteIds(corpo).ShouldBe([b, a]);
        Posicoes(corpo).ShouldBe([1, 2]);
        statusVazio.ShouldBe(HttpStatusCode.OK);
        vazio.ValueKind.ShouldBe(JsonValueKind.Array);
        vazio.GetArrayLength().ShouldBe(0);
    }

    [Fact]
    [Trait("RN", "RN-17")]
    public async Task GetTopClientes_ClienteInativo_ApareceNoRanking()
    {
        var produto = await ProdutoAsync();
        var inativo = await ClienteAsync("Cliente desativado", ativo: false);
        await PedidoAsync(inativo, Utc(2026, 3, 1), produto, 123.45m);

        var (status, corpo) = await ObterAsync(_rota2026);

        status.ShouldBe(HttpStatusCode.OK);
        ClienteIds(corpo).ShouldBe([inativo]);
        Valores(corpo).ShouldBe([123.45m]);
    }

    [Theory]
    [Trait("CA", "CA-26")]
    [InlineData("")]
    [InlineData("?dataFim=2026-12-31")]
    [InlineData("?dataInicio=2026-01-01")]
    [InlineData("?dataInicio=&dataFim=2026-12-31")]
    [InlineData("?dataInicio=01/01/2026&dataFim=2026-12-31")]
    [InlineData("?dataInicio=2026-01-01&dataFim=2026-02-30")]
    [InlineData("?dataInicio=2026-04-01&dataFim=2026-03-31")]
    public async Task GetTopClientes_SemPeriodo_Retorna400(string consulta)
    {
        var (status, corpo) = await ObterAsync($"/relatorios/top-clientes{consulta}");

        status.ShouldBe(HttpStatusCode.BadRequest);
        corpo.GetProperty("code").GetString().ShouldBe("VALIDACAO");
        corpo.GetProperty("errors").EnumerateObject().ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("CA", "CA-27")]
    public async Task GetTopClientes_ValorTotal_IgualSomaDosItens()
    {
        // Pedidos criados pela própria API (preço vindo do banco, RN-09), com quantidades e centavos.
        var teclado = await ProdutoAsync(149.90m);
        var mouse = await ProdutoAsync(29.95m);
        var cabo = await ProdutoAsync(0.33m, estoque: 1_000);
        var ana = await ClienteAsync("Ana");
        var bia = await ClienteAsync("Bia");
        await CriarPedidoPelaApiAsync(ana, (teclado, 2), (mouse, 3), (cabo, 7));
        await CriarPedidoPelaApiAsync(ana, (cabo, 101));
        await CriarPedidoPelaApiAsync(bia, (mouse, 1), (cabo, 3));

        // Período que cobre o instante atual do servidor: a API grava DataPedido pelo relógio.
        var (status, corpo) = await ObterAsync("/relatorios/top-clientes?dataInicio=2000-01-01&dataFim=2099-12-31");

        status.ShouldBe(HttpStatusCode.OK);
        ClienteIds(corpo).ShouldBe([ana, bia]);
        foreach (var linha in corpo.EnumerateArray())
        {
            var somaDosItens = await SomaDosItensAsync(linha.GetProperty("clienteId").GetInt32());
            linha.GetProperty("valorTotal").GetDecimal().ShouldBe(somaDosItens);
        }

        Valores(corpo).ShouldBe([(2 * 149.90m) + (3 * 29.95m) + (7 * 0.33m) + (101 * 0.33m), 29.95m + (3 * 0.33m)]);
    }

    private async Task CriarPedidoPelaApiAsync(int clienteId, params (int ProdutoId, int Quantidade)[] itens)
    {
        using var http = _factory.CreateClient();
        var corpo = new { clienteId, itens = itens.Select(i => new { produtoId = i.ProdutoId, quantidade = i.Quantidade }) };

        var resposta = await http.PostAsJsonAsync("/pedidos", corpo, _ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private async Task<decimal> SomaDosItensAsync(int clienteId) =>
        Convert.ToDecimal(
            await Sql.EscalarAsync(banco.ConnectionStringSa, """
                SELECT SUM(i.Quantidade * i.PrecoUnitario)
                FROM dbo.Pedidos p JOIN dbo.ItensPedido i ON i.PedidoId = p.Id
                WHERE p.ClienteId = @cliente;
                """, _ct, new SqlParameter("@cliente", clienteId)),
            CultureInfo.InvariantCulture);
}
