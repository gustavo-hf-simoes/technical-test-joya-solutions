using System.Reflection;
using System.Runtime.CompilerServices;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Architecture = ArchUnitNET.Domain.Architecture;
using Type = System.Type;

namespace Joya.Pedidos.UnitTests.Arquitetura;

/// <summary>
/// Regras do CLAUDE.md e de docs/esteira/02-arquitetura.md §3 transformadas em testes (HL-08, ENU-L019).
/// As regras são escritas por assembly, namespace (ResideInNamespaceOrBelow) ou sufixo de nome. Nenhuma usa
/// WithoutRequiringPositiveResults: o ArchUnitNET reprova a regra se o seletor ficar vazio, então o teste
/// não passa sem verificar nada (ARQ-02). O assembly do EF Core entra no modelo para a regra dos controllers.
/// </summary>
public sealed class RegrasDeArquiteturaTests
{
    private const string _nsCore = "Joya.Pedidos.Core";
    private const string _nsInfra = "Joya.Pedidos.Infrastructure";
    private const string _nsApi = "Joya.Pedidos.Api";
    private const string _nsTestes = "Joya.Pedidos.UnitTests";

    private static readonly Assembly _core = typeof(Core.Common.TipoErro).Assembly;
    private static readonly Assembly _infra = Assembly.Load(_nsInfra);
    private static readonly Assembly _api = typeof(Program).Assembly;
    private static readonly Assembly[] _producao = [_core, _infra, _api];

    private static readonly Architecture _arquitetura = new ArchLoader()
        .LoadAssemblies(_core, _infra, _api, typeof(RegrasDeArquiteturaTests).Assembly, typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly)
        .Build();

