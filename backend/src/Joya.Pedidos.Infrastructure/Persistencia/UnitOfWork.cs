using System.Data;
using Joya.Pedidos.Core.Common;
using Microsoft.EntityFrameworkCore;

namespace Joya.Pedidos.Infrastructure.Persistencia;

/// <summary>
/// Transação explícita sobre o <see cref="PedidosDbContext"/> do escopo (ADR-0004, RN-10). Sem <i>retry</i> automático:
/// nenhuma estratégia de execução com repetição no provedor. Quem reenvia é o cliente da API, com a chave de idempotência.
/// </summary>
public sealed class UnitOfWork(PedidosDbContext contexto) : IUnitOfWork
{
    public async Task<Result<T>> ExecutarEmTransacaoAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operacao, CancellationToken ct)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(operacao);

        // READ COMMITTED com READ_COMMITTED_SNAPSHOT ON (spec 4.3). O DisposeAsync desfaz a transação não confirmada,
        // inclusive quando a operação lança ou o commit é cancelado.
        await using var transacao = await contexto.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        Result<T> resultado;
        try
        {
            resultado = await operacao(ct);
        }
        catch
        {
            // Exceção é o inesperado: desfaz (no dispose), esquece o que foi rastreado e deixa subir para o 500.
            contexto.ChangeTracker.Clear();
            throw;
        }

        if (resultado.IsSuccess)
        {
            await transacao.CommitAsync(ct);
            return resultado;
        }

        // Falha de negócio (Result.Failure): nada da tentativa fica no banco nem no rastreador.
        await transacao.RollbackAsync(ct);
        contexto.ChangeTracker.Clear();
        return resultado;
    }
}
