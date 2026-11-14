namespace Joya.Pedidos.UnitTests.Apoio;

/// <summary>Relógio fixo para os testes (evita o pacote Microsoft.Extensions.TimeProvider.Testing).</summary>
internal sealed class RelogioFalso(DateTimeOffset agora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => agora;
}
