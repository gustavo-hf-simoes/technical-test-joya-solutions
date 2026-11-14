using Joya.Pedidos.Core.Common;
using Joya.Pedidos.Infrastructure.Saude;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Joya.Pedidos.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra as implementações das portas. A connection string só vem de configuração (<c>ConnectionStrings:Default</c>).</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Lida na hora do uso: o ambiente de teste injeta a configuração depois do Program montar o builder.
        services.AddScoped<IVerificadorDeBanco>(sp => new VerificadorDeBanco(
            sp.GetRequiredService<IConfiguration>().GetConnectionString("Default"),
            sp.GetRequiredService<ILogger<VerificadorDeBanco>>()));

        return services;
    }
}
