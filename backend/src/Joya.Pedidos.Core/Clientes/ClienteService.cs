namespace Joya.Pedidos.Core.Clientes;

public sealed class ClienteService(IClienteRepository clientes) : IClienteService
{
    public Task<IReadOnlyList<ClienteResponse>> ListarAsync(bool? ativo, CancellationToken ct) =>
        clientes.ListarAsync(ativo, ct);
}
