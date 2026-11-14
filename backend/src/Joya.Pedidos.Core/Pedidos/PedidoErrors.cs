using Joya.Pedidos.Core.Common;

namespace Joya.Pedidos.Core.Pedidos;

/// <summary>Falhas de negócio do pedido (catálogo da spec 5.1).</summary>
public static class PedidoErrors
{
    public static Error ClienteNaoEncontrado(int clienteId) =>
        Error.Unprocessable("CLIENTE_NAO_ENCONTRADO", $"O cliente {clienteId} não existe.");

    public static Error ClienteInativo(int clienteId) =>
        Error.Unprocessable("CLIENTE_INATIVO", $"O cliente {clienteId} está inativo.");

    public static Error ProdutoNaoEncontrado(IReadOnlyList<int> produtoIds)
    {
        var mensagem = produtoIds.Count == 1
            ? $"O produto {produtoIds[0]} não existe ou está inativo."
            : $"Os produtos {Listar(produtoIds)} não existem ou estão inativos.";

        return Error.Unprocessable("PRODUTO_NAO_ENCONTRADO", mensagem).With("produtoIds", produtoIds);
    }

    public static Error EstoqueInsuficiente(IReadOnlyList<ItemSemEstoque> itens) =>
        Error.Conflict(
            "ESTOQUE_INSUFICIENTE",
            itens.Count == 1
                ? "Não há estoque suficiente para 1 item do pedido."
                : $"Não há estoque suficiente para {itens.Count} itens do pedido.")
            .With("itens", itens);

    /// <summary>Violação de <c>UX_Pedidos_ChaveIdempotencia</c>, devolvida pelo repositório (ADR-0004). Tratada na US-12.</summary>
    public static Error ChaveIdempotenciaDuplicada() =>
        Error.Conflict("CHAVE_IDEMPOTENCIA_DUPLICADA", "Já existe um pedido com esta chave de idempotência.");

    private static string Listar(IReadOnlyList<int> ids) =>
        string.Join(", ", ids.Take(ids.Count - 1)) + " e " + ids[^1];
}
