using Joya.Pedidos.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Joya.Pedidos.IntegrationTests.Persistencia;

/// <summary>
/// Monta a Infrastructure pelo mesmo <see cref="DependencyInjection.AddInfrastructure"/> da Api, sem HTTP, apontando
/// para o contêiner com o login de menor privilégio (<c>joya_app</c>). O <c>ValidateOnBuild</c> prova que todas as
/// portas registradas resolvem. Cada escopo equivale a uma requisição: um <c>DbContext</c> próprio.
/// </summary>
public sealed class InfraestruturaReal : IAsyncDisposable
{
    private readonly ServiceProvider _provedor;

    public InfraestruturaReal(string connectionString)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DependencyInjection.ConnectionStringPadrao}"] = connectionString,
            })
            .Build();

        var servicos = new ServiceCollection();
        servicos.AddSingleton<IConfiguration>(configuracao);
        servicos.AddLogging();
        servicos.AddInfrastructure();

        _provedor = servicos.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    public AsyncServiceScope CriarEscopo() => _provedor.CreateAsyncScope();

    public ValueTask DisposeAsync() => _provedor.DisposeAsync();
}
