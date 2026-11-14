namespace Joya.Pedidos.Core.Produtos;

public interface IProdutoRepository
{
    /// <summary>Devolve os produtos <b>ativos</b> entre os ids informados. Ids inexistentes ou inativos ficam de fora (RN-07).</summary>
    Task<IReadOnlyList<ProdutoResumo>> ObterAtivosAsync(IReadOnlyCollection<int> ids, CancellationToken ct);

    /// <summary>Estoque atual por id. Só informativo (mensagem do 409): quem decide é a baixa atômica.</summary>
    Task<IReadOnlyDictionary<int, int>> ObterEstoquesAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
}
