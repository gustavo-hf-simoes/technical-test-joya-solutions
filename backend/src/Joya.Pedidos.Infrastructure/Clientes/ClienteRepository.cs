using Joya.Pedidos.Core.Clientes;
using Joya.Pedidos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Joya.Pedidos.Infrastructure.Clientes;

public sealed class ClienteRepository(PedidosDbContext contexto) : IClienteRepository
{
    /// <summary>Seek em <c>PK_Clientes</c> projetando só <c>Id</c>, <c>Nome</c> e <c>Ativo</c> (RN-05, RN-06).</summary>
    public Task<ClienteResumo?> ObterAsync(int id, CancellationToken ct) =>
        contexto.Clientes
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ClienteResumo(c.Id, c.Nome, c.Ativo))
            .FirstOrDefaultAsync(ct);
}
