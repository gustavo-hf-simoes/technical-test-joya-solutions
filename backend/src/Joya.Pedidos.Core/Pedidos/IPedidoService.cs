using Joya.Pedidos.Core.Common;

namespace Joya.Pedidos.Core.Pedidos;

public interface IPedidoService
{
    /// <summary>Cria o pedido: RN-05 a RN-08 nessa ordem, baixa atômica e gravação em uma transação (RN-10).</summary>
    Task<Result<PedidoResponse>> CriarAsync(CriarPedidoRequest request, CancellationToken ct);
}
