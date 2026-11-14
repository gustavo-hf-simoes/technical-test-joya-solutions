namespace Joya.Pedidos.Core.Pedidos;

/// <summary>Item do pedido na resposta (spec 5.2).</summary>
public sealed record PedidoItemResponse(int ProdutoId, string ProdutoNome, int Quantidade, decimal PrecoUnitario, decimal Subtotal);
