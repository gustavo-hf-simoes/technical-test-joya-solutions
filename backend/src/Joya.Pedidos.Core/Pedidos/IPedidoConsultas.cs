using Joya.Pedidos.Core.Common;

namespace Joya.Pedidos.Core.Pedidos;

/// <summary>Porta de leitura de pedidos: devolve os DTOs já projetados (regra de ouro 10).</summary>
public interface IPedidoConsultas
{
    /// <summary>
    /// Página de pedidos por <c>DataPedido DESC, Id DESC</c> (RN-16), com filtro semiaberto sobre a coluna (RN-15).
    /// Cliente inexistente é só um filtro: devolve página vazia.
    /// </summary>
    Task<PaginaResponse<PedidoResumoResponse>> ListarAsync(
        int? clienteId, Periodo periodo, int page, int pageSize, CancellationToken ct);

    /// <summary>Pedido com os itens (projeção única, spec 5.2); <c>null</c> se o id não existe (CA-34).</summary>
    Task<PedidoResponse?> ObterPorIdAsync(int id, CancellationToken ct);
}
