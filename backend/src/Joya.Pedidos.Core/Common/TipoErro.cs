namespace Joya.Pedidos.Core.Common;

/// <summary>Categoria da falha. A Api traduz cada tipo em um status HTTP, em um único lugar.</summary>
public enum TipoErro
{
    /// <summary>400: a requisição é estruturalmente inválida.</summary>
    Validacao,

    /// <summary>404: o recurso da URL não existe.</summary>
    NaoEncontrado,

    /// <summary>422: o corpo referencia algo inexistente ou em estado inválido.</summary>
    RegraDeNegocio,

    /// <summary>409: conflito com o estado atual.</summary>
    Conflito,
}
