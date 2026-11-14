namespace Joya.Pedidos.Core.Relatorios;

/// <summary>Porta de leitura dos relatórios: devolve os DTOs já projetados (regra de ouro 10).</summary>
public interface IRelatorioConsultas
{
    /// <summary>
    /// Até <paramref name="quantidade"/> clientes com maior soma de <c>ValorTotal</c> no intervalo semiaberto
    /// <c>[inicio, fimExclusivo)</c> (RN-15), agrupados por <c>ClienteId</c>, em ordem <c>valorTotal DESC, clienteId ASC</c>,
    /// com <c>Posicao</c> de 1 em diante (RN-17). Clientes sem pedido no intervalo não aparecem.
    /// </summary>
    Task<IReadOnlyList<TopClienteResponse>> TopClientesAsync(
        DateTime inicio, DateTime fimExclusivo, int quantidade, CancellationToken ct);
}
