using Joya.Pedidos.Core.Common;
using Joya.Pedidos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Joya.Pedidos.Infrastructure.Saude;

/// <summary>
/// Readiness pelo mesmo <see cref="PedidosDbContext"/> da aplicação (mesma connection string e login). O
/// <c>CanConnectAsync</c> abre a conexão e devolve <c>false</c> em falha de banco; cancelamento continua subindo.
/// </summary>
public sealed class VerificadorDeBanco(PedidosDbContext contexto, ILogger<VerificadorDeBanco> logger) : IVerificadorDeBanco
{
    public async Task<bool> EstaDisponivelAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(contexto.Database.GetConnectionString()))
        {
            logger.LogWarning("A connection string ConnectionStrings:Default não está configurada.");
            return false;
        }

        var disponivel = await contexto.Database.CanConnectAsync(ct);
        if (!disponivel)
        {
            logger.LogWarning("O banco de dados não respondeu à verificação de readiness.");
        }

        return disponivel;
    }
}
