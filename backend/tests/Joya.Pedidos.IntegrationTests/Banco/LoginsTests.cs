using Joya.Pedidos.IntegrationTests.Infra;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Banco;

/// <summary>
/// T-04.3 · spec §6.5. Cada login é exercitado de verdade: o que ele pode fazer
/// funciona, e o que não pode é negado pelo SQL Server.
/// </summary>
[Collection(SqlServerCollection.Nome)]
public sealed class LoginsTests(SqlServerFixture banco) : IAsyncLifetime
{
    private const int _permissaoNegadaNoObjeto = 229;
    private const int _permissaoNegadaNoBanco = 262;

    public static TheoryData<string> EscritasDoMcp => new()
    {
        "INSERT INTO dbo.Clientes (Nome, Email) VALUES (N'Intruso', 'intruso@exemplo.com');",
        "INSERT INTO dbo.Produtos (Sku, Nome, Preco, Estoque) VALUES ('X-1', N'Intruso', 1, 1);",
        "INSERT INTO dbo.Pedidos (ClienteId, ValorTotal) VALUES (1, 1);",
        "INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES (1, 1, 1, 1);",
        "UPDATE dbo.Produtos SET Estoque = 0;",
        "DELETE FROM dbo.Pedidos;",
    };

    public async Task InitializeAsync()
    {
        await banco.ResetAsync(CancellationToken.None);

        // Arrange como sa: um cliente, um produto e um pedido com um item.
        await Sql.ExecutarAsync(banco.ConnectionStringSa, """
            INSERT INTO dbo.Clientes (Nome, Email) VALUES (N'Ana', 'ana@exemplo.com');
            INSERT INTO dbo.Produtos (Sku, Nome, Preco, Estoque) VALUES ('SKU-1', N'Caneta', 2.50, 10);
            INSERT INTO dbo.Pedidos (ClienteId, ValorTotal) VALUES (1, 2.50);
            INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES (1, 1, 1, 2.50);
            """, CancellationToken.None);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    [Trait("Regra", "RN-12")]
    [Trait("Spec", "6.5")]
    public async Task LoginApp_TentaDelete_PermissaoNegada()
    {
        var ct = CancellationToken.None;

        var excecao = await Should.ThrowAsync<SqlException>(() =>
            Sql.ExecutarAsync(banco.ConnectionStringApp, "DELETE FROM dbo.Pedidos WHERE Id = 1;", ct));

        excecao.Number.ShouldBe(_permissaoNegadaNoObjeto);
        excecao.Message.ShouldContain("DELETE");
        (await Sql.EscalarAsync(banco.ConnectionStringSa, "SELECT COUNT(*) FROM dbo.Pedidos;", ct)).ShouldBe(1);
    }

    [Fact]
    [Trait("Spec", "6.5")]
    public async Task LoginApp_SelectInsertUpdate_Permitidos()
    {
        var ct = CancellationToken.None;
        var app = banco.ConnectionStringApp;

        (await Sql.EscalarAsync(app, "SELECT COUNT(*) FROM dbo.ItensPedido;", ct)).ShouldBe(1);
        (await Sql.ExecutarAsync(app, """
            INSERT INTO dbo.Pedidos (ClienteId, ValorTotal) VALUES (1, 5.00);
            """, ct)).ShouldBe(1);
        (await Sql.ExecutarAsync(app, "UPDATE dbo.Produtos SET Estoque = Estoque - 1 WHERE Id = 1;", ct)).ShouldBe(1);

        (await Sql.EscalarAsync(banco.ConnectionStringSa, "SELECT Estoque FROM dbo.Produtos WHERE Id = 1;", ct)).ShouldBe(9);
        (await Sql.EscalarAsync(banco.ConnectionStringSa, "SELECT COUNT(*) FROM dbo.Pedidos;", ct)).ShouldBe(2);
    }

    [Fact]
    [Trait("Spec", "6.5")]
    public async Task LoginApp_TentaDdl_PermissaoNegada()
    {
        var excecao = await Should.ThrowAsync<SqlException>(() =>
            Sql.ExecutarAsync(banco.ConnectionStringApp, "CREATE TABLE dbo.Intrusa (Id INT);", CancellationToken.None));

        excecao.Number.ShouldBe(_permissaoNegadaNoBanco);
    }

    [Theory]
    [MemberData(nameof(EscritasDoMcp))]
    [Trait("Spec", "6.5")]
    public async Task LoginMcp_TentaEscrever_PermissaoNegada(string comando)
    {
        var ct = CancellationToken.None;
        const string contagem = """
            SELECT CONCAT((SELECT COUNT(*) FROM dbo.Clientes), '|', (SELECT COUNT(*) FROM dbo.Produtos), '|',
                          (SELECT COUNT(*) FROM dbo.Pedidos), '|', (SELECT COUNT(*) FROM dbo.ItensPedido), '|',
                          (SELECT SUM(Estoque) FROM dbo.Produtos));
            """;
        var antes = await Sql.EscalarAsync(banco.ConnectionStringSa, contagem, ct);

        var excecao = await Should.ThrowAsync<SqlException>(() => Sql.ExecutarAsync(banco.ConnectionStringMcp, comando, ct));

        excecao.Number.ShouldBe(_permissaoNegadaNoObjeto);
        (await Sql.EscalarAsync(banco.ConnectionStringSa, contagem, ct)).ShouldBe(antes);
    }

    [Fact]
    [Trait("Spec", "6.5")]
    public async Task LoginMcp_TentaDdl_PermissaoNegada()
    {
        var excecao = await Should.ThrowAsync<SqlException>(() =>
            Sql.ExecutarAsync(banco.ConnectionStringMcp, "CREATE TABLE dbo.Intrusa (Id INT);", CancellationToken.None));

        excecao.Number.ShouldBe(_permissaoNegadaNoBanco);
    }

    [Fact]
    [Trait("Spec", "6.5")]
    public async Task LoginMcp_LeituraDefinicaoEstadoEPlano_Permitidos()
    {
        var ct = CancellationToken.None;
        var mcp = banco.ConnectionStringMcp;

        // db_datareader
        (await Sql.EscalarAsync(mcp, "SELECT COUNT(*) FROM dbo.Pedidos;", ct)).ShouldBe(1);

        // VIEW DEFINITION: sem ela, a definição do CHECK volta nula.
        (await Sql.EscalarAsync(mcp, """
            SELECT definition FROM sys.check_constraints WHERE name = N'CK_Produtos_Estoque';
            """, ct)).ShouldBe("([Estoque]>=(0))");

        // VIEW DATABASE STATE: DMV de escopo de banco.
        (await Sql.EscalarAsync(mcp, """
            SELECT SUM(row_count) FROM sys.dm_db_partition_stats
            WHERE object_id = OBJECT_ID(N'dbo.Pedidos') AND index_id = 1;
            """, ct)).ShouldBe(1L);

        // SHOWPLAN: o plano estimado volta como XML, sem executar a consulta.
        await using var conexao = new SqlConnection(mcp);
        await conexao.OpenAsync(ct);
        await using (var ligar = new SqlCommand("SET SHOWPLAN_XML ON;", conexao))
        {
            await ligar.ExecuteNonQueryAsync(ct);
        }

        await using var consulta = new SqlCommand("SELECT Id FROM dbo.Pedidos WHERE ClienteId = 1;", conexao);
        var plano = (string?)await consulta.ExecuteScalarAsync(ct);
        plano.ShouldNotBeNull().ShouldContain("ShowPlanXML");
    }
}
