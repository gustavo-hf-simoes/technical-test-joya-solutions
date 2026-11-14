namespace Joya.Pedidos.Core.Clientes;

/// <summary>Item de <c>GET /clientes</c> (spec 5.5).</summary>
public sealed record ClienteResponse(int Id, string Nome, bool Ativo);
