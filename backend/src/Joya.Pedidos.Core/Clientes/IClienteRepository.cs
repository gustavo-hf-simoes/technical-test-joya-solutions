namespace Joya.Pedidos.Core.Clientes;

public interface IClienteRepository
{
    /// <summary>Devolve o cliente (ativo ou não) ou <c>null</c> se não existir.</summary>
    Task<ClienteResumo?> ObterAsync(int id, CancellationToken ct);

    /// <summary>Lista clientes ordenados por nome; <paramref name="ativo"/> nulo não filtra (spec 5.5).</summary>
    Task<IReadOnlyList<ClienteResponse>> ListarAsync(bool? ativo, CancellationToken ct);
}
