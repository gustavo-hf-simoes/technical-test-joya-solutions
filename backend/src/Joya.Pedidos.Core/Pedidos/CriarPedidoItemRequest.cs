namespace Joya.Pedidos.Core.Pedidos;

/// <summary>Item do corpo de POST /pedidos (spec 5.2). Não carrega preço: ele vem do banco (RN-09).</summary>
public sealed record CriarPedidoItemRequest(int ProdutoId, int Quantidade);
