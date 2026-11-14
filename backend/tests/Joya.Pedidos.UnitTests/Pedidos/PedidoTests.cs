using Joya.Pedidos.Core.Pedidos;
using Shouldly;

namespace Joya.Pedidos.UnitTests.Pedidos;

public sealed class PedidoTests
{
    private static readonly DateTimeOffset _agora = new(2026, 11, 13, 14, 32, 7, TimeSpan.Zero);

    [Fact] // CA-01, RN-11
    public void Criar_ItensValidos_CalculaValorTotalEmDecimal()
    {
        var pedido = Pedido.Criar(7, _agora, [ItemPedido.Criar(12, 2, 149.90m), ItemPedido.Criar(31, 2, 29.95m)]);

        pedido.ValorTotal.ShouldBe(359.70m);
        pedido.Itens.Select(i => i.Subtotal).ShouldBe([299.80m, 59.90m]);
        pedido.ClienteId.ShouldBe(7);
    }

    [Fact] // RN-11: em double 0.1 + 0.2 != 0.3; em decimal é exato
    public void Criar_PrecosComCentavos_SomaSemErroDeBinario()
    {
        var pedido = Pedido.Criar(1, _agora, [ItemPedido.Criar(1, 1, 0.10m), ItemPedido.Criar(2, 1, 0.20m)]);

        pedido.ValorTotal.ShouldBe(0.30m);
    }

    [Fact] // RN-13, HL-20
    public void Criar_InstanteComTicks_TruncaDataEmMilissegundosEmUtc()
    {
        var comTicks = new DateTimeOffset(2026, 11, 13, 14, 32, 7, TimeSpan.Zero).AddTicks(1_234_567);

        var pedido = Pedido.Criar(1, comTicks, [ItemPedido.Criar(1, 1, 1m)]);

        pedido.DataPedido.ShouldBe(new DateTime(2026, 11, 13, 14, 32, 7, 123, DateTimeKind.Utc));
        pedido.DataPedido.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Criar_InstanteComOffset_ConverteParaUtc()
    {
        var local = new DateTimeOffset(2026, 11, 13, 11, 0, 0, TimeSpan.FromHours(-3));

        var pedido = Pedido.Criar(1, local, [ItemPedido.Criar(1, 1, 1m)]);

        pedido.DataPedido.ShouldBe(new DateTime(2026, 11, 13, 14, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Criar_SemItens_Lanca()
    {
        Should.Throw<ArgumentException>(() => Pedido.Criar(1, _agora, []));
    }

    [Fact]
    public void Criar_MaisDeCinquentaItens_Lanca()
    {
        var itens = Enumerable.Range(1, Pedido.MaximoItens + 1).Select(i => ItemPedido.Criar(i, 1, 1m));

        Should.Throw<ArgumentException>(() => Pedido.Criar(1, _agora, itens));
    }

    [Fact]
    public void Criar_CinquentaItens_Aceita()
    {
        var itens = Enumerable.Range(1, Pedido.MaximoItens).Select(i => ItemPedido.Criar(i, 1, 1m));

        Pedido.Criar(1, _agora, itens).Itens.Count.ShouldBe(Pedido.MaximoItens);
    }

    [Fact] // RN-03
    public void Criar_ProdutoRepetido_Lanca()
    {
        Should.Throw<ArgumentException>(
            () => Pedido.Criar(1, _agora, [ItemPedido.Criar(5, 1, 1m), ItemPedido.Criar(5, 2, 1m)]));
    }

    [Theory] // RN-04
    [InlineData(0)]
    [InlineData(-1)]
    public void Criar_ClienteNaoPositivo_Lanca(int clienteId)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => Pedido.Criar(clienteId, _agora, [ItemPedido.Criar(1, 1, 1m)]));
    }

    [Theory] // RN-02
    [InlineData(0)]
    [InlineData(ItemPedido.QuantidadeMaxima + 1)]
    public void CriarItem_QuantidadeForaDoLimite_Lanca(int quantidade)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ItemPedido.Criar(1, quantidade, 1m));
    }

    [Theory] // RN-02: limites aceitos
    [InlineData(ItemPedido.QuantidadeMinima)]
    [InlineData(ItemPedido.QuantidadeMaxima)]
    public void CriarItem_QuantidadeNosLimites_Aceita(int quantidade)
    {
        ItemPedido.Criar(1, quantidade, 1m).Quantidade.ShouldBe(quantidade);
    }

    [Fact]
    public void CriarItem_PrecoNegativo_Lanca()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ItemPedido.Criar(1, 1, -0.01m));
    }

    [Fact]
    public void CriarItem_ProdutoNaoPositivo_Lanca()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ItemPedido.Criar(0, 1, 1m));
    }
}
