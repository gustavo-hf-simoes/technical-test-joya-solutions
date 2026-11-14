using Joya.Pedidos.Infrastructure.Clientes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Joya.Pedidos.Infrastructure.Persistencia.Configuracoes;

/// <summary>Descreve <c>dbo.Clientes</c> (database/01-schema.sql). O mapeamento não cria nada: sem índices nem defaults.</summary>
internal sealed class ClienteConfiguracao : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes", "dbo");
        builder.HasKey(c => c.Id).HasName("PK_Clientes");

        builder.Property(c => c.Id).ValueGeneratedOnAdd().UseIdentityColumn();
        builder.Property(c => c.Nome).HasMaxLength(150).IsUnicode().IsRequired();
        builder.Property(c => c.Email).HasMaxLength(254).IsUnicode(false).IsRequired();
        builder.Property(c => c.Ativo).IsRequired();
    }
}
