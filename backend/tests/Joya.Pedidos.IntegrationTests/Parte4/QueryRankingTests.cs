using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Joya.Pedidos.IntegrationTests.Api;
using Joya.Pedidos.IntegrationTests.Infra;
using Joya.Pedidos.IntegrationTests.Persistencia;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Parte4;

/// <summary>
/// US-10 · Parte 4 do enunciado. Executa os arquivos de <c>database/parte4/</c> contra o SQL Server real:
/// T-10.1 prova os defeitos da query original, T-10.2 prova a query corrigida (CA-33).
/// Os planos são lidos com o login <c>claude_mcp</c>, o mesmo do MCP, que tem <c>SHOWPLAN</c> (spec §6.5).
/// </summary>
[Collection(SqlServerCollection.Nome)]
public sealed class QueryRankingTests(SqlServerFixture banco) : IAsyncLifetime
{
    private static readonly CancellationToken _ct = CancellationToken.None;

    public Task InitializeAsync() => banco.ResetAsync(_ct);

    // A carga de exemplo de CA-33 não pode vazar para as outras classes.
    public Task DisposeAsync() => banco.ResetAsync(_ct);

    /// <summary>Carga de exemplo (CA-33: "Dado a carga de exemplo") com estatísticas que a descrevem.</summary>
    private async Task CarregarExemploAsync()
    {
        var carga = await banco.ExecutarInitAsync(["04-carga"], _ct);
        carga.ExitCode.ShouldBe(0, carga.Stderr);
        await ScriptsParte4.AtualizarEstatisticasAsync(banco.ConnectionStringSa, _ct);
    }

    private static DateTime Utc(int ano, int mes, int dia, int hora = 0, int minuto = 0, int segundo = 0, int ms = 0) =>
        new(ano, mes, dia, hora, minuto, segundo, ms, DateTimeKind.Utc);

