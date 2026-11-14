using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Joya.Pedidos.IntegrationTests.Api;
using Joya.Pedidos.IntegrationTests.Infra;
using Joya.Pedidos.IntegrationTests.Persistencia;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Pedidos;

/// <summary>
/// Atomicidade do estoque sob concorrência (spec 4.3, RN-08, RN-10, CA-10), com <c>POST /pedidos</c> de ponta a ponta
/// contra o SQL Server real e a API conectada como <c>joya_app</c>.
/// <para>
/// <b>Largada real.</b> Uma barreira de aplicação solta as 20 requisições de uma vez, e uma barreira no banco garante
/// que elas se sobrepõem: antes da largada, o teste segura com <c>sa</c> um <i>lock</i> <c>UPDLOCK</c> na linha do
/// produto e só o solta quando as 20 sessões da API estão paradas esperando esse <i>lock</i> dentro da própria
/// transação. Assim toda requisição já leu o catálogo (<c>READ_COMMITTED_SNAPSHOT</c>) antes de qualquer baixa, e as
/// 20 baixas disputam a mesma linha no mesmo instante. Uma implementação <i>check-then-act</i> venderia acima do
/// estoque de forma determinística aqui, e não só "às vezes".
/// </para>
/// <para>Sem <i>retry</i> (ADR-0004, HL-23): um <i>deadlock</i> apareceria como <c>500</c>, e o teste exige zero.</para>
/// </summary>
[Collection(SqlServerCollection.Nome)]
public sealed class ConcorrenciaTests(SqlServerFixture banco) : IAsyncLifetime
{
    private const int _requisicoes = 20;
    private const decimal _preco = 19.90m;

    /// <summary>Tempo para as 20 sessões chegarem ao <i>lock</i>. Menor que o timeout de comando do EF (30 s).</summary>
    private static readonly TimeSpan _prazoParaEnfileirar = TimeSpan.FromSeconds(15);

    /// <summary>Teto de segurança para o teste nunca travar a suíte se algo sair do previsto.</summary>
    private static readonly TimeSpan _prazoParaResponder = TimeSpan.FromSeconds(60);

    private static readonly CancellationToken _ct = CancellationToken.None;

    private readonly ApiFactory _factory = new(banco.ConnectionStringApp);

