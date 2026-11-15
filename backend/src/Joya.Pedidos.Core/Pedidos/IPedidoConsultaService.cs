using Joya.Pedidos.Core.Common;

namespace Joya.Pedidos.Core.Pedidos;

public interface IPedidoConsultaService
{
    /// <summary>Lista pedidos com filtros opcionais e paginação (RN-15, RN-16). O formato já foi validado antes.</summary>
    Task<Result<PaginaResponse<PedidoResumoResponse>>> ListarAsync(ListarPedidosQuery query, CancellationToken ct);

    /// <summary>Obtém um pedido com os itens e os preços praticados (RN-09); inexistente é <c>PEDIDO_NAO_ENCONTRADO</c> (CA-34).</summary>
    Task<Result<PedidoResponse>> ObterPorIdAsync(int id, CancellationToken ct);
}