    private static readonly string[] _referenciasProibidasNoCore =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Microsoft.Data.SqlClient",
        "System.Data.SqlClient",
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.Logging",
    ];

    private static readonly string[] _sufixosDeDto = ["Request", "Query", "Response"];

    // Nomes de entidade de domínio (spec 6): Pedido e ItemPedido no Core; Cliente e Produto na Infrastructure.
    private static readonly string[] _entidades = ["Pedido", "ItemPedido", "Cliente", "Produto"];

    [Fact]
    public void Core_Dependencias_NaoReferenciaEfAspNetNemSqlClient()
    {
        // Reflexão pura: o ArchUnitNET só enxerga tipos usados, não pacotes referenciados.
        var referencias = _core.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        var proibidas = referencias
            .Where(nome => _referenciasProibidasNoCore.Any(p => nome.StartsWith(p, StringComparison.Ordinal)))
            .ToList();

        proibidas.ShouldBeEmpty("o Core não pode referenciar EF Core, ASP.NET Core, SqlClient, DI nem Logging");
        referencias.ShouldNotBeEmpty("a lista vem de um assembly carregado: vazia indicaria reflexão quebrada");
    }

    [Fact]
    public void Core_Dependencias_NaoReferenciaInfrastructureNemApi()
    {
        Types().That().ResideInNamespaceOrBelow(_nsCore)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceOrBelow(_nsInfra)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceOrBelow(_nsApi)
            .Check(_arquitetura);

        _core.GetReferencedAssemblies().Select(a => a.Name)
            .ShouldNotContain(n => n == _nsInfra || n == _nsApi);
    }

    [Fact]
    public void Infrastructure_Dependencias_NaoReferenciaApi()
    {
        Types().That().ResideInNamespaceOrBelow(_nsInfra)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceOrBelow(_nsApi)
            .Check(_arquitetura);

        var referencias = _infra.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();
        referencias.ShouldNotContain(n => n == _nsApi);
        referencias.ShouldNotContain(n => n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Controllers_Dependencias_NaoAcessamDbContext()
    {
        Classes().That().HaveNameEndingWith("Controller").And().ResideInNamespaceOrBelow(_nsApi)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceOrBelow(_nsInfra)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceOrBelow("Microsoft.EntityFrameworkCore")
            .Check(_arquitetura);
    }

    [Fact]
    public void Dtos_Declaracao_SaoSealedRecord()
    {
        var dtos = TiposDeclarados(_core)
            .Where(t => _sufixosDeDto.Any(s => NomeSemAridade(t).EndsWith(s, StringComparison.Ordinal)))
            .ToList();

        // Um record gerado pelo compilador tem o método <Clone>$.
        dtos.ShouldNotBeEmpty("PaginaResponse<T> já é um DTO do Core");
        var violacoes = dtos
            .Where(t => !t.IsSealed || t.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is null)
            .Select(t => t.FullName)
            .ToList();

        violacoes.ShouldBeEmpty("DTOs (*Request, *Query, *Response) devem ser sealed record");
    }

    [Fact]
    public void Entidades_Propriedades_NaoTemSetterPublico()
    {
        // `init` também conta como setter público. Procura no Core e na Infrastructure, onde as entidades vivem.
        var entidades = TiposDeclarados(_core).Concat(TiposDeclarados(_infra))
            .Where(t => _entidades.Contains(t.Name))
            .ToList();

        entidades.Select(t => t.Name).ShouldBe(_entidades, ignoreOrder: true, "as quatro entidades da spec 6 devem ser encontradas");

        var violacoes = entidades
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod is { IsPublic: true })
                .Select(p => $"{t.FullName}.{p.Name}"))
            .ToList();

        violacoes.ShouldBeEmpty("entidades têm setters privados e nascem por construtor ou fábrica");
    }

    [Fact]
    public void MetodosAsync_Assinatura_RecebemCancellationTokenPorUltimo()
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        var violacoes = _producao
            .SelectMany(TiposDeclarados)
            .SelectMany(t => t.GetMethods(Flags))
            .Where(m => m.Name.EndsWith("Async", StringComparison.Ordinal) && DevolveTask(m) && !ImplementaFramework(m))
            .Where(m => m.GetParameters() is not [.., { ParameterType: var ultimo }] || ultimo != typeof(CancellationToken))
            .Select(m => $"{m.DeclaringType?.FullName}.{m.Name}")
            .ToList();

        violacoes.ShouldBeEmpty("todo método *Async recebe CancellationToken como último parâmetro");
    }

    [Fact]
    public void Classes_Declaracao_SaoSealed()
    {
        // Exceção: Program (partial, exigido pelo WebApplicationFactory). Tipos gerados pelo compilador ficam de fora.
        var naoSealed = _producao
            .SelectMany(TiposDeclarados)
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsSealed && t.Name != "Program")
            .Select(t => t.FullName)
            .ToList();

        naoSealed.ShouldBeEmpty("classes concretas são sealed por padrão");

        Classes().That().ResideInAssembly(_core).Or().ResideInAssembly(_infra).Or().ResideInAssembly(_api)
            .And().AreNotAbstract()
            .And().DoNotHaveName("Program")
            .And().DoNotHaveFullNameContaining("<")
            .Should().BeSealed()
            .Check(_arquitetura);
    }

    [Fact]
    public void TestesDeRegra_Dependencias_NaoUsamInfrastructureNemApi()
    {
        // Os testes de regra (fora de .Arquitetura) não podem tocar Infrastructure nem Api: continuam sem I/O.
        Types().That().ResideInNamespaceOrBelow(_nsTestes)
            .And().DoNotResideInNamespaceOrBelow($"{_nsTestes}.Arquitetura")
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceOrBelow(_nsInfra)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceOrBelow(_nsApi)
            .Check(_arquitetura);
    }

    private static IEnumerable<Type> TiposDeclarados(Assembly assembly) =>
        assembly.GetTypes().Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute), false) && !t.IsNested);

    private static string NomeSemAridade(Type tipo)
    {
        var nome = tipo.Name;
        var crase = nome.IndexOf('`', StringComparison.Ordinal);
        return crase < 0 ? nome : nome[..crase];
    }

    private static bool DevolveTask(MethodInfo metodo)
    {
        var retorno = metodo.ReturnType;
        return retorno == typeof(Task) || retorno == typeof(ValueTask)
            || (retorno.IsGenericType && (retorno.GetGenericTypeDefinition() == typeof(Task<>)
                || retorno.GetGenericTypeDefinition() == typeof(ValueTask<>)));
    }

    // Métodos que implementam contrato do framework (ex.: IExceptionHandler.TryHandleAsync) têm assinatura imposta.
    private static bool ImplementaFramework(MethodInfo metodo) =>
        metodo.DeclaringType is { } tipo
        && tipo.GetInterfaces()
            .Where(i => i.Namespace?.StartsWith("Joya.Pedidos", StringComparison.Ordinal) != true)
            .Any(i => i.GetMethods().Any(m => m.Name == metodo.Name));
}
