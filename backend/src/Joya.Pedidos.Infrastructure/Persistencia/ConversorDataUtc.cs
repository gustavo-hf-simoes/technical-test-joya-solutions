using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Joya.Pedidos.Infrastructure.Persistencia;

/// <summary>
/// As colunas <c>DATETIME2(3)</c> guardam UTC (spec 6.4), mas o SqlClient devolve <see cref="DateTimeKind.Unspecified"/>.
/// Na leitura, marca o valor como UTC para que o JSON saia com <c>Z</c> (spec 5.1). Na escrita, grava o valor como veio:
/// o domínio só produz UTC (<c>Pedido.Criar</c>). Não aplica função sobre a coluna nas consultas (RN-15).
/// </summary>
public sealed class ConversorDataUtc() : ValueConverter<DateTime, DateTime>(
    valor => valor,
    valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));
