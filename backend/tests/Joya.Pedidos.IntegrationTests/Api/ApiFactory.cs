using System.Globalization;
using Joya.Pedidos.Api.Limites;
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

    /// <summary>Permissões por janela do <c>POST /pedidos</c> no ambiente de teste (HL-19).</summary>
    public const int PermissoesNoAmbienteDeTeste = 100_000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuracao) =>
            configuracao.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                // HL-19: limite de taxa elevado para que CA-10 e CA-37 não esbarrem nele.
                // O 429 é provado em LimitesHttpTests com um limite baixo próprio.
                [$"{LimiteDeTaxaOptions.Secao}:Permissoes"] = PermissoesNoAmbienteDeTeste.ToString(CultureInfo.InvariantCulture),
            }));
        builder.ConfigureServices(servicos =>
            servicos.AddControllers().AddApplicationPart(typeof(RotasDeTesteController).Assembly));
    }
}
