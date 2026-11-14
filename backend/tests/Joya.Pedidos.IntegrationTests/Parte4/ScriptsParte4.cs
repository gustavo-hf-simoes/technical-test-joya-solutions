using System.Globalization;
using System.Text.RegularExpressions;
using Joya.Pedidos.IntegrationTests.Infra;
using Microsoft.Data.SqlClient;

namespace Joya.Pedidos.IntegrationTests.Parte4;

/// <summary>Uma linha do ranking. A query original não devolve o Id, por isso ele é opcional.</summary>
public sealed record LinhaRanking(int? ClienteId, string Nome, decimal? Total);

/// <summary>Leituras lógicas de uma tabela, tiradas do <c>SET STATISTICS IO</c>.</summary>
public sealed record LeiturasDaTabela(string Tabela, long Varreduras, long LeiturasLogicas);

/// <summary>Uma execução com plano real (<c>SET STATISTICS XML</c>) e leituras por tabela (<c>SET STATISTICS IO</c>).</summary>
public sealed record ExecucaoMedida(
    IReadOnlyList<LinhaRanking> Linhas, PlanoDeExecucao Plano, string PlanoXml, IReadOnlyList<LeiturasDaTabela> Leituras)
{
    public long LeiturasDe(string tabela) =>
        Leituras.Where(l => l.Tabela == tabela).Sum(l => l.LeiturasLogicas);

    public long LeiturasTotais => Leituras.Sum(l => l.LeiturasLogicas);
}

/// <summary>Média das execuções de uma consulta, tirada de <c>sys.dm_exec_query_stats</c>.</summary>
public sealed record EstatisticasDaConsulta(
    long Execucoes, long LeiturasLogicasPorExecucao, double CpuMsPorExecucao, double DecorridoMsPorExecucao,
    double MenorDecorridoMs, double MaiorDecorridoMs, long GrauDeParalelismo, long MemoriaConcedidaKb);

/// <summary>
/// Executa os arquivos de <c>database/parte4/</c> contra o SQL Server do Testcontainers. Os testes leem o
/// <b>próprio arquivo</b>, para que o que foi provado seja exatamente o que está no repositório. Cada arquivo de
/// consulta termina num lote (depois do último <c>GO</c>) que começa com o marcador <c>-- parte4:&lt;nome&gt;</c>;
/// o marcador identifica a consulta em <c>sys.dm_exec_query_stats</c>.
/// </summary>
public static partial class ScriptsParte4
{
    public const string CargaVolume = "00-carga-volume.sql";
    public const string QueryOriginal = "01-query-original.sql";
    public const string QueryCorrigida = "02-query-corrigida.sql";
    public const string VarianteValorTotal = "03-variante-valor-total.sql";

    /// <summary>A carga de volume leva dezenas de segundos; o padrão do SqlClient (30 s) não basta.</summary>
    private const int _timeoutSegundos = 900;

    public static DirectoryInfo Pasta { get; } = new(Path.Combine(SqlServerFixture.PastaDatabase.FullName, "parte4"));

    /// <summary>Lotes do arquivo, separados pelas linhas <c>GO</c>, como o <c>sqlcmd</c> faria.</summary>
    public static async Task<IReadOnlyList<string>> LotesAsync(string arquivo, CancellationToken ct)
    {
        var texto = await File.ReadAllTextAsync(Path.Combine(Pasta.FullName, arquivo), ct);
        return [.. SeparadorDeLotes().Split(texto).Where(lote => !string.IsNullOrWhiteSpace(lote))];
    }

    /// <summary>O último lote do arquivo: a consulta, sem o cabeçalho e o <c>USE</c>.</summary>
    public static async Task<string> ConsultaAsync(string arquivo, CancellationToken ct) =>
        (await LotesAsync(arquivo, ct))[^1];

