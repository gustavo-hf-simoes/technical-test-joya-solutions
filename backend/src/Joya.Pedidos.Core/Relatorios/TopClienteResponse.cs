namespace Joya.Pedidos.Core.Relatorios;

/// <summary>Linha do ranking de GET /relatorios/top-clientes (spec 5.4). Uma linha por <c>ClienteId</c> (RN-17).</summary>
public sealed record TopClienteResponse(
    int Posicao,
    int ClienteId,
    string ClienteNome,
    decimal ValorTotal,
    int QuantidadePedidos);
