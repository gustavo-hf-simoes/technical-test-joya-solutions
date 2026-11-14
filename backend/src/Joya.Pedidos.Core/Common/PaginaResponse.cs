namespace Joya.Pedidos.Core.Common;

/// <summary>Página de resultados (spec 5.3).</summary>
public sealed record PaginaResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems)
{
    /// <summary>Zero quando não há itens; caso contrário, o teto de TotalItems / PageSize.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)(((long)TotalItems + PageSize - 1) / PageSize);
}
