namespace Joya.Pedidos.Api.Limites;

/// <summary>Limites contra abuso (RNF-06, HL-12). Valores do ADR-0002.</summary>
public static class LimitesHttp
{
    /// <summary>
    /// Tamanho máximo do corpo: 32 KiB, cerca de 4× o maior pedido válido indentado (ADR-0002, item 1).
    /// Vale para o Kestrel inteiro e, pelo <see cref="LimiteDeCorpoFilter"/> global, para toda action MVC
    /// (o <c>TestServer</c> ignora o limite do Kestrel e o <c>[RequestSizeLimit]</c>).
    /// </summary>
    public const int CorpoMaximoBytes = 32 * 1024;

    /// <summary>
    /// Política de limite de taxa do <c>POST /pedidos</c> (ADR-0002, item 2).
    /// A action do <c>POST /pedidos</c> (T-06.4) recebe <c>[EnableRateLimiting(LimitesHttp.PoliticaCriarPedido)]</c>.
    /// </summary>
    public const string PoliticaCriarPedido = "criar-pedido";
}
