using Joya.Pedidos.Core.Pedidos;
using Joya.Pedidos.Infrastructure.Clientes;
using Joya.Pedidos.Infrastructure.Persistencia.Configuracoes;
using Joya.Pedidos.Infrastructure.Produtos;
using Microsoft.EntityFrameworkCore;

namespace Joya.Pedidos.Infrastructure.Persistencia;

/// <summary>
/// Mapeamento EF Core sobre o schema de database/*.sql, que é a única fonte de verdade (regra de ouro 9).
/// O modelo descreve o banco e nunca o cria: sem migrations, sem criar tabelas e sem declarar índices.
/// </summary>
public sealed class PedidosDbContext(DbContextOptions<PedidosDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Produto> Produtos => Set<Produto>();

    public DbSet<Pedido> Pedidos => Set<Pedido>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // Todo instante é DATETIME2(3) em UTC (spec 6.4, RN-13).
        configurationBuilder.Properties<DateTime>()
            .HavePrecision(3)
            .HaveConversion<ConversorDataUtc>();

        // T-06.5: escalar decimal da baixa (OUTPUT inserted.Preco) com o mesmo tipo das colunas de dinheiro (regra de ouro 7).
        configurationBuilder.DefaultTypeMapping<decimal>().HasPrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new ClienteConfiguracao());
        modelBuilder.ApplyConfiguration(new ProdutoConfiguracao());
        modelBuilder.ApplyConfiguration(new PedidoConfiguracao());
        modelBuilder.ApplyConfiguration(new ItemPedidoConfiguracao());
    }
}
