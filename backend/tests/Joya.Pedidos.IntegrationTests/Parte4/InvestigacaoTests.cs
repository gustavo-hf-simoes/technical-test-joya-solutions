using System.Diagnostics;
using System.Globalization;
using Joya.Pedidos.IntegrationTests.Infra;
using Shouldly;
using Xunit.Abstractions;

namespace Joya.Pedidos.IntegrationTests.Parte4;

/// <summary>
/// T-10.3 · medição da Parte 4 com volume (<c>database/parte4/00-carga-volume.sql</c>). Gera os números de
/// <c>database/parte4/investigacao.md</c>: plano estimado e real, leituras lógicas por tabela (<c>STATISTICS IO</c>)
/// e médias de <c>sys.dm_exec_query_stats</c>. Planos e leituras saem com o login <c>claude_mcp</c>, o mesmo do MCP;
/// a DMV, com <c>sa</c>. Os números vão para o log do teste (<c>--logger "console;verbosity=detailed"</c>) e os
/// planos reais, como <c>.sqlplan</c>, para a pasta <c>parte4-planos</c> da saída do build.
/// As asserções conferem só o que não depende da máquina: forma do plano e quem lê menos.
/// </summary>
[Collection(SqlServerCollection.Nome)]
public sealed class InvestigacaoTests(SqlServerFixture banco, ITestOutputHelper saida) : IAsyncLifetime
{
    /// <summary>Execuções medidas por consulta, depois do aquecimento.</summary>
    private const int _execucoes = 10;

    private static readonly CancellationToken _ct = CancellationToken.None;

    private static readonly string[] _arquivos =
        [ScriptsParte4.QueryOriginal, ScriptsParte4.QueryCorrigida, ScriptsParte4.VarianteValorTotal];

    public Task InitializeAsync() => banco.ResetAsync(_ct);

    // 800 mil linhas não podem vazar para as outras classes. O DELETE do Respawn não cabe no timeout padrão
    // do SqlClient com esse volume: o grosso sai antes, com timeout longo, e o Respawn só reinicia o IDENTITY.
    public async Task DisposeAsync()
    {
        await ScriptsParte4.RemoverDadosAsync(banco.ConnectionStringSa, _ct);
        await banco.ResetAsync(_ct);
    }

