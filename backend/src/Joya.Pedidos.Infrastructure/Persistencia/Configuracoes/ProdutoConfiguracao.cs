using Joya.Pedidos.Infrastructure.Produtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Joya.Pedidos.Infrastructure.Persistencia.Configuracoes;

/// <summary>Descreve <c>dbo.Produtos</c> (database/01-schema.sql). O mapeamento não cria nada: sem índices nem defaults.</summary>
internal sealed class ProdutoConfiguracao : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("Produtos", "dbo");
        builder.HasKey(p => p.Id).HasName("PK_Produtos");

        builder.Property(p => p.Id).ValueGeneratedOnAdd().UseIdentityColumn();
        builder.Property(p => p.Sku).HasMaxLength(40).IsUnicode(false).IsRequired();
        builder.Property(p => p.Nome).HasMaxLength(150).IsUnicode().IsRequired();
        builder.Property(p => p.Preco).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.Estoque).IsRequired();
        builder.Property(p => p.Ativo).IsRequired();
    }
}
