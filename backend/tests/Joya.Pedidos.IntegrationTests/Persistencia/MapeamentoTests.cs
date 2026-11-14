using Joya.Pedidos.Core.Common;
using Joya.Pedidos.Core.Pedidos;
using Joya.Pedidos.Infrastructure.Persistencia;
using Joya.Pedidos.IntegrationTests.Infra;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Persistencia;

/// <summary>
/// T-06.4 · spec §6.2 e §6.4. O mapeamento EF Core é escrito à mão sobre o schema de <c>database/*.sql</c> (sem
/// migrations, regra de ouro 9). Estes testes provam que os dois concordam: pelo modelo relacional do EF contra o
/// catálogo e por um pedido gravado pelo EF e lido de volta pelo SQL puro e pelo EF.
/// </summary>
[Collection(SqlServerCollection.Nome)]
public sealed class MapeamentoTests(SqlServerFixture banco) : IAsyncLifetime
{
    private const int _violacaoDeCheckOuFk = 547;

    private static readonly DateTimeOffset _agora = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    private readonly InfraestruturaReal _infra = new(banco.ConnectionStringApp);

    public Task InitializeAsync() => banco.ResetAsync(CancellationToken.None);

    public async Task DisposeAsync() => await _infra.DisposeAsync();

    [Fact]
    [Trait("Spec", "6.2")]
    public async Task Mapeamento_ModeloEf_ColunasIguaisAoSchema()
    {
        var ct = CancellationToken.None;

        // Colunas que existem no banco e o EF não mapeia de propósito: têm DEFAULT e a aplicação não as lê.
        string[] foraDoModelo = ["Clientes.CriadoEm datetime2(3) NOT NULL", "Produtos.CriadoEm datetime2(3) NOT NULL"];

        var catalogo = await Sql.ListarAsync(banco.ConnectionStringSa, """
            SELECT CONCAT(TABLE_NAME COLLATE DATABASE_DEFAULT, '.', COLUMN_NAME COLLATE DATABASE_DEFAULT, ' ', DATA_TYPE COLLATE DATABASE_DEFAULT,
                   CASE WHEN DATA_TYPE IN ('varchar', 'nvarchar') THEN CONCAT('(', CHARACTER_MAXIMUM_LENGTH, ')')
                        WHEN DATA_TYPE = 'decimal' THEN CONCAT('(', NUMERIC_PRECISION, ',', NUMERIC_SCALE, ')')
                        WHEN DATA_TYPE = 'datetime2' THEN CONCAT('(', DATETIME_PRECISION, ')')
                        ELSE '' END,
                   CASE WHEN IS_NULLABLE = 'YES' THEN ' NULL' ELSE ' NOT NULL' END)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo';
            """, ct);

        var restricoesNoCatalogo = await Sql.ListarAsync(banco.ConnectionStringSa, """
            SELECT name FROM sys.key_constraints WHERE type = 'PK' AND schema_id = SCHEMA_ID(N'dbo')
            UNION ALL
            SELECT name FROM sys.foreign_keys WHERE schema_id = SCHEMA_ID(N'dbo');
            """, ct);

        await using var escopo = _infra.CriarEscopo();
        var contexto = escopo.ServiceProvider.GetRequiredService<PedidosDbContext>();
        var modelo = contexto.GetService<IDesignTimeModel>().Model.GetRelationalModel();

        var colunasDoEf = modelo.Tables
            .SelectMany(tabela => tabela.Columns.Select(coluna =>
                $"{tabela.Name}.{coluna.Name} {coluna.StoreType}{(coluna.IsNullable ? " NULL" : " NOT NULL")}"))
            .Order(StringComparer.Ordinal)
            .ToList();

        var restricoesDoEf = modelo.Tables
            .SelectMany(tabela => tabela.ForeignKeyConstraints.Select(fk => fk.Name)
                .Concat(tabela.PrimaryKey is { } pk ? [pk.Name] : []))
            .Order(StringComparer.Ordinal)
            .ToList();

        modelo.Tables.ShouldAllBe(tabela => tabela.Schema == "dbo");
        colunasDoEf.ShouldBe([.. catalogo.Except(foraDoModelo).Order(StringComparer.Ordinal)]);
        restricoesDoEf.ShouldBe([.. restricoesNoCatalogo.Order(StringComparer.Ordinal)]);
    }

    [Fact]
    [Trait("Spec", "6.4")]
    [Trait("RN", "RN-13")]
    public async Task Mapeamento_PedidoGravado_LeIgualAoSchema()
    {
        var ct = CancellationToken.None;
        var clienteId = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Ana", ativo: true, ct);
        var teclado = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 10.50m, 5, ativo: true, ct);
        var mouse = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Mouse", 19.99m, 5, ativo: true, ct);
        var chave = Guid.NewGuid();

        // Instante com sub-milissegundos: a entidade trunca (HL-20) e o datetime2(3) guarda exatamente o mesmo valor.
        var agora = new DateTimeOffset(2026, 11, 14, 12, 34, 56, TimeSpan.Zero).AddTicks(1_234_567);
        var esperado = new DateTime(2026, 11, 14, 12, 34, 56, 123, DateTimeKind.Utc);

