namespace Joya.Pedidos.Core.Pedidos;

/// <summary>Corpo de POST /pedidos (spec 5.2). Itens é anulável: a ausência é tratada pelo validator (RN-01).</summary>
public sealed record CriarPedidoRequest(int ClienteId, IReadOnlyList<CriarPedidoItemRequest>? Itens);