    [Fact]
    [Trait("Tarefa", "T-10.3")]
    [Trait("CA", "CA-33")]
    [Trait("Categoria", "Medicao")] // uns 2 min com a carga; exclua com --filter "Categoria!=Medicao"
    public async Task Investigacao_CargaDeVolume_MedePlanosELeituras()
    {
        var relogio = Stopwatch.StartNew();
        await ScriptsParte4.ExecutarArquivoAsync(banco.ConnectionStringSa, ScriptsParte4.CargaVolume, _ct);
        saida.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Carga de volume: {relogio.Elapsed.TotalSeconds:0.0} s"));
        await EscreverVolumeAsync();

        // 1. Plano estimado, plano real e STATISTICS IO, com o login do MCP. A primeira execução também aquece o cache.
        var medidas = new Dictionary<string, ExecucaoMedida>();
        var pastaPlanos = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "parte4-planos"));
        foreach (var arquivo in _arquivos)
        {
            var consulta = await ScriptsParte4.ConsultaAsync(arquivo, _ct);
            var estimado = await ScriptsParte4.PlanoEstimadoAsync(banco.ConnectionStringMcp, consulta, _ct);
            var medida = await ScriptsParte4.ExecutarMedindoAsync(banco.ConnectionStringMcp, consulta, _ct);
            medidas[arquivo] = medida;

            var caminho = Path.Combine(pastaPlanos.FullName, Path.ChangeExtension(arquivo, ".sqlplan"));
            await File.WriteAllTextAsync(caminho, medida.PlanoXml, _ct);

            saida.WriteLine("");
            saida.WriteLine($"===== {arquivo} =====");
            saida.WriteLine(
                $"Linhas: {medida.Linhas.Count} · custo estimado: {estimado.CustoEstimado.ToString("0.000", CultureInfo.InvariantCulture)}" +
                $" · DOP: {medida.Plano.GrauDeParalelismo}");
            saida.WriteLine("STATISTICS IO: " + string.Join(" | ", medida.Leituras.Select(l =>
                $"{l.Tabela}: scan count {l.Varreduras}, logical reads {l.LeiturasLogicas}")));
            saida.WriteLine($"Plano real ({caminho}):");
            saida.WriteLine(medida.Plano.Arvore());
        }

        // 2. Tempo e leituras médias por sys.dm_exec_query_stats, sem as opções de diagnóstico ligadas.
        await ScriptsParte4.LimparCacheDePlanosAsync(banco.ConnectionStringSa, _ct);
        for (var i = 0; i < _execucoes; i++)
        {
            foreach (var arquivo in _arquivos)
            {
                await ScriptsParte4.RankingAsync(banco.ConnectionStringMcp, arquivo, _ct);
            }
        }

        saida.WriteLine("");
        saida.WriteLine("| Consulta | Execuções | Leituras lógicas/exec | CPU ms/exec | Decorrido ms/exec | Menor | Maior | DOP | Memória KB |");
        saida.WriteLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var arquivo in _arquivos)
        {
            var e = await ScriptsParte4.EstatisticasAsync(banco.ConnectionStringSa, arquivo, _ct);
            saida.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"| {arquivo} | {e.Execucoes} | {e.LeiturasLogicasPorExecucao} | {e.CpuMsPorExecucao:0.0} | " +
                $"{e.DecorridoMsPorExecucao:0.0} | {e.MenorDecorridoMs:0.0} | {e.MaiorDecorridoMs:0.0} | " +
                $"{e.GrauDeParalelismo} | {e.MemoriaConcedidaKb} |"));
        }

        var original = medidas[ScriptsParte4.QueryOriginal];
        var corrigida = medidas[ScriptsParte4.QueryCorrigida];
        var variante = medidas[ScriptsParte4.VarianteValorTotal];

        // Antes: função sobre a coluna e Pedidos lida inteira.
        original.Plano.TemFuncaoSobreDataPedido.ShouldBeTrue();
        original.Plano.VarreTabela("Pedidos").ShouldBeTrue();
        original.Plano.BuscaPelaColuna("Pedidos", "DataPedido").ShouldBeFalse();

        // Depois: seek em Pedidos, sem função sobre DataPedido, e menos leituras em Pedidos (CA-33).
        foreach (var depois in new[] { corrigida, variante })
        {
            depois.Plano.TemFuncaoSobreDataPedido.ShouldBeFalse();
            depois.Plano.VarreTabela("Pedidos").ShouldBeFalse(depois.Plano.Arvore());
            depois.Plano.BuscaPelaColuna("Pedidos", "DataPedido").ShouldBeTrue(depois.Plano.Arvore());
            depois.LeiturasDe("Pedidos").ShouldBeLessThan(original.LeiturasDe("Pedidos"));
        }

        // A variante sobre ValorTotal dá o mesmo ranking, linha a linha (RN-09), sem ler ItensPedido.
        variante.Linhas.ShouldBe(corrigida.Linhas);
        variante.LeiturasDe("ItensPedido").ShouldBe(0L);
        variante.LeiturasTotais.ShouldBeLessThan(corrigida.LeiturasTotais);

        // A original junta os homônimos: tem menos linhas que clientes compradores.
        corrigida.Linhas.Select(l => l.ClienteId).ShouldBeUnique();
        original.Linhas.Count.ShouldBeLessThan(corrigida.Linhas.Count);
        original.Linhas.Count.ShouldBe(corrigida.Linhas.Select(l => l.Nome).Distinct().Count());
    }

    private async Task EscreverVolumeAsync()
    {
        string[] contagens =
        [
            "SELECT COUNT(*) FROM dbo.Clientes",
            "SELECT COUNT(*) FROM dbo.Produtos",
            "SELECT COUNT(*) FROM dbo.Pedidos",
            "SELECT COUNT(*) FROM dbo.ItensPedido",
            "SELECT COUNT(*) FROM dbo.Pedidos WHERE DataPedido >= '2026-01-01' AND DataPedido < '2027-01-01'",
            "SELECT COUNT(DISTINCT Nome) FROM dbo.Clientes",
            "SELECT COUNT(DISTINCT ClienteId) FROM dbo.Pedidos WHERE DataPedido >= '2026-01-01' AND DataPedido < '2027-01-01'",
        ];

        foreach (var consulta in contagens)
        {
            saida.WriteLine($"{consulta} → {await Sql.EscalarAsync(banco.ConnectionStringSa, consulta, _ct)}");
        }
    }
}