        int pedidoId;
        await using (var escopo = _infra.CriarEscopo())
        {
            var unidade = escopo.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var repositorio = escopo.ServiceProvider.GetRequiredService<IPedidoRepository>();
            var pedido = Pedido.Criar(
                clienteId, agora, [ItemPedido.Criar(teclado, 2, 10.50m), ItemPedido.Criar(mouse, 1, 19.99m)], chave);

            var resultado = await unidade.ExecutarEmTransacaoAsync(c => repositorio.AdicionarAsync(pedido, c), ct);

            resultado.IsSuccess.ShouldBeTrue();
            pedidoId = resultado.Value;
            pedidoId.ShouldBePositive();
            pedido.Id.ShouldBe(pedidoId);
        }

        // Lido pelo SQL puro, sem o EF: o que o mapeamento gravou de fato. O reset deixa só este pedido no banco.
        var linhaPedido = await Sql.ListarAsync(banco.ConnectionStringSa, """
            SELECT CONCAT(Id, '|', ClienteId, '|', CONVERT(varchar(23), DataPedido, 126), '|', ValorTotal, '|', ChaveIdempotencia)
            FROM dbo.Pedidos;
            """, ct);
        linhaPedido.ShouldBe([$"{pedidoId}|{clienteId}|2026-11-14T12:34:56.123|40.99|{chave.ToString().ToUpperInvariant()}"]);

        var linhasItens = await Sql.ListarAsync(banco.ConnectionStringSa, """
            SELECT CONCAT(PedidoId, '|', ProdutoId, '|', Quantidade, '|', PrecoUnitario)
            FROM dbo.ItensPedido ORDER BY ProdutoId;
            """, ct);
        linhasItens.ShouldBe([$"{pedidoId}|{teclado}|2|10.50", $"{pedidoId}|{mouse}|1|19.99"]);

        // Lido pelo EF num escopo novo: materializa a entidade (construtor de materialização e campo _itens).
        // Include só aqui, no teste do mapeamento; as leituras da aplicação projetam direto para o DTO (regra de ouro 10).
        await using (var escopo = _infra.CriarEscopo())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<PedidosDbContext>();
            var lido = await contexto.Pedidos.AsNoTracking().Include(p => p.Itens).SingleAsync(p => p.Id == pedidoId, ct);

            lido.ClienteId.ShouldBe(clienteId);
            lido.DataPedido.ShouldBe(esperado);
            lido.DataPedido.Kind.ShouldBe(DateTimeKind.Utc);
            lido.ValorTotal.ShouldBe(40.99m);
            lido.ChaveIdempotencia.ShouldBe(chave);
            lido.Itens.OrderBy(i => i.ProdutoId).Select(i => (i.ProdutoId, i.Quantidade, i.PrecoUnitario, i.Subtotal))
                .ShouldBe([(teclado, 2, 10.50m, 21.00m), (mouse, 1, 19.99m, 19.99m)]);
        }
    }

    [Fact]
    [Trait("RN", "RN-14")]
    public async Task AdicionarAsync_ChaveIdempotenciaRepetida_RetornaChaveIdempotenciaDuplicada()
    {
        var ct = CancellationToken.None;
        var clienteId = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Ana", ativo: true, ct);
        var produtoId = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 10.50m, 5, ativo: true, ct);
        var chave = Guid.NewGuid();

        var primeiro = await GravarAsync(clienteId, produtoId, chave, ct);
        var segundo = await GravarAsync(clienteId, produtoId, chave, ct);

        primeiro.IsSuccess.ShouldBeTrue();
        segundo.IsFailure.ShouldBeTrue();
        segundo.Error.Code.ShouldBe("CHAVE_IDEMPOTENCIA_DUPLICADA");
        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, ct)).ShouldBe(1);
        (await Arranjo.ContarItensAsync(banco.ConnectionStringSa, ct)).ShouldBe(1);
    }

    [Fact]
    [Trait("RN", "RN-14")]
    public async Task AdicionarAsync_SemChaveIdempotencia_GravaVariosPedidos()
    {
        var ct = CancellationToken.None;
        var clienteId = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Ana", ativo: true, ct);
        var produtoId = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 10.50m, 5, ativo: true, ct);

        // O índice único é filtrado (WHERE ChaveIdempotencia IS NOT NULL): vários NULL convivem.
        (await GravarAsync(clienteId, produtoId, chave: null, ct)).IsSuccess.ShouldBeTrue();
        (await GravarAsync(clienteId, produtoId, chave: null, ct)).IsSuccess.ShouldBeTrue();

        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, ct)).ShouldBe(2);
    }

    [Fact]
    public async Task AdicionarAsync_OutraFalhaDeBanco_ContinuaExcecao()
    {
        var ct = CancellationToken.None;
        var produtoId = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 10.50m, 5, ativo: true, ct);
        const int clienteInexistente = 999;

        // Só a violação de UX_Pedidos_ChaveIdempotencia vira Result (ADR-0004, item 3). FK quebrada é inesperada: exceção.
        var excecao = await Should.ThrowAsync<DbUpdateException>(
            () => GravarAsync(clienteInexistente, produtoId, Guid.NewGuid(), ct));

        excecao.InnerException.ShouldBeOfType<SqlException>().Number.ShouldBe(_violacaoDeCheckOuFk);
        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, ct)).ShouldBe(0);
    }

    private async Task<Result<int>> GravarAsync(int clienteId, int produtoId, Guid? chave, CancellationToken ct)
    {
        await using var escopo = _infra.CriarEscopo();
        var unidade = escopo.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IPedidoRepository>();
        var pedido = Pedido.Criar(clienteId, _agora, [ItemPedido.Criar(produtoId, 1, 10.50m)], chave);

        return await unidade.ExecutarEmTransacaoAsync(c => repositorio.AdicionarAsync(pedido, c), ct);
    }
}
