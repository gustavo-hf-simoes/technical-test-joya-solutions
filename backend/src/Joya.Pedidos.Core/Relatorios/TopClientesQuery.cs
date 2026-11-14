namespace Joya.Pedidos.Core.Relatorios;

/// <summary>Parâmetros de GET /relatorios/top-clientes (spec 5.4): as duas datas são obrigatórias (RN-17).</summary>
public sealed record TopClientesQuery(string? DataInicio = null, string? DataFim = null);
