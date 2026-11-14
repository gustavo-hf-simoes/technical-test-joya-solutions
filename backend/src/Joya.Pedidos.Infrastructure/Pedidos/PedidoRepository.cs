using Joya.Pedidos.Core.Common;
using Joya.Pedidos.Core.Pedidos;
using Joya.Pedidos.Infrastructure.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Joya.Pedidos.Infrastructure.Pedidos;

public sealed class PedidoRepository(PedidosDbContext contexto) : IPedidoRepository
{
    /// <summary>Índice único filtrado da RN-14 (database/02-indices.sql).</summary>
    public const string IndiceChaveIdempotencia = "UX_Pedidos_ChaveIdempotencia";

    private const int _violacaoDeIndiceUnico = 2601;
    private const int _violacaoDeChaveUnica = 2627;

    /// <summary>
    /// <c>INSERT</c> do pedido (<c>OUTPUT INSERTED.Id</c>) e dos itens num único <c>SaveChanges</c>, dentro da transação do
    /// <see cref="IUnitOfWork"/>. Só a violação de <see cref="IndiceChaveIdempotencia"/> vira <c>Result</c> (ADR-0004, item 3);
    /// qualquer outra falha de banco continua sendo exceção.
    /// </summary>
    public async Task<Result<int>> AdicionarAsync(Pedido pedido, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(pedido);

        contexto.Pedidos.Add(pedido);
        try
        {
            await contexto.SaveChangesAsync(ct);
        }
        catch (DbUpdateException excecao) when (ViolouChaveIdempotencia(excecao))
        {
            return PedidoErrors.ChaveIdempotenciaDuplicada();
        }

        return pedido.Id;
    }

    private static bool ViolouChaveIdempotencia(DbUpdateException excecao) =>
        excecao.InnerException is SqlException { Number: _violacaoDeIndiceUnico or _violacaoDeChaveUnica } sql
        && sql.Message.Contains(IndiceChaveIdempotencia, StringComparison.Ordinal);
}
