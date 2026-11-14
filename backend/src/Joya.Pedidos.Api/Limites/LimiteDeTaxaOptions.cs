namespace Joya.Pedidos.Api.Limites;

/// <summary>
/// Janela fixa por IP do <c>POST /pedidos</c> (ADR-0002, item 2). Vem de <c>LimiteDeTaxa:CriarPedido</c>.
/// O ambiente de teste eleva <see cref="Permissoes"/> para que CA-10 e CA-37 não esbarrem no limite (HL-19).
/// </summary>
public sealed class LimiteDeTaxaOptions
{
    public const string Secao = "LimiteDeTaxa:CriarPedido";

    /// <summary>Requisições aceitas por IP em cada janela. Padrão do ADR-0002: 10.</summary>
    public int Permissoes { get; init; } = 10;

    /// <summary>Duração da janela, em segundos. Padrão do ADR-0002: 10.</summary>
    public int JanelaSegundos { get; init; } = 10;

    public TimeSpan Janela => TimeSpan.FromSeconds(JanelaSegundos);

    public bool EhValido() => Permissoes > 0 && JanelaSegundos > 0;
}
