using Joya.Pedidos.IntegrationTests.Infra;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Banco;

/// <summary>
/// T-04.1 · spec §6.2 e §4.3 (RCSI). Confere o catálogo do SQL Server depois de
/// <c>database/init.sh</c>, e não o texto dos scripts.
/// As colunas de catálogo entram nos <c>CONCAT</c> com <c>COLLATE DATABASE_DEFAULT</c>: as colunas
/// <c>*_desc</c> e <c>type</c> usam Latin1_General_CI_AS_KS_WS e os nomes usam a collation do banco,
/// e misturar as duas sem coerção explícita é erro 468 no SQL Server.
/// </summary>
[Collection(SqlServerCollection.Nome)]
public sealed class SchemaTests(SqlServerFixture banco) : IAsyncLifetime
{
    private const int _violacaoDeCheckOuFk = 547;
    private const int _violacaoDeChavePrimaria = 2627;

    public Task InitializeAsync() => banco.ResetAsync(CancellationToken.None);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    [Trait("Spec", "6.2")]
    public async Task Schema_ScriptsAplicados_CriaTabelasChavesEConstraints()
    {
        var ct = CancellationToken.None;

        var tabelas = await Sql.ListarAsync(banco.ConnectionStringSa, """
            SELECT name FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo') ORDER BY name;
            """, ct);
        tabelas.ShouldBe(["Clientes", "ItensPedido", "Pedidos", "Produtos"]);

        var colunas = await Sql.ListarAsync(banco.ConnectionStringSa, """
            SELECT CONCAT(TABLE_NAME COLLATE DATABASE_DEFAULT, '.', COLUMN_NAME COLLATE DATABASE_DEFAULT, ' ', DATA_TYPE COLLATE DATABASE_DEFAULT,
                   CASE WHEN DATA_TYPE IN ('varchar', 'nvarchar') THEN CONCAT('(', CHARACTER_MAXIMUM_LENGTH, ')')
                        WHEN DATA_TYPE = 'decimal' THEN CONCAT('(', NUMERIC_PRECISION, ',', NUMERIC_SCALE, ')')
                        WHEN DATA_TYPE = 'datetime2' THEN CONCAT('(', DATETIME_PRECISION, ')')
                        ELSE '' END,
                   CASE WHEN IS_NULLABLE = 'YES' THEN ' NULL' ELSE ' NOT NULL' END,
                   CASE WHEN COLUMNPROPERTY(OBJECT_ID(QUOTENAME(TABLE_SCHEMA) + '.' + QUOTENAME(TABLE_NAME)), COLUMN_NAME, 'IsIdentity') = 1
                        THEN ' IDENTITY' ELSE '' END)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo'
            ORDER BY TABLE_NAME, ORDINAL_POSITION;
            """, ct);
        colunas.ShouldBe([
            "Clientes.Id int NOT NULL IDENTITY",
            "Clientes.Nome nvarchar(150) NOT NULL",
            "Clientes.Email varchar(254) NOT NULL",
            "Clientes.Ativo bit NOT NULL",
            "Clientes.CriadoEm datetime2(3) NOT NULL",
            "ItensPedido.PedidoId int NOT NULL",
            "ItensPedido.ProdutoId int NOT NULL",
            "ItensPedido.Quantidade int NOT NULL",
            "ItensPedido.PrecoUnitario decimal(18,2) NOT NULL",
            "Pedidos.Id int NOT NULL IDENTITY",
            "Pedidos.ClienteId int NOT NULL",
            "Pedidos.DataPedido datetime2(3) NOT NULL",
            "Pedidos.ValorTotal decimal(18,2) NOT NULL",
            "Pedidos.ChaveIdempotencia uniqueidentifier NULL",
            "Produtos.Id int NOT NULL IDENTITY",
            "Produtos.Sku varchar(40) NOT NULL",
            "Produtos.Nome nvarchar(150) NOT NULL",
            "Produtos.Preco decimal(18,2) NOT NULL",
            "Produtos.Estoque int NOT NULL",
            "Produtos.Ativo bit NOT NULL",
            "Produtos.CriadoEm datetime2(3) NOT NULL",
        ]);

        var chaves = await Sql.ListarAsync(banco.ConnectionStringSa, """
            SELECT CONCAT(kc.name COLLATE DATABASE_DEFAULT, ' ', kc.type COLLATE DATABASE_DEFAULT, ' ', OBJECT_NAME(kc.parent_object_id) COLLATE DATABASE_DEFAULT, '(',
                   (SELECT STRING_AGG(c.name COLLATE DATABASE_DEFAULT, ',') WITHIN GROUP (ORDER BY ic.key_ordinal)
                    FROM sys.index_columns ic
                    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                    WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id),
                   ') ', i.type_desc COLLATE DATABASE_DEFAULT)
            FROM sys.key_constraints kc
            JOIN sys.indexes i ON i.object_id = kc.parent_object_id AND i.index_id = kc.unique_index_id
            WHERE kc.schema_id = SCHEMA_ID(N'dbo')
            ORDER BY kc.name;
            """, ct);
        chaves.ShouldBe([
            "PK_Clientes PK Clientes(Id) CLUSTERED",
            "PK_ItensPedido PK ItensPedido(PedidoId,ProdutoId) CLUSTERED",   // RN-03
            "PK_Pedidos PK Pedidos(Id) CLUSTERED",
            "PK_Produtos PK Produtos(Id) CLUSTERED",
            "UQ_Clientes_Email UQ Clientes(Email) NONCLUSTERED",
            "UQ_Produtos_Sku UQ Produtos(Sku) NONCLUSTERED",
        ]);

        var estrangeiras = await Sql.ListarAsync(banco.ConnectionStringSa, """
            SELECT CONCAT(fk.name COLLATE DATABASE_DEFAULT, ' ', OBJECT_NAME(fk.parent_object_id) COLLATE DATABASE_DEFAULT, '(', pc.name COLLATE DATABASE_DEFAULT, ') -> ',
                   OBJECT_NAME(fk.referenced_object_id) COLLATE DATABASE_DEFAULT, '(', rc.name COLLATE DATABASE_DEFAULT, ') ON DELETE ',
                   fk.delete_referential_action_desc COLLATE DATABASE_DEFAULT,
                   CASE WHEN fk.is_disabled = 0 AND fk.is_not_trusted = 0 THEN ' confiavel' ELSE ' NAO_CONFIAVEL' END)
            FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
            JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
            JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
            WHERE fk.schema_id = SCHEMA_ID(N'dbo')
            ORDER BY fk.name;
            """, ct);
        estrangeiras.ShouldBe([
            "FK_ItensPedido_Pedidos ItensPedido(PedidoId) -> Pedidos(Id) ON DELETE NO_ACTION confiavel",
            "FK_ItensPedido_Produtos ItensPedido(ProdutoId) -> Produtos(Id) ON DELETE NO_ACTION confiavel",
            "FK_Pedidos_Clientes Pedidos(ClienteId) -> Clientes(Id) ON DELETE NO_ACTION confiavel",
        ]);

        var checks = await Sql.ListarAsync(banco.ConnectionStringSa, """
            SELECT CONCAT(name COLLATE DATABASE_DEFAULT, ' ', OBJECT_NAME(parent_object_id) COLLATE DATABASE_DEFAULT, ' ', definition COLLATE DATABASE_DEFAULT,
                   CASE WHEN is_disabled = 0 AND is_not_trusted = 0 THEN ' confiavel' ELSE ' NAO_CONFIAVEL' END)
            FROM sys.check_constraints
            WHERE schema_id = SCHEMA_ID(N'dbo')
            ORDER BY name;
            """, ct);
        checks.ShouldBe([
            "CK_Clientes_Nome_Vazio Clientes (len(ltrim(rtrim([Nome])))>(0)) confiavel",
            "CK_ItensPedido_Preco ItensPedido ([PrecoUnitario]>=(0)) confiavel",
            "CK_ItensPedido_Quantidade ItensPedido ([Quantidade]>(0)) confiavel",
            "CK_Pedidos_ValorTotal Pedidos ([ValorTotal]>=(0)) confiavel",
            "CK_Produtos_Estoque Produtos ([Estoque]>=(0)) confiavel",   // RN-08, última linha de defesa
            "CK_Produtos_Nome_Vazio Produtos (len(ltrim(rtrim([Nome])))>(0)) confiavel",
            "CK_Produtos_Preco Produtos ([Preco]>=(0)) confiavel",
        ]);

        var padroes = await Sql.ListarAsync(banco.ConnectionStringSa, """
            SELECT CONCAT(dc.name COLLATE DATABASE_DEFAULT, ' ', OBJECT_NAME(dc.parent_object_id) COLLATE DATABASE_DEFAULT, '.', c.name COLLATE DATABASE_DEFAULT, ' ',
                   dc.definition COLLATE DATABASE_DEFAULT)
            FROM sys.default_constraints dc
            JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
            WHERE dc.schema_id = SCHEMA_ID(N'dbo')
            ORDER BY dc.name;
            """, ct);
        padroes.ShouldBe([
            "DF_Clientes_Ativo Clientes.Ativo ((1))",
            "DF_Clientes_CriadoEm Clientes.CriadoEm (sysutcdatetime())",
            "DF_Pedidos_DataPedido Pedidos.DataPedido (sysutcdatetime())",
            "DF_Produtos_Ativo Produtos.Ativo ((1))",
            "DF_Produtos_CriadoEm Produtos.CriadoEm (sysutcdatetime())",
        ]);

        // Spec §4.3: READ COMMITTED com READ_COMMITTED_SNAPSHOT ON.
        var rcsi = await Sql.EscalarAsync(banco.ConnectionStringSa, """
            SELECT is_read_committed_snapshot_on FROM sys.databases WHERE name = DB_NAME();
            """, ct);
        rcsi.ShouldBe(true);
    }

