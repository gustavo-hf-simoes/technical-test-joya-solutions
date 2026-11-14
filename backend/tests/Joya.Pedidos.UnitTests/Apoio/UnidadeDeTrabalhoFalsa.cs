using Joya.Pedidos.Core.Common;

namespace Joya.Pedidos.UnitTests.Apoio;

/// <summary>Executa a operação direto e registra se teria havido commit ou rollback (ADR-0004).</summary>
internal sealed class UnidadeDeTrabalhoFalsa : IUnitOfWork
{
    public int Commits { get; private set; }

    public int Rollbacks { get; private set; }

    public async Task<Result<T>> ExecutarEmTransacaoAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operacao, CancellationToken ct)
        where T : notnull
    {
        var resultado = await operacao(ct);

        if (resultado.IsSuccess)
        {
            Commits++;
        }
        else
        {
            Rollbacks++;
        }

        return resultado;
    }
}
