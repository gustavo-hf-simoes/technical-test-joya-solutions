namespace Joya.Pedidos.Core.Produtos;

/// <summary>Produto ativo encontrado: só o necessário para validar (RN-07) e montar a resposta.</summary>
public sealed record ProdutoResumo(int Id, string Nome);
