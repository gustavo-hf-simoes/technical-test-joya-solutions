namespace Joya.Pedidos.Core.Clientes;

/// <summary>Dados mínimos do cliente para validar o pedido (RN-05, RN-06) e montar a resposta.</summary>
public sealed record ClienteResumo(int Id, string Nome, bool Ativo);