    private async Task<int> ClienteAsync(string nome) =>
        await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, nome, ativo: true, _ct);

    private async Task<int> ProdutoAsync() =>
        await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Produto", 1m, 1000, ativo: true, _ct);

    /// <summary>Pedido de um item com o valor dado (2 × valor/2), para a soma dos itens ser exercida.</summary>
    private async Task PedidoAsync(int clienteId, DateTime data, int produtoId, decimal valor) =>
        await Arranjo.InserirPedidoAsync(banco.ConnectionStringSa, clienteId, data, [(produtoId, 2, valor / 2)], _ct);

    // ---- T-10.1 · defeitos da query original ------------------------------------------------------------

    [Fact]
    [Trait("Tarefa", "T-10.1")]
    [Trait("RN", "RN-17")]
    public async Task QueryOriginal_ClientesHomonimos_SomaIndevidamente()
    {
        var produto = await ProdutoAsync();
        var joao1 = await ClienteAsync("João da Silva");
        var joao2 = await ClienteAsync("João da Silva");
        await PedidoAsync(joao1, Utc(2026, 3, 10), produto, 100m);
        await PedidoAsync(joao2, Utc(2026, 7, 1), produto, 50m);

        var ranking = await ScriptsParte4.RankingAsync(banco.ConnectionStringSa, ScriptsParte4.QueryOriginal, _ct);

        // Dois clientes distintos viram uma linha só, com a soma dos dois, e sem Id para separá-los.
        ranking.ShouldBe([new LinhaRanking(null, "João da Silva", 150m)]);
    }

    [Fact]
    [Trait("Tarefa", "T-10.1")]
    [Trait("RN", "RN-17")]
    public async Task QueryOriginal_Ordenacao_RetornaMenorTotalPrimeiro()
    {
        var produto = await ProdutoAsync();
        var grande = await ClienteAsync("Cliente Grande");
        var pequeno = await ClienteAsync("Cliente Pequeno");
        var medio = await ClienteAsync("Cliente Médio");
        await PedidoAsync(grande, Utc(2026, 2, 1), produto, 300m);
        await PedidoAsync(pequeno, Utc(2026, 2, 2), produto, 100m);
        await PedidoAsync(medio, Utc(2026, 2, 3), produto, 200m);

        var ranking = await ScriptsParte4.RankingAsync(banco.ConnectionStringSa, ScriptsParte4.QueryOriginal, _ct);

        // Um "ranking" que começa por quem menos comprou.
        ranking.Select(l => l.Nome).ShouldBe(["Cliente Pequeno", "Cliente Médio", "Cliente Grande"]);
        ranking.Select(l => l.Total).ShouldBe([100m, 200m, 300m]);
    }

    [Fact]
    [Trait("Tarefa", "T-10.1")]
    [Trait("RN", "RN-15")]
    public async Task QueryOriginal_Plano_LeTodosOsPedidosComFuncaoSobreDataPedido()
    {
        await CarregarExemploAsync();
        var consulta = await ScriptsParte4.ConsultaAsync(ScriptsParte4.QueryOriginal, _ct);

        var plano = await ScriptsParte4.PlanoEstimadoAsync(banco.ConnectionStringMcp, consulta, _ct);

        // Com a função sobre a coluna não existe seek por DataPedido: todos os pedidos são lidos e o ano só é
        // filtrado depois. Com 34 pedidos o otimizador pode trocar o scan por seeks por ClienteId, que também
        // leem todos; o scan de Pedidos com volume é provado em InvestigacaoTests.
        plano.TemFuncaoSobreDataPedido.ShouldBeTrue(plano.Arvore());
        plano.BuscaPelaColuna("Pedidos", "DataPedido").ShouldBeFalse(plano.Arvore());
    }

    // ---- T-10.2 · query corrigida -----------------------------------------------------------------------

    [Fact]
    [Trait("Tarefa", "T-10.2")]
    [Trait("CA", "CA-33")]
    public async Task QueryCorrigida_Ranking2026_CoincideComTopClientes()
    {
        await CarregarExemploAsync();
        var esperado = await RankingCalculadoEmMemoriaAsync(2026);

        var ranking = await ScriptsParte4.RankingAsync(banco.ConnectionStringSa, ScriptsParte4.QueryCorrigida, _ct);

        // Ranking completo: uma linha por ClienteId, só quem comprou em 2026, total = soma dos itens de 2026.
        ranking.Select(l => (l.ClienteId, l.Total)).ShouldBe(esperado.Select(e => ((int?)e.ClienteId, (decimal?)e.Total)));
        ranking.Select(l => l.ClienteId).ShouldBeUnique();
        ranking.Count.ShouldBeGreaterThan(5, "a carga de exemplo tem mais de 5 compradores em 2026");

        // Ordem decrescente de total, com desempate pelo menor Id.
        ranking.ShouldBe([.. ranking.OrderByDescending(l => l.Total).ThenBy(l => l.ClienteId)]);

        // Os homônimos da carga (dois "Maria Silva", dois "João Souza") aparecem em linhas separadas.
        ranking.Count(l => l.Nome == "Maria Silva").ShouldBe(2);
        ranking.Count(l => l.Nome == "João Souza").ShouldBe(2);

        // Os 5 primeiros são os mesmos de GET /relatorios/top-clientes de 2026.
        var api = await TopClientesDaApiAsync();
        ranking.Take(5).Select(l => (l.ClienteId ?? 0, l.Total ?? 0m)).ShouldBe(api);

        // O plano não varre Pedidos nem aplica função sobre DataPedido: a faixa de 2026 delimita o seek.
        var consulta = await ScriptsParte4.ConsultaAsync(ScriptsParte4.QueryCorrigida, _ct);
        var plano = await ScriptsParte4.PlanoEstimadoAsync(banco.ConnectionStringMcp, consulta, _ct);
        plano.VarreTabela("Pedidos").ShouldBeFalse(plano.Arvore());
        plano.BuscaPelaColuna("Pedidos", "DataPedido").ShouldBeTrue(plano.Arvore());
        plano.TemFuncaoSobreDataPedido.ShouldBeFalse(plano.Arvore());
    }

    [Fact]
    [Trait("Tarefa", "T-10.2")]
    [Trait("CA", "CA-23")]
    public async Task QueryCorrigida_ClientesHomonimos_UmaLinhaPorCliente()
    {
        var produto = await ProdutoAsync();
        var joao1 = await ClienteAsync("João da Silva");
        var joao2 = await ClienteAsync("João da Silva");
        await PedidoAsync(joao1, Utc(2026, 3, 10), produto, 100m);
        await PedidoAsync(joao2, Utc(2026, 7, 1), produto, 50m);

        var ranking = await ScriptsParte4.RankingAsync(banco.ConnectionStringSa, ScriptsParte4.QueryCorrigida, _ct);

        ranking.ShouldBe(
        [
            new LinhaRanking(joao1, "João da Silva", 100m),
            new LinhaRanking(joao2, "João da Silva", 50m),
        ]);
    }

    [Fact]
    [Trait("Tarefa", "T-10.2")]
    [Trait("CA", "CA-22")]
    public async Task QueryCorrigida_FronteirasDoAno_ContaSoPedidosDe2026()
    {
        var produto = await ProdutoAsync();
        var cliente = await ClienteAsync("Cliente");
        await PedidoAsync(cliente, Utc(2025, 12, 31, 23, 59, 59, 999), produto, 1000m); // fora
        await PedidoAsync(cliente, Utc(2026, 1, 1), produto, 10m);                      // dentro
        await PedidoAsync(cliente, Utc(2026, 12, 31, 23, 59, 59, 999), produto, 20m);   // dentro
        await PedidoAsync(cliente, Utc(2027, 1, 1), produto, 2000m);                    // fora
        var so2025 = await ClienteAsync("Só 2025");
        await PedidoAsync(so2025, Utc(2025, 6, 1), produto, 500m);

        var ranking = await ScriptsParte4.RankingAsync(banco.ConnectionStringSa, ScriptsParte4.QueryCorrigida, _ct);

        ranking.ShouldBe([new LinhaRanking(cliente, "Cliente", 30m)]);
    }

    [Fact]
    [Trait("Tarefa", "T-10.2")]
    [Trait("CA", "CA-24")]
    public async Task QueryCorrigida_Empate_OrdenaPorMenorClienteId()
    {
        var produto = await ProdutoAsync();
        var primeiro = await ClienteAsync("Zeca");
        var segundo = await ClienteAsync("Abel");
        var maior = await ClienteAsync("Maior");
        await PedidoAsync(segundo, Utc(2026, 4, 1), produto, 80m);
        await PedidoAsync(primeiro, Utc(2026, 4, 2), produto, 80m);
        await PedidoAsync(maior, Utc(2026, 4, 3), produto, 90m);

        var ranking = await ScriptsParte4.RankingAsync(banco.ConnectionStringSa, ScriptsParte4.QueryCorrigida, _ct);

        ranking.Select(l => l.ClienteId).ShouldBe([maior, primeiro, segundo]);
    }

    /// <summary>
    /// Oráculo independente do SQL testado: lê os itens sem filtro e agrega em memória por <c>ClienteId</c>.
    /// </summary>
    private async Task<IReadOnlyList<(int ClienteId, decimal Total)>> RankingCalculadoEmMemoriaAsync(int ano)
    {
        var itens = new List<(int ClienteId, DateTime Data, decimal Valor)>();
        await using (var conexao = new SqlConnection(banco.ConnectionStringSa))
        {
            await conexao.OpenAsync(_ct);
            await using var cmd = new SqlCommand("""
                SELECT p.ClienteId, p.DataPedido, i.Quantidade, i.PrecoUnitario
                FROM dbo.Pedidos p JOIN dbo.ItensPedido i ON i.PedidoId = p.Id;
                """, conexao);
            await using var leitor = await cmd.ExecuteReaderAsync(_ct);
            while (await leitor.ReadAsync(_ct))
            {
                itens.Add((leitor.GetInt32(0), leitor.GetDateTime(1), leitor.GetInt32(2) * leitor.GetDecimal(3)));
            }
        }

        return
        [
            .. itens
                .Where(i => i.Data.Year == ano)
                .GroupBy(i => i.ClienteId)
                .Select(g => (ClienteId: g.Key, Total: g.Sum(i => i.Valor)))
                .OrderByDescending(t => t.Total)
                .ThenBy(t => t.ClienteId),
        ];
    }

    private async Task<IReadOnlyList<(int ClienteId, decimal ValorTotal)>> TopClientesDaApiAsync()
    {
        using var factory = new ApiFactory(banco.ConnectionStringApp);
        using var http = factory.CreateClient();
        var resposta = await http.GetAsync("/relatorios/top-clientes?dataInicio=2026-01-01&dataFim=2026-12-31", _ct);
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(_ct);
        return
        [
            .. corpo.EnumerateArray().Select(l => (
                l.GetProperty("clienteId").GetInt32(),
                decimal.Parse(l.GetProperty("valorTotal").GetRawText(), CultureInfo.InvariantCulture))),
        ];
    }
}
