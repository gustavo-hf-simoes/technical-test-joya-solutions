using Joya.Pedidos.Core.Clientes;
using Joya.Pedidos.Core.Common;
using Joya.Pedidos.Core.Estoque; // T-06.5
using Joya.Pedidos.Core.Pedidos;
using Joya.Pedidos.Core.Produtos;
using Joya.Pedidos.Infrastructure.Clientes;
using Joya.Pedidos.Infrastructure.Estoque; // T-06.5
using Joya.Pedidos.Infrastructure.Pedidos;
using Joya.Pedidos.Infrastructure.Persistencia;
using Joya.Pedidos.Infrastructure.Produtos;
using Joya.Pedidos.Infrastructure.Saude;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Joya.Pedidos.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Nome da connection string (<c>ConnectionStrings:Default</c>), que só vem de configuração ou ambiente.</summary>
    public const string ConnectionStringPadrao = "Default";

    /// <summary>Registra o <see cref="PedidosDbContext"/> e as implementações das portas, todos com tempo de vida do escopo.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // --- T-06.4: DbContext, unidade de trabalho e repositórios ---
        // Connection string lida na hora do uso: o ambiente de teste injeta a configuração depois do Program montar o builder.
        // Sem repetição automática no provedor (ADR-0004, HL-23): repetir um commit cuja confirmação se perdeu duplicaria o pedido.
        services.AddDbContext<PedidosDbContext>((provedor, opcoes) => opcoes.UseSqlServer(
            provedor.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringPadrao)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IVerificadorDeBanco, VerificadorDeBanco>();

        // --- T-06.5: baixa atômica de estoque (spec 4.3), no mesmo DbContext e na mesma transação do IUnitOfWork ---
        services.AddScoped<IEstoqueRepository, EstoqueRepository>();

        return services;
    }
}
