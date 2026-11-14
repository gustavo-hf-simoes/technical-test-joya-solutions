using Joya.Pedidos.Core.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Joya.Pedidos.Infrastructure.Saude;

/// <summary>Abre uma conexão e executa <c>SELECT 1</c>. Qualquer falha de conexão significa "indisponível".</summary>
public sealed class VerificadorDeBanco(string? connectionString, ILogger<VerificadorDeBanco> logger) : IVerificadorDeBanco
{
    public async Task<bool> EstaDisponivelAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("A connection string ConnectionStrings:Default não está configurada.");
            return false;
        }

        try
        {
            await using var conexao = new SqlConnection(connectionString);
            await conexao.OpenAsync(ct);
            await using var comando = new SqlCommand("SELECT 1", conexao);
            await comando.ExecuteScalarAsync(ct);
            return true;
        }
        catch (SqlException excecao)
        {
            logger.LogWarning(excecao, "O banco de dados não respondeu à verificação de readiness.");
            return false;
        }
    }
}
