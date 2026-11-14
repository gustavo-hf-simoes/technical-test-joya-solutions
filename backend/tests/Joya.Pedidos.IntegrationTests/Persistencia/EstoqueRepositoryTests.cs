using Joya.Pedidos.Core.Common;
using Joya.Pedidos.Core.Estoque;
using Joya.Pedidos.IntegrationTests.Infra;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Persistencia;

/// <summary>
/// T-06.5 · spec §4.3, RN-08, RN-09 e RN-10. A baixa é uma única instrução <c>UPDATE ... OUTPUT</c>: o SQL Server
/// decide o saldo sob o <i>lock</i> da linha e devolve o preço da mesma instrução. Roda como <c>joya_app</c>.
/// </summary>
[Collection(SqlServerCollection.Nome)]
public sealed class EstoqueRepositoryTests(SqlServerFixture banco) : IAsyncLifetime
{
    private static readonly Error _semBaixa = Error.Conflict("SEM_BAIXA", "A instrução atômica não debitou o estoque.");

    private readonly InfraestruturaReal _infra = new(banco.ConnectionStringApp);

    public Task InitializeAsync() => banco.ResetAsync(CancellationToken.None);

    public async Task DisposeAsync() => await _infra.DisposeAsync();

    [Fact]
    [Trait("RN", "RN-08")]
    [Trait("RN", "RN-09")]
    public async Task TentarBaixarAsync_EstoqueSuficiente_DebitaERetornaPreco()
    {
        var ct = CancellationToken.None;
        var produtoId = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 149.90m, 10, ativo: true, ct);

        var preco = await BaixarAsync(produtoId, 3, ct);

