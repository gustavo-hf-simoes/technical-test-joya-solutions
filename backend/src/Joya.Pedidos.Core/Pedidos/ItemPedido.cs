namespace Joya.Pedidos.Core.Pedidos;

/// <summary>Item de um pedido. O preço é o praticado no momento da baixa (RN-09) e nunca muda (RN-12).</summary>
public sealed class ItemPedido
{
    public const int QuantidadeMinima = 1;
    public const int QuantidadeMaxima = 10_000;

    private ItemPedido(int produtoId, int quantidade, decimal precoUnitario)
    {
        ProdutoId = produtoId;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
    }

    public int ProdutoId { get; private set; }

    public int Quantidade { get; private set; }

    public decimal PrecoUnitario { get; private set; }

    /// <summary>Quantidade × preço unitário, em decimal (RN-11). Não é persistido.</summary>
    public decimal Subtotal => Quantidade * PrecoUnitario;

    /// <summary>Cria um item protegendo os invariantes. Violação é erro de programação: a validação estrutural (RN-02, RN-04) vem antes.</summary>
    public static ItemPedido Criar(int produtoId, int quantidade, decimal precoUnitario)
    {
        if (produtoId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(produtoId), produtoId, "O produto deve ser um inteiro positivo.");
        }

        if (quantidade is < QuantidadeMinima or > QuantidadeMaxima)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantidade), quantidade, $"A quantidade deve estar entre {QuantidadeMinima} e {QuantidadeMaxima}.");
        }

        if (precoUnitario < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(precoUnitario), precoUnitario, "O preço unitário não pode ser negativo.");
        }

        return new ItemPedido(produtoId, quantidade, precoUnitario);
    }
}
