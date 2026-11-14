namespace Joya.Pedidos.Core.Clientes;

public interface IClienteRepository
{
    /// <summary>Devolve o cliente (ativo ou não) ou <c>null</c> se não existir.</summary>
    Task<ClienteResumo?> ObterAsync(int id, CancellationToken ct);
}
