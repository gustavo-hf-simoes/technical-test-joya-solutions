namespace Joya.Pedidos.Core.Produtos;

/// <summary>Item de <c>GET /produtos</c> (spec 5.5). <c>Preco</c> e <c>Estoque</c> são informativos; o <c>POST</c> decide.</summary>
public sealed record ProdutoResponse(int Id, string Sku, string Nome, decimal Preco, int Estoque);