        preco.ShouldBe(149.90m);
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, produtoId, ct)).ShouldBe(7);
    }

    [Fact]
    [Trait("RN", "RN-08")]
    [Trait("CA", "CA-08")]
    public async Task TentarBaixarAsync_EstoqueInsuficiente_NaoDebita()
    {
        var ct = CancellationToken.None;
        var produtoId = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 149.90m, 5, ativo: true, ct);

        var preco = await BaixarAsync(produtoId, 6, ct);

        preco.ShouldBeNull();
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, produtoId, ct)).ShouldBe(5);
    }

    [Fact]
    [Trait("RN", "RN-08")]
    [Trait("CA", "CA-09")]
    public async Task TentarBaixarAsync_ConsomeTodoEstoque_ZeraEstoque()
    {
        var ct = CancellationToken.None;
        var produtoId = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 149.90m, 5, ativo: true, ct);

        var preco = await BaixarAsync(produtoId, 5, ct);

        preco.ShouldBe(149.90m);
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, produtoId, ct)).ShouldBe(0);
    }

    [Fact]
    [Trait("RN", "RN-07")]
    public async Task TentarBaixarAsync_ProdutoInativo_NaoDebita()
    {
        var ct = CancellationToken.None;
        var produtoId = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Antigo", 149.90m, 5, ativo: false, ct);

        // O produto pode ser inativado entre a validação e a baixa: o WHERE Ativo = 1 fecha essa janela.
        var preco = await BaixarAsync(produtoId, 1, ct);

        preco.ShouldBeNull();
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, produtoId, ct)).ShouldBe(5);
    }

    [Fact]
    [Trait("RN", "RN-07")]
    public async Task TentarBaixarAsync_ProdutoInexistente_RetornaNulo()
    {
        (await BaixarAsync(999, 1, CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    [Trait("RN", "RN-09")]
    [Trait("CA", "CA-11")]
    public async Task TentarBaixarAsync_PrecoDeTabelaAlterado_RetornaPrecoVigente()
    {
        var ct = CancellationToken.None;
        var produtoId = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 149.90m, 10, ativo: true, ct);
        (await BaixarAsync(produtoId, 1, ct)).ShouldBe(149.90m);

        await Sql.ExecutarAsync(banco.ConnectionStringSa, "UPDATE dbo.Produtos SET Preco = 199.90 WHERE Id = @id;", ct,
            new SqlParameter("@id", produtoId));

        (await BaixarAsync(produtoId, 1, ct)).ShouldBe(199.90m);
    }

    [Fact]
    [Trait("RN", "RN-10")]
    public async Task TentarBaixarAsync_TransacaoDesfeita_EstoqueVolta()
    {
        var ct = CancellationToken.None;
        var teclado = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 149.90m, 10, ativo: true, ct);
        var mouse = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Mouse", 19.99m, 1, ativo: true, ct);

        await using var escopo = _infra.CriarEscopo();
        var unidade = escopo.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var estoque = escopo.ServiceProvider.GetRequiredService<IEstoqueRepository>();

        // O primeiro item debita, o segundo não tem saldo: a baixa do primeiro tem de ser desfeita (tudo ou nada).
        var resultado = await unidade.ExecutarEmTransacaoAsync<decimal>(async c =>
        {
            (await estoque.TentarBaixarAsync(teclado, 3, c)).ShouldBe(149.90m);
            return await estoque.TentarBaixarAsync(mouse, 2, c) is { } preco ? preco : _semBaixa;
        }, ct);

        resultado.IsFailure.ShouldBeTrue();
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, teclado, ct)).ShouldBe(10);
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, mouse, ct)).ShouldBe(1);
    }

    [Fact]
    [Trait("RN", "RN-10")]
    public async Task TentarBaixarAsync_ForaDeTransacao_LancaENaoDebita()
    {
        var ct = CancellationToken.None;
        var produtoId = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 149.90m, 10, ativo: true, ct);

        await using var escopo = _infra.CriarEscopo();
        var estoque = escopo.ServiceProvider.GetRequiredService<IEstoqueRepository>();

        await Should.ThrowAsync<InvalidOperationException>(() => estoque.TentarBaixarAsync(produtoId, 1, ct));
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, produtoId, ct)).ShouldBe(10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [Trait("RN", "RN-02")]
    public async Task TentarBaixarAsync_QuantidadeNaoPositiva_LancaENaoAlteraEstoque(int quantidade)
    {
        var ct = CancellationToken.None;
        var produtoId = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 149.90m, 10, ativo: true, ct);

        // Quantidade negativa somaria estoque ("Estoque - (-3)"): é barrada antes de chegar ao SQL.
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => BaixarAsync(produtoId, quantidade, ct));
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, produtoId, ct)).ShouldBe(10);
    }

    [Fact]
    [Trait("RN", "RN-08")]
    [Trait("Spec", "4.3")]
    public async Task TentarBaixarAsync_BaixasConcorrentes_NuncaDebitaAlemDoEstoque()
    {
        var ct = CancellationToken.None;
        const int estoqueInicial = 5;
        const int requisicoes = 20;
        var produtoId = await Arranjo.InserirProdutoAsync(
            banco.ConnectionStringSa, "Teclado", 149.90m, estoqueInicial, ativo: true, ct);

        // 20 escopos (20 DbContexts, 20 conexões, 20 transações) liberados ao mesmo tempo, 1 unidade cada.
        // O CA-10 de ponta a ponta (HTTP) é da T-06.7; aqui se prova a instrução atômica isolada.
        using var largada = new SemaphoreSlim(0, requisicoes);
        var tarefas = Enumerable.Range(0, requisicoes)
            .Select(_ => Task.Run(async () =>
            {
                await largada.WaitAsync(ct);
                return await BaixarAsync(produtoId, 1, ct);
            }, ct))
            .ToList();
        largada.Release(requisicoes);
        var precos = await Task.WhenAll(tarefas);

        precos.Count(p => p is not null).ShouldBe(estoqueInicial);
        precos.Count(p => p is null).ShouldBe(requisicoes - estoqueInicial);
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, produtoId, ct)).ShouldBe(0);
    }

    /// <summary>Uma baixa numa transação própria, como faz o <c>PedidoService</c>: confirma se debitou, desfaz se não.</summary>
    private async Task<decimal?> BaixarAsync(int produtoId, int quantidade, CancellationToken ct)
    {
        await using var escopo = _infra.CriarEscopo();
        var unidade = escopo.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var estoque = escopo.ServiceProvider.GetRequiredService<IEstoqueRepository>();

        var resultado = await unidade.ExecutarEmTransacaoAsync<decimal>(
            async c => await estoque.TentarBaixarAsync(produtoId, quantidade, c) is { } preco ? preco : _semBaixa, ct);

        return resultado.IsSuccess ? resultado.Value : null;
    }
}
