using Microsoft.Data.SqlClient;

namespace Joya.Pedidos.IntegrationTests.Infra;

/// <summary>
/// Atalhos de ADO.NET para Arrange e Assert direto no banco. Os textos SQL são fixos,
/// escritos nos próprios testes; valores variáveis entram sempre como parâmetro.
/// </summary>
public static class Sql
{
    /// <summary>Executa um comando e devolve as linhas afetadas.</summary>
    public static async Task<int> ExecutarAsync(
        string connectionString, string comando, CancellationToken ct, params SqlParameter[] parametros)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);
        await using var cmd = new SqlCommand(comando, conexao);
        cmd.Parameters.AddRange(parametros);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Executa uma consulta escalar.</summary>
    public static async Task<object?> EscalarAsync(
        string connectionString, string consulta, CancellationToken ct, params SqlParameter[] parametros)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);
        await using var cmd = new SqlCommand(consulta, conexao);
        cmd.Parameters.AddRange(parametros);
        var valor = await cmd.ExecuteScalarAsync(ct);
        return valor is DBNull ? null : valor;
    }

    /// <summary>
    /// Executa uma consulta de uma coluna de texto e devolve as linhas na ordem do banco.
    /// Feito para conferir o catálogo (<c>sys.*</c>) contra uma lista esperada.
    /// </summary>
    public static async Task<IReadOnlyList<string>> ListarAsync(
        string connectionString, string consulta, CancellationToken ct)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);
        await using var cmd = new SqlCommand(consulta, conexao);
        await using var leitor = await cmd.ExecuteReaderAsync(ct);

        var linhas = new List<string>();
        while (await leitor.ReadAsync(ct))
        {
            linhas.Add(leitor.GetString(0));
        }

        return linhas;
    }
}
