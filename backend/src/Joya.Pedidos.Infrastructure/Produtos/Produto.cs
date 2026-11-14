namespace Joya.Pedidos.Infrastructure.Produtos;

/// <summary>
/// Linha de <c>dbo.Produtos</c> para as consultas (spec 6.2). O estoque nunca é alterado por este tipo: a baixa é a
/// instrução atômica do <c>EstoqueRepository</c> (spec 4.3, regra de ouro 4). <c>CriadoEm</c> não é mapeado.
/// </summary>
public sealed class Produto
{
    private Produto(int id, string sku, string nome, decimal preco, int estoque, bool ativo)
    {
        Id = id;
        Sku = sku;
        Nome = nome;
        Preco = preco;
        Estoque = estoque;
        Ativo = ativo;
    }

    public int Id { get; private set; }

    public string Sku { get; private set; }

    public string Nome { get; private set; }

    public decimal Preco { get; private set; }

    public int Estoque { get; private set; }

    public bool Ativo { get; private set; }
}
