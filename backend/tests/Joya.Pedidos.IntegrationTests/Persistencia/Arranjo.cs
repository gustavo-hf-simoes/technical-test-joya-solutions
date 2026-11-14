using System.Globalization;
using Joya.Pedidos.IntegrationTests.Infra;
using Microsoft.Data.SqlClient;

namespace Joya.Pedidos.IntegrationTests.Persistencia;

/// <summary>Arrange e Assert direto no banco, como <c>sa</c>, sem passar pelo código testado.</summary>
public static class Arranjo
{
    public static async Task<int> InserirClienteAsync(
        string connectionString, string nome, bool ativo, CancellationToken ct)
    {
        var id = await Sql.EscalarAsync(connectionString, """
            INSERT INTO dbo.Clientes (Nome, Email, Ativo) OUTPUT INSERTED.Id VALUES (@nome, @email, @ativo);
            """, ct,
            new SqlParameter("@nome", nome),
            new SqlParameter("@email", $"{Guid.NewGuid():N}@exemplo.com"),
            new SqlParameter("@ativo", ativo));
        return Convert.ToInt32(id, CultureInfo.InvariantCulture);
    }

    public static async Task<int> InserirProdutoAsync(
        string connectionString, string nome, decimal preco, int estoque, bool ativo, CancellationToken ct)
    {
        var id = await Sql.EscalarAsync(connectionString, """
            INSERT INTO dbo.Produtos (Sku, Nome, Preco, Estoque, Ativo) OUTPUT INSERTED.Id
            VALUES (@sku, @nome, @preco, @estoque, @ativo);
            """, ct,
            new SqlParameter("@sku", $"SKU-{Guid.NewGuid():N}"[..20]),
            new SqlParameter("@nome", nome),
            new SqlParameter("@preco", System.Data.SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = preco },
            new SqlParameter("@estoque", estoque),
            new SqlParameter("@ativo", ativo));
        return Convert.ToInt32(id, CultureInfo.InvariantCulture);
    }

    public static async Task<int> EstoqueAsync(string connectionString, int produtoId, CancellationToken ct) =>
        Convert.ToInt32(
            await Sql.EscalarAsync(connectionString, "SELECT Estoque FROM dbo.Produtos WHERE Id = @id;", ct,
                new SqlParameter("@id", produtoId)),
            CultureInfo.InvariantCulture);

    public static async Task<int> ContarPedidosAsync(string connectionString, CancellationToken ct) =>
        Convert.ToInt32(await Sql.EscalarAsync(connectionString, "SELECT COUNT(*) FROM dbo.Pedidos;", ct), CultureInfo.InvariantCulture);

    public static async Task<int> ContarItensAsync(string connectionString, CancellationToken ct) =>
        Convert.ToInt32(await Sql.EscalarAsync(connectionString, "SELECT COUNT(*) FROM dbo.ItensPedido;", ct), CultureInfo.InvariantCulture);

    /// <summary>Grava um pedido com a data dada (UTC) e os itens informados; o total é a soma dos itens.</summary>
    public static async Task<int> InserirPedidoAsync(
        string connectionString,
        int clienteId,
        DateTime dataPedido,
        IReadOnlyList<(int ProdutoId, int Quantidade, decimal Preco)> itens,
        CancellationToken ct)
    {
        var total = itens.Sum(i => i.Quantidade * i.Preco);
        var id = Convert.ToInt32(
            await Sql.EscalarAsync(connectionString, """
                INSERT INTO dbo.Pedidos (ClienteId, DataPedido, ValorTotal) OUTPUT INSERTED.Id
                VALUES (@cliente, @data, @total);
                """, ct,
                new SqlParameter("@cliente", clienteId),
                new SqlParameter("@data", System.Data.SqlDbType.DateTime2) { Scale = 3, Value = dataPedido },
                new SqlParameter("@total", System.Data.SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = total }),
            CultureInfo.InvariantCulture);

        foreach (var item in itens)
        {
            await Sql.ExecutarAsync(connectionString, """
                INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario)
                VALUES (@pedido, @produto, @quantidade, @preco);
                """, ct,
                new SqlParameter("@pedido", id),
                new SqlParameter("@produto", item.ProdutoId),
                new SqlParameter("@quantidade", item.Quantidade),
                new SqlParameter("@preco", System.Data.SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = item.Preco });
        }

        return id;
    }
}
