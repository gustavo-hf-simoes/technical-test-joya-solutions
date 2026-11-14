namespace Joya.Pedidos.Core.Common;

/// <summary>Porta para saber se o banco de dados aceita conexões (readiness, CA-36).</summary>
public interface IVerificadorDeBanco
{
    /// <summary>Devolve <c>true</c> quando conseguiu abrir a conexão e executar uma consulta trivial.</summary>
    Task<bool> EstaDisponivelAsync(CancellationToken ct);
}
