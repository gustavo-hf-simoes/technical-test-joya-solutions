using Joya.Pedidos.Core.Common;

namespace Joya.Pedidos.Core.Pedidos;

public interface IPedidoRepository
{
    /// <summary>
    /// Grava pedido e itens (dentro da transação do <see cref="IUnitOfWork"/>) e devolve o id gerado.
    /// Falha com <see cref="PedidoErrors.ChaveIdempotenciaDuplicada"/> se violar <c>UX_Pedidos_ChaveIdempotencia</c> (ADR-0004).
    /// </summary>
    Task<Result<int>> AdicionarAsync(Pedido pedido, CancellationToken ct);
}
