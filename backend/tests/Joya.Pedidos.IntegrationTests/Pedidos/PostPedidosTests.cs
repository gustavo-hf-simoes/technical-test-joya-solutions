using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Joya.Pedidos.IntegrationTests.Api;
using Joya.Pedidos.IntegrationTests.Infra;
using Joya.Pedidos.IntegrationTests.Persistencia;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace Joya.Pedidos.IntegrationTests.Pedidos;

/// <summary>
/// <c>POST /pedidos</c> de ponta a ponta (spec 5.2): HTTP, validação, regras, baixa atômica e gravação no SQL Server
/// real, com a API conectada como <c>joya_app</c>. Cada teste monta os próprios dados (Respawn entre testes).
/// </summary>
[Collection(SqlServerCollection.Nome)]
public sealed class PostPedidosTests(SqlServerFixture banco) : IAsyncLifetime
{
    private static readonly CancellationToken _ct = CancellationToken.None;

    private readonly ApiFactory _factory = new(banco.ConnectionStringApp);

    public Task InitializeAsync() => banco.ResetAsync(_ct);

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("CA", "CA-01")]
    public async Task PostPedidos_PedidoValido_Retorna201ComLocationEDebitaEstoque()
    {
        var clienteId = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Mercado Bom Preço", ativo: true, _ct);
        var teclado = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Teclado", 149.90m, 10, ativo: true, _ct);
        var mouse = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Mouse Pad", 29.95m, 5, ativo: true, _ct);

        var resposta = await PostAsync(new
        {
            clienteId,
            itens = new[] { new { produtoId = mouse, quantidade = 2 }, new { produtoId = teclado, quantidade = 2 } },
        });

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        resposta.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(_ct);
        var id = corpo.GetProperty("id").GetInt32();
        resposta.Headers.Location?.OriginalString.ShouldBe($"/pedidos/{id}");
        corpo.GetProperty("clienteId").GetInt32().ShouldBe(clienteId);
        corpo.GetProperty("clienteNome").GetString().ShouldBe("Mercado Bom Preço");
        corpo.GetProperty("valorTotal").GetDecimal().ShouldBe(359.70m);
        corpo.GetProperty("dataPedido").GetString().ShouldEndWith("Z");
        var itens = corpo.GetProperty("itens").EnumerateArray().ToList();
        itens.Count.ShouldBe(2);
        var itemTeclado = itens.Single(i => i.GetProperty("produtoId").GetInt32() == teclado);
        itemTeclado.GetProperty("produtoNome").GetString().ShouldBe("Teclado");
        itemTeclado.GetProperty("quantidade").GetInt32().ShouldBe(2);
        itemTeclado.GetProperty("precoUnitario").GetDecimal().ShouldBe(149.90m);
        itemTeclado.GetProperty("subtotal").GetDecimal().ShouldBe(299.80m);

        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, teclado, _ct)).ShouldBe(8);
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, mouse, _ct)).ShouldBe(3);
        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, _ct)).ShouldBe(1);
        (await Arranjo.ContarItensAsync(banco.ConnectionStringSa, _ct)).ShouldBe(2);
        var total = await Sql.EscalarAsync(
            banco.ConnectionStringSa, "SELECT ValorTotal FROM dbo.Pedidos WHERE Id = @id;", _ct, new SqlParameter("@id", id));
        total.ShouldBe(359.70m);
    }

    [Fact]
    [Trait("CA", "CA-02")]
    public async Task PostPedidos_SemItens_Retorna400SemGravar()
    {
        var clienteId = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Cliente", ativo: true, _ct);

        var vazio = await PostAsync(new { clienteId, itens = Array.Empty<object>() });
        var ausente = await PostAsync(new { clienteId });

        foreach (var resposta in new[] { vazio, ausente })
        {
            var problema = await LerProblemaAsync(resposta, HttpStatusCode.BadRequest, "VALIDACAO");
            problema.GetProperty("errors").GetProperty("itens").EnumerateArray().ShouldNotBeEmpty();
        }

        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, _ct)).ShouldBe(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_001)]
    [Trait("CA", "CA-03")]
    public async Task PostPedidos_QuantidadeInvalida_Retorna400(int quantidade)
    {
        var clienteId = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Cliente", ativo: true, _ct);
        var produtoId = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Produto", 10m, 5, ativo: true, _ct);

        var resposta = await PostAsync(new { clienteId, itens = new[] { new { produtoId, quantidade } } });

        var problema = await LerProblemaAsync(resposta, HttpStatusCode.BadRequest, "VALIDACAO");
        problema.GetProperty("errors").GetProperty("itens[0].quantidade").EnumerateArray().ShouldNotBeEmpty();
        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, _ct)).ShouldBe(0);
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, produtoId, _ct)).ShouldBe(5);
    }

    [Fact]
    [Trait("CA", "CA-04")]
    public async Task PostPedidos_ProdutoRepetido_Retorna400()
    {
        var clienteId = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Cliente", ativo: true, _ct);
        var produtoId = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Produto", 10m, 5, ativo: true, _ct);

        var resposta = await PostAsync(new
        {
            clienteId,
            itens = new[] { new { produtoId, quantidade = 1 }, new { produtoId, quantidade = 2 } },
        });

        await LerProblemaAsync(resposta, HttpStatusCode.BadRequest, "VALIDACAO");
        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, _ct)).ShouldBe(0);
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, produtoId, _ct)).ShouldBe(5);
    }

    [Fact]
    [Trait("CA", "CA-05")]
    public async Task PostPedidos_ClienteInexistente_Retorna422()
    {
        var produtoId = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Produto", 10m, 5, ativo: true, _ct);

        var resposta = await PostAsync(new { clienteId = 9999, itens = new[] { new { produtoId, quantidade = 1 } } });

        var problema = await LerProblemaAsync(resposta, HttpStatusCode.UnprocessableEntity, "CLIENTE_NAO_ENCONTRADO");
        problema.GetProperty("title").GetString().ShouldBe("Cliente inexistente");
        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, _ct)).ShouldBe(0);
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, produtoId, _ct)).ShouldBe(5);
    }

    [Fact]
    [Trait("CA", "CA-06")]
    public async Task PostPedidos_ClienteInativo_Retorna422()
    {
        var clienteId = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Inativo", ativo: false, _ct);
        var produtoId = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Produto", 10m, 5, ativo: true, _ct);

        var resposta = await PostAsync(new { clienteId, itens = new[] { new { produtoId, quantidade = 1 } } });

        var problema = await LerProblemaAsync(resposta, HttpStatusCode.UnprocessableEntity, "CLIENTE_INATIVO");
        problema.GetProperty("title").GetString().ShouldBe("Cliente inativo");
        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, _ct)).ShouldBe(0);
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, produtoId, _ct)).ShouldBe(5);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    public async Task PostPedidos_ProdutosInvalidos_Retorna422ComIds()
    {
        var clienteId = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Cliente", ativo: true, _ct);
        var valido = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Válido", 10m, 5, ativo: true, _ct);
        var inativo = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Inativo", 10m, 5, ativo: false, _ct);
        const int inexistente = 99_999;

        var resposta = await PostAsync(new
        {
            clienteId,
            itens = new[]
            {
                new { produtoId = valido, quantidade = 1 },
                new { produtoId = inativo, quantidade = 1 },
                new { produtoId = inexistente, quantidade = 1 },
            },
        });

        var problema = await LerProblemaAsync(resposta, HttpStatusCode.UnprocessableEntity, "PRODUTO_NAO_ENCONTRADO");
        problema.GetProperty("title").GetString().ShouldBe("Produto inexistente ou inativo");
        problema.GetProperty("type").GetString().ShouldBe("/erros/produto-nao-encontrado");
        problema.GetProperty("produtoIds").EnumerateArray().Select(e => e.GetInt32()).ShouldBe([inativo, inexistente]);
        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, _ct)).ShouldBe(0);
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, valido, _ct)).ShouldBe(5);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    public async Task PostPedidos_EstoqueInsuficiente_Retorna409SemGravarNada()
    {
        var clienteId = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Cliente", ativo: true, _ct);
        var suficiente = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Suficiente", 10m, 10, ativo: true, _ct);
        var curto = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Curto", 20m, 1, ativo: true, _ct);

        var resposta = await PostAsync(new
        {
            clienteId,
            itens = new[] { new { produtoId = suficiente, quantidade = 3 }, new { produtoId = curto, quantidade = 2 } },
        });

        var problema = await LerProblemaAsync(resposta, HttpStatusCode.Conflict, "ESTOQUE_INSUFICIENTE");
        problema.GetProperty("title").GetString().ShouldBe("Estoque insuficiente");
        problema.GetProperty("type").GetString().ShouldBe("/erros/estoque-insuficiente");
        var itens = problema.GetProperty("itens").EnumerateArray().ToList();
        itens.Count.ShouldBe(1);
        itens[0].EnumerateObject().Select(p => p.Name)
            .ShouldBe(["produtoId", "produtoNome", "quantidadeSolicitada", "estoqueDisponivel"]);
        itens[0].GetProperty("produtoId").GetInt32().ShouldBe(curto);
        itens[0].GetProperty("produtoNome").GetString().ShouldBe("Curto");
        itens[0].GetProperty("quantidadeSolicitada").GetInt32().ShouldBe(2);
        itens[0].GetProperty("estoqueDisponivel").GetInt32().ShouldBe(1);
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, suficiente, _ct)).ShouldBe(10);
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, curto, _ct)).ShouldBe(1);
        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, _ct)).ShouldBe(0);
        (await Arranjo.ContarItensAsync(banco.ConnectionStringSa, _ct)).ShouldBe(0);
    }

    [Fact]
    [Trait("CA", "CA-09")]
    public async Task PostPedidos_ConsomeTodoEstoque_Retorna201EZeraEstoque()
    {
        var clienteId = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Cliente", ativo: true, _ct);
        var produtoId = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Produto", 10m, 4, ativo: true, _ct);

        var resposta = await PostAsync(new { clienteId, itens = new[] { new { produtoId, quantidade = 4 } } });

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await Arranjo.EstoqueAsync(banco.ConnectionStringSa, produtoId, _ct)).ShouldBe(0);
    }

    [Fact]
    [Trait("CA", "CA-12")]
    public async Task PostPedidos_PrecoNoCorpo_IgnoraEUsaPrecoDeTabela()
    {
        var clienteId = await Arranjo.InserirClienteAsync(banco.ConnectionStringSa, "Cliente", ativo: true, _ct);
        var produtoId = await Arranjo.InserirProdutoAsync(banco.ConnectionStringSa, "Produto", 100.00m, 5, ativo: true, _ct);

        var resposta = await PostAsync(new
        {
            clienteId,
            valorTotal = 0.01m,
            itens = new[] { new { produtoId, quantidade = 2, precoUnitario = 0.01m, subtotal = 0.02m } },
        });

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(_ct);
        corpo.GetProperty("valorTotal").GetDecimal().ShouldBe(200.00m);
        corpo.GetProperty("itens")[0].GetProperty("precoUnitario").GetDecimal().ShouldBe(100.00m);
        var gravado = await Sql.EscalarAsync(banco.ConnectionStringSa, "SELECT PrecoUnitario FROM dbo.ItensPedido;", _ct);
        gravado.ShouldBe(100.00m);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    public async Task PostPedidos_RegrasEmConflito_Retorna400Primeiro()
    {
        // Itens vazios (RN-01, 400) e cliente inexistente (RN-05, 422): a validação estrutural vence.
        var resposta = await PostAsync(new { clienteId = 9999, itens = Array.Empty<object>() });

        await LerProblemaAsync(resposta, HttpStatusCode.BadRequest, "VALIDACAO");
        (await Arranjo.ContarPedidosAsync(banco.ConnectionStringSa, _ct)).ShouldBe(0);
    }

    [Fact] // spec 5.1: corpo malformado é 400 VALIDACAO
    public async Task PostPedidos_JsonMalformado_Retorna400()
    {
        using var cliente = _factory.CreateClient();
        using var conteudo = new StringContent("{\"clienteId\": ", Encoding.UTF8, "application/json");

        var resposta = await cliente.PostAsync("/pedidos", conteudo, _ct);

        await LerProblemaAsync(resposta, HttpStatusCode.BadRequest, "VALIDACAO");
    }

    [Fact] // spec 5.1: tipo errado no corpo
    public async Task PostPedidos_TipoInvalidoNoCorpo_Retorna400()
    {
        var resposta = await PostAsync(new { clienteId = "abc", itens = Array.Empty<object>() });

        await LerProblemaAsync(resposta, HttpStatusCode.BadRequest, "VALIDACAO");
    }

    private async Task<HttpResponseMessage> PostAsync(object corpo)
    {
        using var cliente = _factory.CreateClient();
        return await cliente.PostAsJsonAsync("/pedidos", corpo, _ct);
    }

    private static async Task<JsonElement> LerProblemaAsync(
        HttpResponseMessage resposta, HttpStatusCode status, string codigo)
    {
        resposta.StatusCode.ShouldBe(status);
        resposta.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var problema = await resposta.Content.ReadFromJsonAsync<JsonElement>(_ct);
        problema.GetProperty("status").GetInt32().ShouldBe((int)status);
        problema.GetProperty("code").GetString().ShouldBe(codigo);
        problema.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        return problema;
    }
}
