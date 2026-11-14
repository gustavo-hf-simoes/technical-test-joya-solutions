namespace Joya.Pedidos.Core.Common;

/// <summary>Executa uma operação de forma atômica (ADR-0004). Sem retentativa automática.</summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Abre a transação, executa a operação e faz commit se ela devolver sucesso. Com <c>Failure</c> faz rollback
    /// e devolve o mesmo resultado; com exceção faz rollback e a exceção sobe.
    /// </summary>
    Task<Result<T>> ExecutarEmTransacaoAsync<T>(Func<CancellationToken, Task<Result<T>>> operacao, CancellationToken ct)
        where T : notnull;
}
