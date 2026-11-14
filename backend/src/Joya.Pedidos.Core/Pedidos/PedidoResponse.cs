namespace Joya.Pedidos.Core.Pedidos;

/// <summary>Pedido na resposta (spec 5.2). <c>DataPedido</c> é UTC com precisão de milissegundos (RN-13).</summary>
public sealed record PedidoResponse(
    int Id,
    int ClienteId,
    string ClienteNome,
    DateTime DataPedido,
    decimal ValorTotal,
    IReadOnlyList<PedidoItemResponse> Itens);
