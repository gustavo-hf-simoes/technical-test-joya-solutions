namespace Joya.Pedidos.Core.Estoque;

/// <summary>Única porta que debita estoque (spec 4.3, regra de ouro 4).</summary>
public interface IEstoqueRepository
{
    /// <summary>
    /// Instrução atômica <c>UPDATE ... OUTPUT inserted.Preco WHERE Id = @id AND Ativo = 1 AND Estoque &gt;= @quantidade</c>.
    /// Devolve o preço de tabela capturado na mesma instrução (RN-09) ou <c>null</c> se não houve baixa.
    /// Deve rodar na transação aberta por <see cref="Common.IUnitOfWork"/>.
    /// </summary>
    Task<decimal?> TentarBaixarAsync(int produtoId, int quantidade, CancellationToken ct);
}
