using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Joya.Pedidos.IntegrationTests.Parte4;

/// <summary>
/// Um operador do plano (<c>RelOp</c>). Os valores reais só existem no plano real. <c>ColunasDoSeek</c> são as
/// colunas que delimitam a busca (<c>SeekPredicates</c>): só elas reduzem o que é lido do índice.
/// </summary>
public sealed record OperadorDoPlano(
    int Nivel,
    string OperacaoFisica,
    string OperacaoLogica,
    string? Tabela,
    string? Indice,
    double LinhasEstimadas,
    long? LinhasReais,
    long? LeiturasLogicas,
    IReadOnlyList<string> ColunasDoSeek);

/// <summary>
/// Leitura do Showplan XML do SQL Server: a árvore de operadores, o custo estimado, o paralelismo e todas as
/// expressões escalares (predicados). É o mesmo XML que o <c>explain_sql</c> do MCP devolveria.
/// </summary>
public sealed partial record PlanoDeExecucao(
    IReadOnlyList<OperadorDoPlano> Operadores,
    IReadOnlyList<string> Expressoes,
    double CustoEstimado,
    int GrauDeParalelismo)
{
    private static readonly XNamespace _ns = "http://schemas.microsoft.com/sqlserver/2004/07/showplan";

    public static PlanoDeExecucao Ler(string xml)
    {
        var documento = XDocument.Parse(xml);

        var operadores = documento.Descendants(_ns + "RelOp").Select(LerOperador).ToList();
        var expressoes = documento.Descendants(_ns + "ScalarOperator")
            .Select(s => (string?)s.Attribute("ScalarString"))
            .OfType<string>()
            .ToList();
        var custo = documento.Descendants(_ns + "StmtSimple")
            .Select(s => (string?)s.Attribute("StatementSubTreeCost"))
            .OfType<string>()
            .Sum(c => double.Parse(c, CultureInfo.InvariantCulture));
        var paralelismo = documento.Descendants(_ns + "QueryPlan")
            .Select(q => (int?)q.Attribute("DegreeOfParallelism"))
            .Max() ?? 1;

        return new PlanoDeExecucao(operadores, expressoes, custo, paralelismo);
    }

    /// <summary>Algum operador lê a tabela inteira (<c>Table Scan</c>, <c>Index Scan</c> ou <c>Clustered Index Scan</c>).</summary>
    public bool VarreTabela(string tabela) =>
        Operadores.Any(o => o.Tabela == tabela && o.OperacaoFisica.EndsWith("Scan", StringComparison.Ordinal));

    /// <summary>
    /// Algum operador busca na tabela delimitando a faixa pela coluna, como o seek de <c>IX_Pedidos_DataPedido</c>
    /// por <c>DataPedido</c>. Falso quando a coluna só aparece como predicado residual ou num <c>Filter</c>: aí
    /// todas as linhas da tabela são lidas antes do filtro.
    /// </summary>
    public bool BuscaPelaColuna(string tabela, string coluna) =>
        Operadores.Any(o => o.Tabela == tabela && o.ColunasDoSeek.Contains(coluna));

    /// <summary>Alguma expressão aplica função à coluna, como <c>datepart(year,[..].[Pedidos].[DataPedido])</c> (RN-15).</summary>
    public bool TemFuncaoSobreDataPedido => Expressoes.Any(e => FuncaoSobreDataPedido().IsMatch(e));

    /// <summary>A árvore em texto, um operador por linha, indentada pelo nível.</summary>
    public string Arvore()
    {
        var texto = new StringBuilder();
        foreach (var o in Operadores)
        {
            texto.Append(' ', o.Nivel * 2).Append(o.OperacaoFisica);
            if (o.OperacaoLogica != o.OperacaoFisica)
            {
                texto.Append(" (").Append(o.OperacaoLogica).Append(')');
            }

            if (o.Tabela is not null)
            {
                texto.Append(' ').Append(o.Tabela).Append('.').Append(o.Indice ?? "(heap)");
            }

            if (o.ColunasDoSeek.Count > 0)
            {
                texto.Append(" seek(").AppendJoin(", ", o.ColunasDoSeek).Append(')');
            }

            texto.Append(CultureInfo.InvariantCulture, $"  estimadas={o.LinhasEstimadas:0.#}");
            if (o.LinhasReais is { } reais)
            {
                texto.Append(CultureInfo.InvariantCulture, $"  reais={reais}");
            }

            if (o.LeiturasLogicas is { } leituras)
            {
                texto.Append(CultureInfo.InvariantCulture, $"  leituras={leituras}");
            }

            texto.AppendLine();
        }

        return texto.ToString();
    }

    private static OperadorDoPlano LerOperador(XElement relOp)
    {
        // O Object que pertence a este RelOp, e não a um RelOp filho.
        var objeto = relOp.Descendants(_ns + "Object")
            .FirstOrDefault(o => ReferenceEquals(o.Ancestors(_ns + "RelOp").First(), relOp));
        var contadores = relOp.Element(_ns + "RunTimeInformation")?.Elements(_ns + "RunTimeCountersPerThread").ToList();
        var colunasDoSeek = relOp.Elements(_ns + "IndexScan").Elements(_ns + "SeekPredicates")
            .Descendants(_ns + "RangeColumns").Elements(_ns + "ColumnReference")
            .Select(c => SemColchetes((string?)c.Attribute("Column")))
            .OfType<string>()
            .Distinct()
            .ToList();

        return new OperadorDoPlano(
            relOp.Ancestors(_ns + "RelOp").Count(),
            (string?)relOp.Attribute("PhysicalOp") ?? "?",
            (string?)relOp.Attribute("LogicalOp") ?? "?",
            SemColchetes((string?)objeto?.Attribute("Table")),
            SemColchetes((string?)objeto?.Attribute("Index")),
            double.Parse((string?)relOp.Attribute("EstimateRows") ?? "0", CultureInfo.InvariantCulture),
            contadores is null ? null : contadores.Sum(c => (long?)c.Attribute("ActualRows") ?? 0),
            contadores is null ? null : contadores.Sum(c => (long?)c.Attribute("ActualLogicalReads") ?? 0),
            colunasDoSeek);
    }

    private static string? SemColchetes(string? nome) => nome?.Trim('[', ']');

    [GeneratedRegex(@"\w+\((\w+(\(\d+\))?,)?\[\w+\]\.\[dbo\]\.\[Pedidos\]\.\[DataPedido\]", RegexOptions.IgnoreCase)]
    private static partial Regex FuncaoSobreDataPedido();
}
