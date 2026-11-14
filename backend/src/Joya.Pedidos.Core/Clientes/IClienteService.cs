namespace Joya.Pedidos.Core.Clientes;

public interface IClienteService
{
    /// <summary>Lista clientes por nome. <paramref name="ativo"/> nulo traz todos (spec 5.5, HL-18).</summary>
    Task<IReadOnlyList<ClienteResponse>> ListarAsync(bool? ativo, CancellationToken ct);
}

public sealed class ClienteService(IClienteRepository clientes) : IClienteService
{
    public Task<IReadOnlyList<ClienteResponse>> ListarAsync(bool? ativo, CancellationToken ct) =>
        clientes.ListarAsync(ativo, ct);
}
