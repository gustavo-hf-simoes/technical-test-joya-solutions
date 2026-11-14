namespace Joya.Pedidos.Core.Pedidos;

/// <summary>Linha da listagem (spec 5.3). <c>QuantidadeItens</c> é o número de linhas do pedido, não a soma das quantidades.</summary>
public sealed record PedidoResumoResponse(
    int Id,
    int ClienteId,
    string ClienteNome,
    DateTime DataPedido,
    decimal ValorTotal,
    int QuantidadeItens);
