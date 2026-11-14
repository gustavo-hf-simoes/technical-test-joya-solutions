using Joya.Pedidos.Core.Pedidos;
using Joya.Pedidos.Infrastructure.Clientes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Joya.Pedidos.Infrastructure.Persistencia.Configuracoes;

/// <summary>
/// Descreve <c>dbo.Pedidos</c> (database/01-schema.sql). <c>DataPedido</c> sem <c>HasDefaultValueSql</c>: o valor vem
/// sempre do <c>TimeProvider</c> (RN-13). Índices só em database/02-indices.sql (nenhum índice declarado no modelo).
/// </summary>
internal sealed class PedidoConfiguracao : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos", "dbo");
        builder.HasKey(p => p.Id).HasName("PK_Pedidos");

        builder.Property(p => p.Id).ValueGeneratedOnAdd().UseIdentityColumn();
        builder.Property(p => p.ClienteId).IsRequired();
        builder.Property(p => p.DataPedido).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(p => p.ValorTotal).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.ChaveIdempotencia).IsRequired(false);

        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(p => p.ClienteId)
            .HasConstraintName("FK_Pedidos_Clientes")
            .OnDelete(DeleteBehavior.NoAction);

        // Agregado: itens só pelo pedido, gravados junto com ele. A lista pública é somente leitura (regra de ouro 2).
        builder.HasMany(p => p.Itens)
            .WithOne()
            .HasForeignKey(ItemPedidoConfiguracao.PedidoId)
            .HasConstraintName("FK_ItensPedido_Pedidos")
            .OnDelete(DeleteBehavior.NoAction);
        builder.Navigation(p => p.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
