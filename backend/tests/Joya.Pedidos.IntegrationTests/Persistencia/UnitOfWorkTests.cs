using Joya.Pedidos.Core.Common;
using Joya.Pedidos.Core.Pedidos;
using Joya.Pedidos.Infrastructure.Persistencia;
using Joya.Pedidos.IntegrationTests.Infra;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Persistencia;

/// <summary>
/// T-06.4 · RN-10 e ADR-0004. A unidade de trabalho confirma em <c>Success</c>, desfaz em <c>Failure</c> e em exceção,
/// e limpa o <c>ChangeTracker</c> para que nada desfeito no banco continue "vivo" no contexto.
/// </summary>
[Collection(SqlServerCollection.Nome)]
public sealed class UnitOfWorkTests(SqlServerFixture banco) : IAsyncLifetime
{
    private static readonly DateTimeOffset _agora = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    private readonly InfraestruturaReal _infra = new(banco.ConnectionStringApp);

    public Task InitializeAsync() => banco.ResetAsync(CancellationToken.None);

    public async Task DisposeAsync() => await _infra.DisposeAsync();

    [Fact]
    [Trait("RN", "RN-10")]
    public async Task ExecutarEmTransacaoAsync_OperacaoSucesso_ConfirmaAlteracoes()
    {
        var ct = CancellationToken.None;
        var (clienteId, produtoId) = await ArranjarAsync(ct);

        await using var escopo = _infra.CriarEscopo();
        var unidade = escopo.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IPedidoRepository>();

        var resultado = await unidade.ExecutarEmTransacaoAsync(
            c => repositorio.AdicionarAsync(NovoPedido(clienteId, produtoId), c), ct);

        resultado.IsSuccess.ShouldBeTrue();
        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, ct)).ShouldBe(1);
        (await Arranjo.ContarItensAsync(banco.ConnectionStringSa, ct)).ShouldBe(1);
    }

    [Fact]
    [Trait("RN", "RN-10")]
    public async Task ExecutarEmTransacaoAsync_OperacaoFalha_DesfazAlteracoes()
    {
        var ct = CancellationToken.None;
        var (clienteId, produtoId) = await ArranjarAsync(ct);
        var falha = PedidoErrors.ClienteInativo(clienteId);

        await using var escopo = _infra.CriarEscopo();
        var unidade = escopo.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IPedidoRepository>();
        var contexto = escopo.ServiceProvider.GetRequiredService<PedidosDbContext>();

        // O INSERT chega ao banco (SaveChanges) e só depois a operação decide falhar: o rollback precisa desfazê-lo.
        var resultado = await unidade.ExecutarEmTransacaoAsync<int>(async c =>
        {
            var gravado = await repositorio.AdicionarAsync(NovoPedido(clienteId, produtoId), c);
            gravado.IsSuccess.ShouldBeTrue();
            return falha;
        }, ct);

        resultado.IsFailure.ShouldBeTrue();
        resultado.Error.ShouldBe(falha);
        contexto.ChangeTracker.Entries().ShouldBeEmpty();
        contexto.Database.CurrentTransaction.ShouldBeNull();
        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, ct)).ShouldBe(0);
        (await Arranjo.ContarItensAsync(banco.ConnectionStringSa, ct)).ShouldBe(0);
    }

    [Fact]
    [Trait("RN", "RN-10")]
    public async Task ExecutarEmTransacaoAsync_OperacaoLanca_DesfazEPropaga()
    {
        var ct = CancellationToken.None;
        var (clienteId, produtoId) = await ArranjarAsync(ct);

        await using var escopo = _infra.CriarEscopo();
        var unidade = escopo.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IPedidoRepository>();
        var contexto = escopo.ServiceProvider.GetRequiredService<PedidosDbContext>();

        await Should.ThrowAsync<InvalidOperationException>(() => unidade.ExecutarEmTransacaoAsync<int>(async c =>
        {
            await repositorio.AdicionarAsync(NovoPedido(clienteId, produtoId), c);
            throw new InvalidOperationException("falha inesperada depois do INSERT");
        }, ct));

        contexto.ChangeTracker.Entries().ShouldBeEmpty();
        contexto.Database.CurrentTransaction.ShouldBeNull();
        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, ct)).ShouldBe(0);
        (await Arranjo.ContarItensAsync(banco.ConnectionStringSa, ct)).ShouldBe(0);
    }

    [Fact]
    [Trait("RN", "RN-10")]
    public async Task ExecutarEmTransacaoAsync_DepoisDeFalha_MesmoEscopoGravaDeNovo()
    {
        var ct = CancellationToken.None;
        var (clienteId, produtoId) = await ArranjarAsync(ct);

        await using var escopo = _infra.CriarEscopo();
        var unidade = escopo.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IPedidoRepository>();

        // Sem o ChangeTracker.Clear(), o pedido desfeito seria reenviado no próximo SaveChanges do mesmo contexto.
        await unidade.ExecutarEmTransacaoAsync<int>(async c =>
        {
            await repositorio.AdicionarAsync(NovoPedido(clienteId, produtoId), c);
            return PedidoErrors.ClienteInativo(clienteId);
        }, ct);
        var segundo = await unidade.ExecutarEmTransacaoAsync(
            c => repositorio.AdicionarAsync(NovoPedido(clienteId, produtoId), c), ct);

        segundo.IsSuccess.ShouldBeTrue();
        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, ct)).ShouldBe(1);
        (await Arranjo.ContarItensAsync(banco.ConnectionStringSa, ct)).ShouldBe(1);
    }

    private async Task<(int ClienteId, int ProdutoId)> ArranjarAsync(CancellationToken ct) =>
        (await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Ana", ativo: true, ct),
         await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 10.50m, 5, ativo: true, ct));

    private static Pedido NovoPedido(int clienteId, int produtoId) =>
        Pedido.Criar(clienteId, _agora, [ItemPedido.Criar(produtoId, 1, 10.50m)]);
}