    [Fact]
    [Trait("Regra", "RN-08")]
    public async Task Schema_EstoqueNegativo_RejeitadoPeloCheck()
    {
        var ct = CancellationToken.None;
        await Sql.ExecutarAsync(banco.ConnectionStringSa, """
            INSERT INTO dbo.Produtos (Sku, Nome, Preco, Estoque) VALUES ('SKU-1', N'Caneta', 2.50, 1);
            """, ct);

        var excecao = await Should.ThrowAsync<SqlException>(() => Sql.ExecutarAsync(banco.ConnectionStringSa, """
            UPDATE dbo.Produtos SET Estoque = Estoque - 2 WHERE Sku = 'SKU-1';
            """, ct));

        excecao.Number.ShouldBe(_violacaoDeCheckOuFk);
        excecao.Message.ShouldContain("CK_Produtos_Estoque");
        (await Sql.EscalarAsync(banco.ConnectionStringSa, "SELECT Estoque FROM dbo.Produtos WHERE Sku = 'SKU-1';", ct))
            .ShouldBe(1);
    }

    [Fact]
    [Trait("Regra", "RN-03")]
    public async Task Schema_ProdutoRepetidoNoPedido_RejeitadoPelaChavePrimaria()
    {
        var ct = CancellationToken.None;
        await Sql.ExecutarAsync(banco.ConnectionStringSa, """
            INSERT INTO dbo.Clientes (Nome, Email) VALUES (N'Ana', 'ana@exemplo.com');
            INSERT INTO dbo.Produtos (Sku, Nome, Preco, Estoque) VALUES ('SKU-1', N'Caneta', 2.50, 10);
            INSERT INTO dbo.Pedidos (ClienteId, ValorTotal) VALUES (1, 2.50);
            INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES (1, 1, 1, 2.50);
            """, ct);

        var excecao = await Should.ThrowAsync<SqlException>(() => Sql.ExecutarAsync(banco.ConnectionStringSa, """
            INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES (1, 1, 2, 2.50);
            """, ct));

        excecao.Number.ShouldBe(_violacaoDeChavePrimaria);
        excecao.Message.ShouldContain("PK_ItensPedido");
    }

    [Fact]
    [Trait("Requisito", "RNF-04")]
    public async Task Scripts_AplicadosDuasVezes_NaoFalhamNemDuplicamObjetos()
    {
        var ct = CancellationToken.None;
        const string contarObjetos = """
            SELECT CONCAT(type, ' ', COUNT(*)) FROM sys.objects
            WHERE schema_id = SCHEMA_ID(N'dbo') AND is_ms_shipped = 0
            GROUP BY type ORDER BY type;
            """;
        var antes = await Sql.ListarAsync(banco.ConnectionStringSa, contarObjetos, ct);

        var resultado = await banco.ExecutarInitAsync(banco.EtapasAplicadas, ct);

        resultado.ExitCode.ShouldBe(0, $"stderr: {resultado.Stderr}");
        resultado.Stderr.ShouldBeEmpty();
        (await Sql.ListarAsync(banco.ConnectionStringSa, contarObjetos, ct)).ShouldBe(antes);
    }
}
