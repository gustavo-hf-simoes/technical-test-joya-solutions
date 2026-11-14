using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Joya.Pedidos.Core.Relatorios;
using Joya.Pedidos.Infrastructure;
using Joya.Pedidos.IntegrationTests.Infra;
using Joya.Pedidos.IntegrationTests.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit.Abstractions;

namespace Joya.Pedidos.IntegrationTests.Relatorios;

/// <summary>
/// T-09.2 · forma do SQL que o EF gera para o ranking (Q5 da arquitetura). Prova que os defeitos da query da Parte 4
/// não existem aqui: uma única instrução, agrupada por <c>ClienteId</c> (nunca por nome), com <c>TOP</c>, filtro
/// semiaberto direto sobre <c>DataPedido</c> (sem <c>YEAR()</c> nem outra função sobre a coluna) e sem ler
/// <c>ItensPedido</c>. O SQL capturado sai no log do teste e é a evidência do relatório enquanto o MCP estiver fora (CG-05).
/// </summary>
[Collection(SqlServerCollection.Nome)]
public sealed partial class RelatorioConsultasTests(SqlServerFixture banco, ITestOutputHelper saida) : IAsyncLifetime
{
    private static readonly CancellationToken _ct = CancellationToken.None;

    public Task InitializeAsync() => banco.ResetAsync(_ct);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    [Trait("RN", "RN-17")]
    public async Task TopClientesAsync_SqlGerado_UmComandoAgrupadoPorClienteSemFuncaoSobreData()
    {
        var produto = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Produto", 10m, 100, ativo: true, _ct);
        var cliente = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "João da Silva", ativo: true, _ct);
        await Arranjo.InserirPedidoAsync(
            banco.ConnectionStringSa, cliente, new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), [(produto, 2, 10m)], _ct);

        using var coletor = new ColetorDeComandos();
        await using var provedor = Provedor(coletor);
        await using var escopo = provedor.CreateAsyncScope();
        var consultas = escopo.ServiceProvider.GetRequiredService<IRelatorioConsultas>();

        var ranking = await consultas.TopClientesAsync(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            RelatorioService.TamanhoRanking,
            _ct);

        ranking.ShouldBe([new TopClienteResponse(1, cliente, "João da Silva", 20m, 1)]);
        var sql = coletor.Comandos.ShouldHaveSingleItem(); // sem N+1: nome e totais na mesma instrução
        saida.WriteLine(sql);

        sql.ShouldContain("GROUP BY [p].[ClienteId]");
        GroupByComNome().IsMatch(sql).ShouldBeFalse("O agrupamento não pode usar o nome do cliente (RN-17).");
        sql.ShouldContain("SELECT TOP(@");
        sql.ShouldContain("[p].[DataPedido] >= @");
        sql.ShouldContain("[p].[DataPedido] < @");
        FuncaoSobreDataPedido().IsMatch(sql).ShouldBeFalse("Nenhuma função sobre DataPedido (RN-15).");
        sql.ShouldNotContain("ItensPedido");
        sql.ShouldNotContain("LEFT JOIN");
        OrdemDoRanking().IsMatch(sql).ShouldBeTrue("Ordem valor DESC com desempate por ClienteId ASC (RN-17).");
    }

    private ServiceProvider Provedor(ColetorDeComandos coletor)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DependencyInjection.ConnectionStringPadrao}"] = banco.ConnectionStringApp,
            })
            .Build();

        var servicos = new ServiceCollection();
        servicos.AddSingleton<IConfiguration>(configuracao);
        servicos.AddLogging(log => log.AddProvider(coletor));
        servicos.AddInfrastructure();
        return servicos.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [GeneratedRegex(@"GROUP BY[^\r\n]*Nome")]
    private static partial Regex GroupByComNome();

    [GeneratedRegex(@"\w+\(\s*\[p\]\.\[DataPedido\]")]
    private static partial Regex FuncaoSobreDataPedido();

    [GeneratedRegex(@"ORDER BY COALESCE\(SUM\(\[p\]\.\[ValorTotal\]\), 0\.0\) DESC, \[p\]\.\[ClienteId\]")]
    private static partial Regex OrdemDoRanking();

    /// <summary>Guarda o texto de cada <c>CommandExecuted</c> do EF Core.</summary>
    private sealed class ColetorDeComandos : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _comandos = new();

        public IReadOnlyList<string> Comandos => [.. _comandos];

        public ILogger CreateLogger(string categoryName) =>
            categoryName == DbLoggerCategory.Database.Command.Name ? new Registro(_comandos) : NullLogger.Instance;

        public void Dispose()
        {
        }

        private sealed class Registro(ConcurrentQueue<string> comandos) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (eventId.Id != RelationalEventId.CommandExecuted.Id)
                {
                    return;
                }

                var texto = state is IReadOnlyList<KeyValuePair<string, object?>> campos
                    ? campos.FirstOrDefault(c => c.Key == "commandText").Value as string
                    : null;
                comandos.Enqueue(texto ?? formatter(state, exception));
            }
        }
    }
}
