namespace Joya.Pedidos.Core.Common;

/// <summary>Falha de negócio prevista. Código estável em MAIUSCULAS_COM_UNDERSCORE.</summary>
public sealed record Error
{
    private static readonly IReadOnlyDictionary<string, object?> _semExtensoes =
        new Dictionary<string, object?>();

    private Error(string code, string message, TipoErro tipo, IReadOnlyDictionary<string, object?> extensoes)
    {
        Code = code;
        Message = message;
        Tipo = tipo;
        Extensoes = extensoes;
    }

    public string Code { get; }

    public string Message { get; }

    public TipoErro Tipo { get; }

    /// <summary>Dados extras que vão para o Problem Details (por exemplo, os itens sem estoque).</summary>
    public IReadOnlyDictionary<string, object?> Extensoes { get; }

    public static Error Validation(string code, string message) => new(code, message, TipoErro.Validacao, _semExtensoes);

    public static Error NotFound(string code, string message) => new(code, message, TipoErro.NaoEncontrado, _semExtensoes);

    public static Error Unprocessable(string code, string message) => new(code, message, TipoErro.RegraDeNegocio, _semExtensoes);

    public static Error Conflict(string code, string message) => new(code, message, TipoErro.Conflito, _semExtensoes);

    /// <summary>Devolve uma cópia com a extensão acrescentada.</summary>
    public Error With(string chave, object? valor)
    {
        var copia = new Dictionary<string, object?>(Extensoes.Count + 1);
        foreach (var par in Extensoes)
        {
            copia[par.Key] = par.Value;
        }

        copia[chave] = valor;
        return new Error(Code, Message, Tipo, copia);
    }
}