    /// <summary>Executa todos os lotes do arquivo, em ordem, na mesma sessão.</summary>
    public static async Task ExecutarArquivoAsync(string connectionString, string arquivo, CancellationToken ct)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);
        foreach (var lote in await LotesAsync(arquivo, ct))
        {
            await ExecutarAsync(conexao, lote, ct);
        }
    }

    /// <summary>Executa a consulta do arquivo e devolve as linhas na ordem do banco.</summary>
    public static async Task<IReadOnlyList<LinhaRanking>> RankingAsync(
        string connectionString, string arquivo, CancellationToken ct)
    {
        var consulta = await ConsultaAsync(arquivo, ct);
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);
        await using var cmd = new SqlCommand(consulta, conexao) { CommandTimeout = _timeoutSegundos };
        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        return await LerRankingAsync(leitor, ct);
    }

    /// <summary>Plano estimado (<c>SET SHOWPLAN_XML ON</c>): a consulta é compilada, não executada.</summary>
    public static async Task<PlanoDeExecucao> PlanoEstimadoAsync(string connectionString, string consulta, CancellationToken ct)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);
        await ExecutarAsync(conexao, "SET SHOWPLAN_XML ON;", ct);
        try
        {
            await using var cmd = new SqlCommand(consulta, conexao) { CommandTimeout = _timeoutSegundos };
            var xml = await cmd.ExecuteScalarAsync(ct) as string
                ?? throw new InvalidOperationException("O SHOWPLAN_XML não devolveu o plano.");
            return PlanoDeExecucao.Ler(xml);
        }
        finally
        {
            await ExecutarAsync(conexao, "SET SHOWPLAN_XML OFF;", ct);
        }
    }

    /// <summary>
    /// Executa a consulta com <c>SET STATISTICS IO</c> e <c>SET STATISTICS XML</c> ligados e devolve as linhas,
    /// o plano real (com linhas e leituras reais por operador) e as leituras lógicas por tabela.
    /// </summary>
    public static async Task<ExecucaoMedida> ExecutarMedindoAsync(string connectionString, string consulta, CancellationToken ct)
    {
        var mensagens = new List<string>();
        await using var conexao = new SqlConnection(connectionString);
        conexao.InfoMessage += (_, e) => mensagens.AddRange(e.Errors.Cast<SqlError>().Select(erro => erro.Message));
        await conexao.OpenAsync(ct);
        await ExecutarAsync(conexao, "SET STATISTICS IO ON;", ct);
        await ExecutarAsync(conexao, "SET STATISTICS XML ON;", ct);

        IReadOnlyList<LinhaRanking> linhas;
        string? xml = null;
        await using (var cmd = new SqlCommand(consulta, conexao) { CommandTimeout = _timeoutSegundos })
        await using (var leitor = await cmd.ExecuteReaderAsync(ct))
        {
            linhas = await LerRankingAsync(leitor, ct);
            while (xml is null && await leitor.NextResultAsync(ct))
            {
                if (leitor.FieldCount == 1 && leitor.GetName(0).Contains("Showplan", StringComparison.Ordinal)
                    && await leitor.ReadAsync(ct))
                {
                    xml = leitor.GetString(0);
                }
            }
        }

        await ExecutarAsync(conexao, "SET STATISTICS XML OFF;", ct);
        await ExecutarAsync(conexao, "SET STATISTICS IO OFF;", ct);

        if (xml is null)
        {
            throw new InvalidOperationException("O STATISTICS XML não devolveu o plano real.");
        }

        var leituras = mensagens
            .Select(m => LinhaStatisticsIo().Match(m))
            .Where(m => m.Success)
            .Select(m => new LeiturasDaTabela(
                m.Groups["tabela"].Value,
                long.Parse(m.Groups["varreduras"].Value, CultureInfo.InvariantCulture),
                long.Parse(m.Groups["leituras"].Value, CultureInfo.InvariantCulture)))
            .ToList();

        return new ExecucaoMedida(linhas, PlanoDeExecucao.Ler(xml), xml, leituras);
    }

    /// <summary>
    /// Apaga os dados das quatro tabelas com timeout longo (a carga de volume tem 800 mil linhas). <c>ItensPedido</c>
    /// não é referenciada por nenhuma FK e pode ser truncada; as demais, só com <c>DELETE</c>, filhas primeiro.
    /// </summary>
    public static async Task RemoverDadosAsync(string connectionStringSa, CancellationToken ct)
    {
        await using var conexao = new SqlConnection(connectionStringSa);
        await conexao.OpenAsync(ct);
        await ExecutarAsync(conexao, """
            TRUNCATE TABLE dbo.ItensPedido;
            DELETE FROM dbo.Pedidos;
            DELETE FROM dbo.Produtos;
            DELETE FROM dbo.Clientes;
            """, ct);
    }

    /// <summary>
    /// Recalcula as estatísticas das quatro tabelas. Depois do Respawn elas ainda descrevem o conteúdo de testes
    /// anteriores (tabelas pequenas não atingem o limiar de atualização automática), e o plano sairia de
    /// estimativas falsas. No compose isso não acontece: as estatísticas nascem da própria carga.
    /// </summary>
    public static async Task AtualizarEstatisticasAsync(string connectionStringSa, CancellationToken ct)
    {
        await using var conexao = new SqlConnection(connectionStringSa);
        await conexao.OpenAsync(ct);
        await ExecutarAsync(conexao, """
            UPDATE STATISTICS dbo.Clientes WITH FULLSCAN;
            UPDATE STATISTICS dbo.Produtos WITH FULLSCAN;
            UPDATE STATISTICS dbo.Pedidos WITH FULLSCAN;
            UPDATE STATISTICS dbo.ItensPedido WITH FULLSCAN;
            DBCC FREEPROCCACHE WITH NO_INFOMSGS;
            """, ct);
    }

    /// <summary>Esvazia o cache de planos do servidor, para que <c>sys.dm_exec_query_stats</c> só conte o que vem depois.</summary>
    public static async Task LimparCacheDePlanosAsync(string connectionStringSa, CancellationToken ct) =>
        await Sql.ExecutarAsync(connectionStringSa, "DBCC FREEPROCCACHE WITH NO_INFOMSGS;", ct);

    /// <summary>
    /// Médias de <c>sys.dm_exec_query_stats</c> para a consulta com o marcador <c>-- parte4:&lt;nome&gt;</c>.
    /// Exige <c>VIEW SERVER STATE</c>: roda como <c>sa</c> (o <c>claude_mcp</c> só tem <c>VIEW DATABASE STATE</c>).
    /// </summary>
    public static async Task<EstatisticasDaConsulta> EstatisticasAsync(
        string connectionStringSa, string arquivo, CancellationToken ct)
    {
        var marcador = MarcadorDaConsulta().Match(await ConsultaAsync(arquivo, ct)).Value;
        await using var conexao = new SqlConnection(connectionStringSa);
        await conexao.OpenAsync(ct);
        await using var cmd = new SqlCommand("""
            SELECT SUM(qs.execution_count), SUM(qs.total_logical_reads), SUM(qs.total_worker_time),
                   SUM(qs.total_elapsed_time), MIN(qs.min_elapsed_time), MAX(qs.max_elapsed_time),
                   MAX(qs.last_dop), MAX(qs.max_grant_kb)
            FROM sys.dm_exec_query_stats AS qs
            CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) AS t
            WHERE t.text LIKE @marcador
              AND t.text NOT LIKE N'%dm_exec_query_stats%';
            """, conexao);
        cmd.Parameters.Add(new SqlParameter("@marcador", $"%{marcador}%"));
        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        if (!await leitor.ReadAsync(ct) || await leitor.IsDBNullAsync(0, ct))
        {
            throw new InvalidOperationException($"Nenhuma execução de '{marcador}' em sys.dm_exec_query_stats.");
        }

        var execucoes = leitor.GetInt64(0);
        return new EstatisticasDaConsulta(
            execucoes,
            leitor.GetInt64(1) / execucoes,
            leitor.GetInt64(2) / 1000.0 / execucoes,
            leitor.GetInt64(3) / 1000.0 / execucoes,
            leitor.GetInt64(4) / 1000.0,
            leitor.GetInt64(5) / 1000.0,
            leitor.GetInt64(6),
            leitor.GetInt64(7));
    }

    private static async Task ExecutarAsync(SqlConnection conexao, string lote, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(lote, conexao) { CommandTimeout = _timeoutSegundos };
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<IReadOnlyList<LinhaRanking>> LerRankingAsync(SqlDataReader leitor, CancellationToken ct)
    {
        var colunas = Enumerable.Range(0, leitor.FieldCount).Select(leitor.GetName).ToList();
        var id = colunas.IndexOf("ClienteId");
        var nome = colunas.IndexOf("Nome");
        var total = colunas.IndexOf("Total");

        var linhas = new List<LinhaRanking>();
        while (await leitor.ReadAsync(ct))
        {
            linhas.Add(new LinhaRanking(
                id < 0 ? null : leitor.GetInt32(id),
                leitor.GetString(nome),
                await leitor.IsDBNullAsync(total, ct) ? null : leitor.GetDecimal(total)));
        }

        return linhas;
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex SeparadorDeLotes();

    [GeneratedRegex(@"parte4:[a-z0-9-]+")]
    private static partial Regex MarcadorDaConsulta();

    [GeneratedRegex(@"Table '(?<tabela>[^']+)'\. Scan count (?<varreduras>\d+), logical reads (?<leituras>\d+)")]
    private static partial Regex LinhaStatisticsIo();
}
