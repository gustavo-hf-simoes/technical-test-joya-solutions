namespace Joya.Pedidos.Core.Pedidos;

/// <summary>Item que não pôde ser debitado. <c>EstoqueDisponivel</c> é informativo (spec 5.2).</summary>
public sealed record ItemSemEstoque(int ProdutoId, string ProdutoNome, int QuantidadeSolicitada, int EstoqueDisponivel);
