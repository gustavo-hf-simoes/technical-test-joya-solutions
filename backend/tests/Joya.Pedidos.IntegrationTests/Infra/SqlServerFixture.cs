using System.Security.Cryptography;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;

namespace Joya.Pedidos.IntegrationTests.Infra;

/// <summary>
/// SQL Server real para os testes de integração (ADR-0003).
/// <list type="bullet">
/// <item>Um contêiner por execução, compartilhado pela coleção <see cref="SqlServerCollection"/>.</item>
/// <item>O schema chega pelo <b>mesmo</b> <c>database/init.sh</c> do <c>db-init</c> do compose,
/// copiado para o contêiner e executado com <c>sqlcmd -I -b</c>. Nenhum script é lido pelo SqlClient.</item>
/// <item>Aplica todas as etapas <c>NN-*.sql</c>, menos a carga de exemplo: cada teste monta os próprios dados.</item>
/// <item>As senhas de <c>sa</c>, <c>joya_app</c> e <c>claude_mcp</c> são geradas a cada execução.</item>
/// <item>O reset entre testes usa Respawn com <c>sa</c> (o <c>joya_app</c> não tem <c>DELETE</c>).</item>
/// </list>
/// </summary>
public sealed partial class SqlServerFixture : IAsyncLifetime
{
    /// <summary>Mesma <i>tag</i> do <c>docker-compose.yml</c> (ADR-0003, item 6; checklist DB-01).</summary>
    public const string Imagem = "mcr.microsoft.com/mssql/server:2022-latest";

    /// <summary>Banco criado por <c>01-schema.sql</c> (spec §6).</summary>
    public const string Banco = "JoyaPedidos";

    private const string _pastaNoConteiner = "/database";

    /// <summary>Etapas que o fixture não aplica (ADR-0003, item 2): a carga de exemplo.</summary>
    private static readonly string[] _etapasForaDoFixture = ["04-carga"];

    /// <summary>Tabelas limpas pelo Respawn, com o <c>IDENTITY</c> reiniciado.</summary>
    private static readonly string[] _tabelas = ["Clientes", "Produtos", "Pedidos", "ItensPedido"];

    private readonly string _senhaSa = GerarSenha();
    private readonly string _senhaApp = GerarSenha();
    private readonly string _senhaMcp = GerarSenha();
    private readonly MsSqlContainer _conteiner;
    private Respawner? _respawner;

    public SqlServerFixture()
    {
        _conteiner = new MsSqlBuilder(Imagem)
            .WithPassword(_senhaSa)
            .WithEnvironment("APP_SQL_PASSWORD", _senhaApp)
            .WithEnvironment("MCP_SQL_PASSWORD", _senhaMcp)
            .WithResourceMapping(PastaDatabase, _pastaNoConteiner)
            .Build();

        EtapasAplicadas = [.. PastaDatabase
            .GetFiles("*.sql")
            .Select(arquivo => Path.GetFileNameWithoutExtension(arquivo.Name))
            .Where(etapa => EtapaNumerada().IsMatch(etapa) && !_etapasForaDoFixture.Contains(etapa))
            .Order(StringComparer.Ordinal)];
    }

    /// <summary>Pasta <c>database/</c> do repositório, copiada para o contêiner.</summary>
    public static DirectoryInfo PastaDatabase { get; } = LocalizarPastaDatabase();

    /// <summary>Etapas que o fixture aplica, em ordem (ex.: <c>01-schema</c>, <c>02-indices</c>).</summary>
    public IReadOnlyList<string> EtapasAplicadas { get; }

    /// <summary>Conexão administrativa, só para Arrange, Assert e reset.</summary>
    public string ConnectionStringSa => MontarConnectionString("sa", _senhaSa);

    /// <summary>Login da API (spec §6.5): SELECT, INSERT e UPDATE em <c>dbo</c>.</summary>
    public string ConnectionStringApp => MontarConnectionString("joya_app", _senhaApp);

    /// <summary>Login do MCP (spec §6.5): leitura e diagnóstico.</summary>
    public string ConnectionStringMcp => MontarConnectionString("claude_mcp", _senhaMcp);

    public async Task InitializeAsync()
    {
        await _conteiner.StartAsync();

        var resultado = await ExecutarInitAsync(EtapasAplicadas, CancellationToken.None);
        if (resultado.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"database/init.sh saiu com o código {resultado.ExitCode}.{Environment.NewLine}" +
                $"stderr:{Environment.NewLine}{resultado.Stderr}{Environment.NewLine}" +
                $"stdout:{Environment.NewLine}{resultado.Stdout}");
        }

        await using var conexao = new SqlConnection(ConnectionStringSa);
        await conexao.OpenAsync();
        _respawner = await Respawner.CreateAsync(conexao, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["dbo"],
            TablesToInclude = [.. _tabelas.Select(tabela => new Table("dbo", tabela))],
            WithReseed = true,
        });
    }

    public Task DisposeAsync() => _conteiner.DisposeAsync().AsTask();

    /// <summary>
    /// Roda <c>database/init.sh</c> dentro do contêiner, com as etapas informadas.
    /// Devolve o resultado sem lançar, para que o teste confira o código de saída.
    /// </summary>
    public Task<ExecResult> ExecutarInitAsync(IReadOnlyList<string> etapas, CancellationToken ct) =>
        _conteiner.ExecAsync(["bash", $"{_pastaNoConteiner}/init.sh", .. etapas], ct);

    /// <summary>Apaga os dados das quatro tabelas e reinicia o <c>IDENTITY</c>.</summary>
    public async Task ResetAsync(CancellationToken ct)
    {
        var respawner = _respawner
            ?? throw new InvalidOperationException("O fixture ainda não foi inicializado.");

        await using var conexao = new SqlConnection(ConnectionStringSa);
        await conexao.OpenAsync(ct);
        await respawner.ResetAsync(conexao);
    }

    private string MontarConnectionString(string usuario, string senha) =>
        new SqlConnectionStringBuilder(_conteiner.GetConnectionString())
        {
            InitialCatalog = Banco,
            UserID = usuario,
            Password = senha,
            TrustServerCertificate = true,
        }.ConnectionString;

    /// <summary>Senha aleatória que atende à política do SQL Server e não tem aspa simples.</summary>
    private static string GerarSenha() => $"Aa1_{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}";

    private static DirectoryInfo LocalizarPastaDatabase()
    {
        for (var pasta = new DirectoryInfo(AppContext.BaseDirectory); pasta is not null; pasta = pasta.Parent)
        {
            var candidata = new DirectoryInfo(Path.Combine(pasta.FullName, "database"));
            if (File.Exists(Path.Combine(candidata.FullName, "init.sh")))
            {
                return candidata;
            }
        }

        throw new DirectoryNotFoundException(
            $"Pasta database/ com init.sh não encontrada acima de {AppContext.BaseDirectory}.");
    }

    [GeneratedRegex(@"^\d{2}-[a-z0-9-]+$")]
    private static partial Regex EtapaNumerada();
}
