using Joya.Pedidos.IntegrationTests.Infra;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Banco;

/// <summary>
/// T-04.2 · spec §6.3. Confere chaves, ordem, direção, colunas incluídas, unicidade e
/// filtro de cada índice no catálogo, e que não existe índice além dos da spec.
/// As colunas de catálogo entram nos <c>CONCAT</c> com <c>COLLATE DATABASE_DEFAULT</c>: as colunas
/// <c>*_desc</c> e <c>type</c> usam Latin1_General_CI_AS_KS_WS e os nomes usam a collation do banco,
/// e misturar as duas sem coerção explícita é erro 468 no SQL Server.
/// </summary>
[Collection(SqlServerCollection.Nome)]
public sealed class IndicesTests(SqlServerFixture banco) : IAsyncLifetime
{
    private const int _violacaoDeIndiceUnico = 2601;

    public Task InitializeAsync() => banco.ResetAsync(CancellationToken.None);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    [Trait("Spec", "6.3")]
    [Trait("Requisito", "RNF-02")]
    public async Task Indices_ScriptAplicado_ExistemIndicesDaSpec()
    {
        // Índices que não vêm de PRIMARY KEY nem de UNIQUE (estes estão no teste de schema).
        var indices = await Sql.ListarAsync(banco.ConnectionStringSa, """
            SELECT CONCAT(OBJECT_NAME(i.object_id) COLLATE DATABASE_DEFAULT, '.', i.name COLLATE DATABASE_DEFAULT, ' ', i.type_desc COLLATE DATABASE_DEFAULT,
                   CASE WHEN i.is_unique = 1 THEN ' UNIQUE' ELSE '' END,
                   ' (', chaves.lista COLLATE DATABASE_DEFAULT, ')',
                   CASE WHEN incluidas.lista IS NOT NULL THEN CONCAT(' INCLUDE (', incluidas.lista COLLATE DATABASE_DEFAULT, ')') ELSE '' END,
                   CASE WHEN i.has_filter = 1 THEN CONCAT(' WHERE ', i.filter_definition COLLATE DATABASE_DEFAULT) ELSE '' END,
                   CASE WHEN i.is_disabled = 1 THEN ' DESABILITADO' ELSE '' END)
            FROM sys.indexes i
            CROSS APPLY (SELECT STRING_AGG(CONCAT(c.name COLLATE DATABASE_DEFAULT, CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE '' END), ', ')
                                WITHIN GROUP (ORDER BY ic.key_ordinal) AS lista
                         FROM sys.index_columns ic
                         JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                         WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0) chaves
            OUTER APPLY (SELECT STRING_AGG(c.name COLLATE DATABASE_DEFAULT, ', ') WITHIN GROUP (ORDER BY c.name) AS lista
                         FROM sys.index_columns ic
                         JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                         WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 1) incluidas
            WHERE OBJECTPROPERTY(i.object_id, 'IsUserTable') = 1
              AND OBJECT_SCHEMA_NAME(i.object_id) = N'dbo'
              AND i.type > 0
              AND i.is_primary_key = 0
              AND i.is_unique_constraint = 0
            ORDER BY OBJECT_NAME(i.object_id), i.name;
            """, CancellationToken.None);

        indices.ShouldBe([
            "ItensPedido.IX_ItensPedido_ProdutoId NONCLUSTERED (ProdutoId)",
            "Pedidos.IX_Pedidos_ClienteId_DataPedido NONCLUSTERED (ClienteId, DataPedido DESC, Id DESC) INCLUDE (ValorTotal)",
            "Pedidos.IX_Pedidos_DataPedido NONCLUSTERED (DataPedido DESC, Id DESC) INCLUDE (ClienteId, ValorTotal)",
            "Pedidos.UX_Pedidos_ChaveIdempotencia NONCLUSTERED UNIQUE (ChaveIdempotencia) WHERE ([ChaveIdempotencia] IS NOT NULL)",
        ]);
    }

    [Fact]
    [Trait("Regra", "RN-14")]
    public async Task Indices_ChaveIdempotenciaRepetida_RejeitadaENulasPermitidas()
    {
        var ct = CancellationToken.None;
        var chave = new SqlParameter("@chave", Guid.NewGuid());
        await Sql.ExecutarAsync(banco.ConnectionStringSa, """
            INSERT INTO dbo.Clientes (Nome, Email) VALUES (N'Ana', 'ana@exemplo.com');
            INSERT INTO dbo.Pedidos (ClienteId, ValorTotal, ChaveIdempotencia) VALUES (1, 10, NULL), (1, 10, NULL), (1, 10, @chave);
            """, ct, chave);

        var excecao = await Should.ThrowAsync<SqlException>(() => Sql.ExecutarAsync(banco.ConnectionStringSa, """
            INSERT INTO dbo.Pedidos (ClienteId, ValorTotal, ChaveIdempotencia) VALUES (1, 10, @chave);
            """, ct, new SqlParameter("@chave", chave.Value)));

        excecao.Number.ShouldBe(_violacaoDeIndiceUnico);
        excecao.Message.ShouldContain("UX_Pedidos_ChaveIdempotencia");
        (await Sql.EscalarAsync(banco.ConnectionStringSa, "SELECT COUNT(*) FROM dbo.Pedidos;", ct)).ShouldBe(3);
    }
}