    public Task InitializeAsync() => banco.ResetAsync(_ct);

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData(5, 1, 5, 0)] // CA-10 literal: estoque 5, 20 × 1 unidade → 5 × 201, 15 × 409, estoque final 0
    [InlineData(7, 2, 3, 1)] // sobra que não atende ninguém: 3 × 2 = 6 debitadas, estoque final 1, nunca negativo
    [Trait("CA", "CA-10")]
    public async Task PostPedidos_VinteRequisicoesSimultaneas_EstoqueNuncaFicaNegativo(
        int estoqueInicial, int quantidade, int aceitosEsperados, int estoqueFinalEsperado)
    {
        var clienteId = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Cliente", ativo: true, _ct);
        var produtoId = await Arranjo.InserirProdutoAsync(
            banco.ConnectionStringSa, "Produto Disputado", _preco, estoqueInicial, ativo: true, _ct);
        var corpos = Enumerable.Range(0, _requisicoes)
            .Select(_ => (object)new { clienteId, itens = new[] { new { produtoId, quantidade } } })
            .ToList();

        var (respostas, enfileiradas) = await DispararSimultaneamenteAsync(corpos, [produtoId]);

        enfileiradas.ShouldBe(_requisicoes, "as 20 baixas precisam disputar o lock ao mesmo tempo");
        respostas.Select(r => r.Status).ShouldAllBe(s => s == HttpStatusCode.Created || s == HttpStatusCode.Conflict);
        var aceitos = respostas.Where(r => r.Status == HttpStatusCode.Created).ToList();
        var recusados = respostas.Where(r => r.Status == HttpStatusCode.Conflict).ToList();
        aceitos.Count.ShouldBe(aceitosEsperados);
        recusados.Count.ShouldBe(_requisicoes - aceitosEsperados);

        // Débito do estoque = 201 × quantidade; o saldo nunca passa de zero para baixo.
        var estoqueFinal = await Arranjo.EstoqueAsync(banco.ConnectionStringSa, produtoId, _ct);
        estoqueFinal.ShouldBe(estoqueFinalEsperado);
        (estoqueInicial - estoqueFinal).ShouldBe(aceitos.Count * quantidade);
        estoqueFinal.ShouldBeGreaterThanOrEqualTo(0);

        foreach (var recusado in recusados)
        {
            ConferirEstoqueInsuficiente(recusado, produtoId, quantidade, estoqueFinalEsperado);
        }

        // Banco coerente com os 201: um pedido e um item por aceite, ids iguais aos devolvidos, sem sobra de rollback.
        var idsDevolvidos = aceitos.Select(a => a.Corpo.GetProperty("id").GetInt32()).Order().ToList();
        (await IdsDosPedidosAsync()).ShouldBe(idsDevolvidos);
        (await Arranjo.ContarItensAsync(banco.ConnectionStringSa, _ct)).ShouldBe(aceitosEsperados);
        (await ContarAsync("""
            SELECT COUNT(*) FROM dbo.ItensPedido
            WHERE ProdutoId = @produtoId AND Quantidade = @quantidade AND PrecoUnitario = @preco;
            """, new SqlParameter("@produtoId", produtoId), new SqlParameter("@quantidade", quantidade), PrecoParametro()))
            .ShouldBe(aceitosEsperados);
        (await PedidosComTotalDivergenteAsync()).ShouldBe(0);
        foreach (var aceito in aceitos)
        {
            aceito.TipoDeConteudo.ShouldBe("application/json");
            aceito.Corpo.GetProperty("valorTotal").GetDecimal().ShouldBe(quantidade * _preco);
        }
    }

    /// <summary>
    /// Spec 4.3 item 3: metade dos corpos lista os produtos em ordem crescente e metade em ordem decrescente. Como o
    /// serviço baixa sempre em ordem crescente de <c>ProdutoId</c>, ninguém segura B esperando A enquanto outro segura
    /// A esperando B. Se a ordem do corpo fosse respeitada, as duas filas da largada formariam um ciclo de
    /// <i>lock</i> e o SQL Server escolheria vítimas de <i>deadlock</i> (500, sem <i>retry</i> pela ADR-0004).
    /// </summary>
    [Fact]
    [Trait("CA", "CA-10")]
    public async Task PostPedidos_DoisProdutosEmOrdensOpostas_SemDeadlockNemEstoqueNegativo()
    {
        const int estoqueA = 5;
        const int estoqueB = 8;
        var clienteId = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Cliente", ativo: true, _ct);
        var produtoA = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Produto A", _preco, estoqueA, ativo: true, _ct);
        var produtoB = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Produto B", _preco, estoqueB, ativo: true, _ct);
        var crescente = new[] { new { produtoId = produtoA, quantidade = 1 }, new { produtoId = produtoB, quantidade = 1 } };
        var decrescente = new[] { new { produtoId = produtoB, quantidade = 1 }, new { produtoId = produtoA, quantidade = 1 } };
        var corpos = Enumerable.Range(0, _requisicoes)
            .Select(i => (object)new { clienteId, itens = i % 2 == 0 ? crescente : decrescente })
            .ToList();

        var (respostas, enfileiradas) = await DispararSimultaneamenteAsync(corpos, [produtoA, produtoB]);

        enfileiradas.ShouldBe(_requisicoes, "as 20 transações precisam disputar os locks ao mesmo tempo");
        respostas.Select(r => r.Status).ShouldAllBe(s => s == HttpStatusCode.Created || s == HttpStatusCode.Conflict);
        var aceitos = respostas.Count(r => r.Status == HttpStatusCode.Created);
        aceitos.ShouldBe(estoqueA); // A é o gargalo: 1 unidade por pedido
        foreach (var recusado in respostas.Where(r => r.Status == HttpStatusCode.Conflict))
        {
            // B nunca falta (sobram 3), então só A aparece na lista, com saldo zero (spec 4.3 item 4).
            ConferirEstoqueInsuficiente(recusado, produtoA, quantidadeSolicitada: 1, estoqueDisponivel: 0);
        }

        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, produtoA, _ct)).ShouldBe(0);
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, produtoB, _ct)).ShouldBe(estoqueB - aceitos); // RN-10
        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, _ct)).ShouldBe(aceitos);
        (await Arranjo.ContarItensAsync(banco.ConnectionStringSa, _ct)).ShouldBe(2 * aceitos);
        (await ContarAsync("""
            SELECT COUNT(*) FROM dbo.Pedidos AS p
            WHERE (SELECT COUNT(*) FROM dbo.ItensPedido AS i WHERE i.PedidoId = p.Id) <> 2;
            """)).ShouldBe(0);
        (await PedidosComTotalDivergenteAsync()).ShouldBe(0);
    }

    /// <summary>
    /// Segura os produtos com <c>UPDLOCK</c> numa transação de <c>sa</c>, solta as requisições juntas, espera todas
    /// pararem no <i>lock</i> e só então libera. Devolve as respostas e quantas sessões ficaram enfileiradas ao mesmo
    /// tempo. O <i>lock</i> é liberado no <c>finally</c>, mesmo se a espera estourar o prazo.
    /// </summary>
    private async Task<(IReadOnlyList<Resposta> Respostas, int Enfileiradas)> DispararSimultaneamenteAsync(
        IReadOnlyList<object> corpos, IReadOnlyList<int> produtosTravados)
    {
        using var cliente = _factory.CreateClient();
        var largada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        List<Task<Resposta>> envios;
        int enfileiradas;

        await using (var trava = new SqlConnection(banco.ConnectionStringSa))
        {
            await trava.OpenAsync(_ct);
            await using var transacao = (SqlTransaction)await trava.BeginTransactionAsync(_ct);
            foreach (var produtoId in produtosTravados)
            {
                await using var travar = new SqlCommand(
                    "SELECT Estoque FROM dbo.Produtos WITH (UPDLOCK, ROWLOCK) WHERE Id = @id;", trava, transacao);
                travar.Parameters.Add(new SqlParameter("@id", produtoId));
                await travar.ExecuteScalarAsync(_ct);
            }

            try
            {
                envios = [.. corpos.Select(corpo => EnviarAposLargadaAsync(cliente, corpo, largada.Task))];
                largada.SetResult();
                enfileiradas = await EsperarSessoesNoLockAsync(corpos.Count);
            }
            finally
            {
                await transacao.RollbackAsync(_ct); // solta o lock: as baixas seguem uma a uma, na ordem do SQL Server
            }
        }

        var respostas = await Task.WhenAll(envios).WaitAsync(_prazoParaResponder, _ct);
        return (respostas, enfileiradas);
    }

    private static async Task<Resposta> EnviarAposLargadaAsync(HttpClient cliente, object corpo, Task largada)
    {
        await largada;
        using var resposta = await cliente.PostAsJsonAsync("/pedidos", corpo, _ct);
        var conteudo = await resposta.Content.ReadFromJsonAsync<JsonElement>(_ct);
        return new Resposta(resposta.StatusCode, resposta.Content.Headers.ContentType?.MediaType, conteudo);
    }

    /// <summary>Conta as requisições do login da API paradas em espera de <i>lock</i>, até chegar ao total ou ao prazo.</summary>
    private async Task<int> EsperarSessoesNoLockAsync(int esperadas)
    {
        var cronometro = Stopwatch.StartNew();
        var noLock = 0;
        while (cronometro.Elapsed < _prazoParaEnfileirar)
        {
            noLock = await ContarAsync("""
                SELECT COUNT(*)
                FROM sys.dm_exec_requests AS r
                JOIN sys.dm_exec_sessions AS s ON s.session_id = r.session_id
                WHERE s.login_name = N'joya_app' AND r.wait_type LIKE N'LCK[_]M[_]%';
                """);
            if (noLock >= esperadas)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), _ct);
        }

        return noLock;
    }

    private static void ConferirEstoqueInsuficiente(
        Resposta resposta, int produtoId, int quantidadeSolicitada, int estoqueDisponivel)
    {
        resposta.TipoDeConteudo.ShouldBe("application/problem+json");
        var problema = resposta.Corpo;
        problema.GetProperty("code").GetString().ShouldBe("ESTOQUE_INSUFICIENTE");
        var itens = problema.GetProperty("itens").EnumerateArray().ToList();
        itens.Count.ShouldBe(1);
        itens[0].GetProperty("produtoId").GetInt32().ShouldBe(produtoId);
        itens[0].GetProperty("quantidadeSolicitada").GetInt32().ShouldBe(quantidadeSolicitada);
        itens[0].GetProperty("estoqueDisponivel").GetInt32().ShouldBe(estoqueDisponivel);
    }

    private async Task<IReadOnlyList<int>> IdsDosPedidosAsync()
    {
        var lista = await Sql.EscalarAsync(
            banco.ConnectionStringSa,
            "SELECT STRING_AGG(CAST(Id AS varchar(12)), ',') WITHIN GROUP (ORDER BY Id) FROM dbo.Pedidos;",
            _ct);
        return lista is string texto
            ? [.. texto.Split(',').Select(id => int.Parse(id, CultureInfo.InvariantCulture))]
            : [];
    }

    /// <summary>RN-11 no banco: <c>ValorTotal</c> de cada pedido é a soma exata dos itens gravados.</summary>
    private Task<int> PedidosComTotalDivergenteAsync() => ContarAsync("""
        SELECT COUNT(*) FROM dbo.Pedidos AS p
        WHERE p.ValorTotal <> ISNULL((SELECT SUM(i.Quantidade * i.PrecoUnitario) FROM dbo.ItensPedido AS i WHERE i.PedidoId = p.Id), -1);
        """);

    private async Task<int> ContarAsync(string consulta, params SqlParameter[] parametros) =>
        Convert.ToInt32(await Sql.EscalarAsync(banco.ConnectionStringSa, consulta, _ct, parametros), CultureInfo.InvariantCulture);

    private static SqlParameter PrecoParametro() =>
        new("@preco", System.Data.SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = _preco };

    private sealed record Resposta(HttpStatusCode Status, string? TipoDeConteudo, JsonElement Corpo);
}
