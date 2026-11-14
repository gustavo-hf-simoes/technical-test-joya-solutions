namespace Joya.Pedidos.Core.Pedidos;

/// <summary>
/// Parâmetros de GET /pedidos (spec 5.3). As datas chegam como texto e o validator confere o formato <c>yyyy-MM-dd</c>
/// (o <i>model binding</i> de <c>DateOnly</c> aceitaria outros formatos).
/// </summary>
public sealed record ListarPedidosQuery(
    int? ClienteId = null,
    string? DataInicio = null,
    string? DataFim = null,
    int Page = ListarPedidosQueryValidator.PaginaPadrao,
    int PageSize = ListarPedidosQueryValidator.TamanhoPadrao);
