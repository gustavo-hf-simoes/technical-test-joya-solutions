namespace Joya.Pedidos.Infrastructure.Clientes;

/// <summary>
/// Linha de <c>dbo.Clientes</c> para as consultas (spec 6.2). Não é entidade de domínio: a API não cria nem altera
/// clientes, e as respostas são projetadas direto para os DTOs do Core. <c>CriadoEm</c> não é mapeado.
/// </summary>
public sealed class Cliente
{
    private Cliente(int id, string nome, string email, bool ativo)
    {
        Id = id;
        Nome = nome;
        Email = email;
        Ativo = ativo;
    }

    public int Id { get; private set; }

    public string Nome { get; private set; }

    public string Email { get; private set; }

    public bool Ativo { get; private set; }
}
