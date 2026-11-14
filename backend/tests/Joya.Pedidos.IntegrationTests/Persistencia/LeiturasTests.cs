using Joya.Pedidos.Core.Clientes;
using Joya.Pedidos.Core.Produtos;
using Joya.Pedidos.IntegrationTests.Infra;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Persistencia;

/// <summary>T-06.4 · leituras de cliente e produto usadas na criação do pedido (Q9 e Q10 da arquitetura).</summary>
[Collection(SqlServerCollection.Nome)]
public sealed class LeiturasTests(SqlServerFixture banco) : IAsyncLifetime
{
    private readonly InfraestruturaReal _infra = new(banco.ConnectionStringApp);

    public Task InitializeAsync() => banco.ResetAsync(CancellationToken.None);

    public async Task DisposeAsync() => await _infra.DisposeAsync();

    [Fact]
    [Trait("RN", "RN-06")]
    public async Task ObterAsync_ClienteExistente_RetornaResumoComAtivo()
    {
        var ct = CancellationToken.None;
        var ativo = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Ana", ativo: true, ct);
        var inativo = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Bia", ativo: false, ct);

        await using var escopo = _infra.CriarEscopo();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IClienteRepository>();

        (await repositorio.ObterAsync(ativo, ct)).ShouldBe(new ClienteResumo(ativo, "Ana", true));
        (await repositorio.ObterAsync(inativo, ct)).ShouldBe(new ClienteResumo(inativo, "Bia", false));
    }

    [Fact]
    [Trait("RN", "RN-05")]
    public async Task ObterAsync_ClienteInexistente_RetornaNulo()
    {
        await using var escopo = _infra.CriarEscopo();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IClienteRepository>();

        (await repositorio.ObterAsync(999, CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    [Trait("RN", "RN-07")]
    public async Task ObterAtivosAsync_AtivosInativosEInexistentes_RetornaSoAtivosOrdenados()
    {
        var ct = CancellationToken.None;
        var teclado = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 10.50m, 5, ativo: true, ct);
        var inativo = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Antigo", 1.00m, 5, ativo: false, ct);
        var mouse = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Mouse", 19.99m, 0, ativo: true, ct);

        await using var escopo = _infra.CriarEscopo();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IProdutoRepository>();

        var ativos = await repositorio.ObterAtivosAsync([mouse, 999, inativo, teclado], ct);

        // Produto ativo sem estoque continua "existente": a falta de estoque é decidida na baixa (RN-08), não aqui.
        ativos.ShouldBe([new ProdutoResumo(teclado, "Teclado"), new ProdutoResumo(mouse, "Mouse")]);
    }

    [Fact]
    [Trait("Spec", "5.2")]
    public async Task ObterEstoquesAsync_Ids_RetornaEstoqueAtualInclusiveDeInativo()
    {
        var ct = CancellationToken.None;
        var teclado = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 10.50m, 7, ativo: true, ct);
        var inativo = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Antigo", 1.00m, 3, ativo: false, ct);

        await using var escopo = _infra.CriarEscopo();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IProdutoRepository>();

        var estoques = await repositorio.ObterEstoquesAsync([teclado, inativo, 999], ct);

        estoques.Count.ShouldBe(2);
        estoques[teclado].ShouldBe(7);
        estoques[inativo].ShouldBe(3);
    }
}
