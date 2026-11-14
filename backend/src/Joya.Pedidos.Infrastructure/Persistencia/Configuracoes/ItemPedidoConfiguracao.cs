using Joya.Pedidos.Core.Pedidos;
using Joya.Pedidos.Infrastructure.Produtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Joya.Pedidos.Infrastructure.Persistencia.Configuracoes;

/// <summary>
/// Descreve <c>dbo.ItensPedido</c> (database/01-schema.sql). <c>PedidoId</c> é propriedade de sombra: o item não
/// conhece o pedido no domínio. Chave composta <c>(PedidoId, ProdutoId)</c> = RN-03. <c>Subtotal</c> é calculado e não é coluna.
/// </summary>
internal sealed class ItemPedidoConfiguracao : IEntityTypeConfiguration<ItemPedido>
{
    /// <summary>Nome da propriedade de sombra e da coluna da FK para <c>dbo.Pedidos</c>.</summary>
    public const string PedidoId = "PedidoId";

    public void Configure(EntityTypeBuilder<ItemPedido> builder)
    {
        builder.ToTable("ItensPedido", "dbo");

        builder.Property<int>(PedidoId).IsRequired();
        builder.HasKey(PedidoId, nameof(ItemPedido.ProdutoId)).HasName("PK_ItensPedido");

        builder.Property(i => i.ProdutoId).ValueGeneratedNever().IsRequired();
        builder.Property(i => i.Quantidade).IsRequired();
        builder.Property(i => i.PrecoUnitario).HasPrecision(18, 2).IsRequired();
        builder.Ignore(i => i.Subtotal);

        builder.HasOne<Produto>()
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .HasConstraintName("FK_ItensPedido_Produtos")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
