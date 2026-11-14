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
}
