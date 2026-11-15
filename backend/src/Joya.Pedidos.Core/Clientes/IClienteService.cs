namespace Joya.Pedidos.Core.Clientes;

public interface IClienteService
{
    /// <summary>Lista clientes por nome. <paramref name="ativo"/> nulo traz todos (spec 5.5, HL-18).</summary>
    Task<IReadOnlyList<ClienteResponse>> ListarAsync(bool? ativo, CancellationToken ct);
}
