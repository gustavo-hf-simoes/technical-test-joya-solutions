using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Joya.Pedidos.IntegrationTests.Api;

/// <summary>
/// API em memória, ambiente <c>Testing</c>, apontada para o SQL Server informado.
/// Registra os controllers de <see cref="RotasDeTesteController"/> (rotas só de teste, fora da produção).
/// </summary>
public sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    /// <summary>Connection string que recusa a conexão de imediato: simula o banco fora do ar (CA-36).</summary>
    public const string ConnectionStringInalcancavel =
        "Server=127.0.0.1,1;Database=JoyaPedidos;User Id=x;Password=x;Connect Timeout=2;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuracao) =>
            configuracao.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
            }));
        builder.ConfigureServices(servicos =>
            servicos.AddControllers().AddApplicationPart(typeof(RotasDeTesteController).Assembly));
    }
}
