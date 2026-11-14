using Joya.Pedidos.Core.Estoque;
using Joya.Pedidos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Joya.Pedidos.Infrastructure.Estoque;

/// <summary>
/// Única escrita de estoque da aplicação (spec 4.3, regra de ouro 4, D7). A decisão "tem saldo?" é do SQL Server, sob o
/// <i>lock</i> de atualização da linha: o C# nunca lê o estoque para decidir (sem <i>check-then-act</i>).
/// </summary>
public sealed class EstoqueRepository(PedidosDbContext contexto) : IEstoqueRepository
{
    public async Task<decimal?> TentarBaixarAsync(int produtoId, int quantidade, CancellationToken ct)
    {
        // Quantidade negativa somaria estoque. A validação estrutural (RN-02) vem antes; isto é erro de programação.
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);

        // RN-10: uma baixa fora da transação do IUnitOfWork não seria desfeita se outro item falhasse.
        if (contexto.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("A baixa de estoque deve rodar dentro de IUnitOfWork.ExecutarEmTransacaoAsync.");
        }

        // FormattableString: cada {valor} vira parâmetro (@p0, @p1...), nunca texto concatenado (RNF-06).
        // Materializa com ToListAsync, sem compor: o EF não pode envolver o UPDATE numa subconsulta.
        // OUTPUT inserted.Preco devolve o preço da mesma instrução que debita (RN-09). 0 linhas = sem baixa.
        var precos = await contexto.Database
            .SqlQuery<decimal>($"""
                UPDATE dbo.Produtos
                SET    Estoque = Estoque - {quantidade}
                OUTPUT inserted.Preco AS Value
                WHERE  Id = {produtoId}
                  AND  Ativo = 1
                  AND  Estoque >= {quantidade}
                """)
            .ToListAsync(ct);

        return precos.Count == 0 ? null : precos[0];
    }
}
