using System.Globalization;
using Joya.Pedidos.IntegrationTests.Infra;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Banco;

/// <summary>
/// T-04.4 · RNF-04. A carga de exemplo (<c>04-carga.sql</c>) fica fora do fixture (ADR-0003, item 2):
/// estes testes a aplicam por conta própria, pelo mesmo <c>init.sh</c>, e deixam o banco limpo ao sair
/// para que os demais testes (schema, índices, logins, endpoints) não vejam os dados de exemplo.
/// </summary>
[Collection(SqlServerCollection.Nome)]
public sealed class CargaTests(SqlServerFixture banco) : IAsyncLifetime
{
    private static readonly string[] _etapaCarga = ["04-carga"];

    private static readonly string[] _contagens =
    [
        "SELECT COUNT(*) FROM dbo.Clientes",
        "SELECT COUNT(*) FROM dbo.Produtos",
        "SELECT COUNT(*) FROM dbo.Pedidos",
        "SELECT COUNT(*) FROM dbo.ItensPedido",
    ];

    public Task InitializeAsync() => banco.ResetAsync(CancellationToken.None);

    public Task DisposeAsync() => banco.ResetAsync(CancellationToken.None);

    [Fact]
    [Trait("Regra", "RNF-04")]
    public async Task Carga_AplicadaDuasVezes_NaoDuplicaLinhas()
    {
        var ct = CancellationToken.None;

        await AplicarCargaAsync(ct);
        var aposPrimeira = await ContarAsync(ct);
        await AplicarCargaAsync(ct);
        var aposSegunda = await ContarAsync(ct);

        aposPrimeira.ShouldAllBe(contagem => contagem > 0);
        aposSegunda.ShouldBe(aposPrimeira);
    }

    [Fact]
    [Trait("Regra", "RNF-04")]
    public async Task Carga_ComPedidoJaGravado_NaoInsereOsPedidosDeExemplo()
    {
        var ct = CancellationToken.None;
        await Sql.ExecutarAsync(banco.ConnectionStringSa, """
            INSERT INTO dbo.Clientes (Nome, Email) VALUES (N'Existente', 'existente@exemplo.com');
            INSERT INTO dbo.Pedidos (ClienteId, ValorTotal) VALUES (1, 0);
            """, ct);

        await AplicarCargaAsync(ct);

        var pedidos = await Sql.EscalarAsync(banco.ConnectionStringSa, "SELECT COUNT(*) FROM dbo.Pedidos", ct);
        pedidos.ShouldBe(1);
    }

    [Fact]
    [Trait("CA", "CA-33")]
    [Trait("CA", "CA-35")]
    public async Task Carga_Aplicada_ContemHomonimosInativosEPedidosDe2025E2026()
    {
        var ct = CancellationToken.None;

        await AplicarCargaAsync(ct);

        await ConferirAsync("""
            SELECT COUNT(*) FROM (
                SELECT Nome FROM dbo.Clientes GROUP BY Nome HAVING COUNT(*) > 1) AS homonimos
            """, minimo: 2, ct);
        await ConferirAsync("SELECT COUNT(*) FROM dbo.Clientes WHERE Ativo = 0", minimo: 1, ct);
        await ConferirAsync("SELECT COUNT(*) FROM dbo.Produtos WHERE Ativo = 0", minimo: 1, ct);
        await ConferirAsync("SELECT COUNT(*) FROM dbo.Produtos WHERE Ativo = 1 AND Estoque = 0", minimo: 1, ct);
        await ConferirAsync(
            "SELECT COUNT(*) FROM dbo.Pedidos WHERE DataPedido >= '2025-01-01' AND DataPedido < '2026-01-01'", minimo: 1, ct);
        await ConferirAsync(
            "SELECT COUNT(*) FROM dbo.Pedidos WHERE DataPedido >= '2026-01-01' AND DataPedido < '2027-01-01'", minimo: 1, ct);
    }

    [Fact]
    [Trait("Regra", "RN-09")]
    public async Task Carga_Aplicada_ValorTotalIgualASomaDosItens()
    {
        var ct = CancellationToken.None;

        await AplicarCargaAsync(ct);

        var divergentes = await Sql.EscalarAsync(banco.ConnectionStringSa, """
            SELECT COUNT(*)
            FROM dbo.Pedidos p
            WHERE p.ValorTotal <> (SELECT SUM(i.Quantidade * i.PrecoUnitario) FROM dbo.ItensPedido i WHERE i.PedidoId = p.Id)
               OR NOT EXISTS (SELECT 1 FROM dbo.ItensPedido i WHERE i.PedidoId = p.Id)
            """, ct);
        divergentes.ShouldBe(0);
    }

    private async Task AplicarCargaAsync(CancellationToken ct)
    {
        var resultado = await banco.ExecutarInitAsync(_etapaCarga, ct);

        resultado.ExitCode.ShouldBe(0, $"stderr: {resultado.Stderr}{Environment.NewLine}stdout: {resultado.Stdout}");
    }

    private async Task<IReadOnlyList<int>> ContarAsync(CancellationToken ct)
    {
        var contagens = new List<int>();
        foreach (var consulta in _contagens)
        {
            var valor = await Sql.EscalarAsync(banco.ConnectionStringSa, consulta, ct);
            contagens.Add(Convert.ToInt32(valor, CultureInfo.InvariantCulture));
        }

        return contagens;
    }

    private async Task ConferirAsync(string consulta, int minimo, CancellationToken ct)
    {
        var valor = Convert.ToInt32(
            await Sql.EscalarAsync(banco.ConnectionStringSa, consulta, ct),
            CultureInfo.InvariantCulture);

        valor.ShouldBeGreaterThanOrEqualTo(minimo, consulta);
    }
}
