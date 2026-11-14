namespace Joya.Pedidos.Core.Pedidos;

/// <summary>Pedido com seus itens. Nasce por <see cref="Criar"/> e não muda depois (RN-12).</summary>
public sealed class Pedido
{
    public const int MinimoItens = 1;
    public const int MaximoItens = 50;

    private readonly List<ItemPedido> _itens;

    private Pedido(int clienteId, DateTime dataPedido, Guid? chaveIdempotencia, List<ItemPedido> itens, decimal valorTotal)
    {
        ClienteId = clienteId;
        DataPedido = dataPedido;
        ChaveIdempotencia = chaveIdempotencia;
        _itens = itens;
        ValorTotal = valorTotal;
    }

    /// <summary>Atribuído pelo banco ao gravar (IDENTITY).</summary>
    public int Id { get; private set; }

    public int ClienteId { get; private set; }

    /// <summary>UTC, truncada em milissegundos (RN-13, HL-20).</summary>
    public DateTime DataPedido { get; private set; }

    /// <summary>Σ (Quantidade × PrecoUnitario), em decimal (RN-11).</summary>
    public decimal ValorTotal { get; private set; }

    public Guid? ChaveIdempotencia { get; private set; }

    public IReadOnlyList<ItemPedido> Itens => _itens;

    /// <summary>
    /// Cria o pedido protegendo os invariantes (1 a 50 itens, sem produto repetido) e calcula o total (RN-11).
    /// A data é truncada em milissegundos para que o <c>201</c> e as leituras devolvam o mesmo instante (RN-13).
    /// </summary>
    public static Pedido Criar(int clienteId, DateTimeOffset agora, IEnumerable<ItemPedido> itens, Guid? chaveIdempotencia = null)
    {
        ArgumentNullException.ThrowIfNull(itens);

        if (clienteId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(clienteId), clienteId, "O cliente deve ser um inteiro positivo.");
        }

        var lista = itens.ToList();
        if (lista.Count is < MinimoItens or > MaximoItens)
        {
            throw new ArgumentException($"O pedido deve ter de {MinimoItens} a {MaximoItens} itens.", nameof(itens));
        }

        if (lista.Select(i => i.ProdutoId).Distinct().Count() != lista.Count)
        {
            throw new ArgumentException("Um produto não pode aparecer em mais de um item do pedido.", nameof(itens));
        }

        var utc = agora.UtcDateTime;
        var dataPedido = new DateTime(utc.Ticks - (utc.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        var valorTotal = lista.Sum(i => i.Subtotal);

        return new Pedido(clienteId, dataPedido, chaveIdempotencia, lista, valorTotal);
    }
}
