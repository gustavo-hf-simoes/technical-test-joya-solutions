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
/// Hoje há poucos tipos de produção. As regras são escritas por assembly, namespace ou sufixo de nome, para
/// valerem sem alteração quando entidades, DTOs e controllers aparecerem. Onde o conjunto de alvos pode estar
/// vazio, o teste diz isso (WithoutRequiringPositiveResults ou comentário) e não finge verificar o que não existe.
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
        .LoadAssemblies(_core, _infra, _api, typeof(RegrasDeArquiteturaTests).Assembly)
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

    // Nomes de entidade de domínio (spec 6): ainda não existem no Core; a regra vale quando nascerem.
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
        Types().That().ResideInNamespace(Regex(_nsCore))
            .Should().NotDependOnAny(Types().That().ResideInNamespace(Regex(_nsInfra)).Or().ResideInNamespace(Regex(_nsApi)))
            .WithoutRequiringPositiveResults() // o conjunto proibido (Infrastructure) ainda não tem tipos
            .Check(_arquitetura);

        _core.GetReferencedAssemblies().Select(a => a.Name)
            .ShouldNotContain(n => n == _nsInfra || n == _nsApi);
    }

    [Fact]
    public void Infrastructure_Dependencias_NaoReferenciaApi()
    {
        // A Infrastructure ainda não tem tipos: sem WithoutRequiringPositiveResults o ArchUnitNET reprovaria
        // a regra por falta de objetos. A checagem por assembly abaixo vale desde já.
        Types().That().ResideInNamespace(Regex(_nsInfra))
            .Should().NotDependOnAny(Types().That().ResideInNamespace(Regex(_nsApi)))
            .WithoutRequiringPositiveResults()
            .Check(_arquitetura);

        var referencias = _infra.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();
        referencias.ShouldNotContain(n => n == _nsApi);
        referencias.ShouldNotContain(n => n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Controllers_Dependencias_NaoAcessamDbContext()
    {
        // Ainda não há controllers: a regra vale quando eles aparecerem (sufixo Controller).
        Classes().That().HaveNameEndingWith("Controller")
            .Should().NotDependOnAny(Types().That().ResideInNamespace(Regex("Microsoft.EntityFrameworkCore"))
                .Or().ResideInNamespace(Regex(_nsInfra)))
            .WithoutRequiringPositiveResults()
            .Check(_arquitetura);
    }

    [Fact]
    public void Dtos_Declaracao_SaoSealedRecord()
    {
        var dtos = TiposDeclarados(_core)
            .Where(t => _sufixosDeDto.Any(s => NomeSemAridade(t).EndsWith(s, StringComparison.Ordinal)))
            .ToList();

        // Hoje: PaginaResponse<T>. Um record gerado pelo compilador tem o método <Clone>$.
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
        // As entidades ainda não existem no Core: a lista fica vazia e o teste passa sem verificar nada por ora.
        // Entra em vigor quando Pedido/ItemPedido/Cliente/Produto forem criados. `init` também conta como setter público.
        var entidades = TiposDeclarados(_core).Where(t => _entidades.Contains(t.Name)).ToList();

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
            .WithoutRequiringPositiveResults()
            .Check(_arquitetura);
    }

    [Fact]
    public void TestesDeRegra_Dependencias_NaoUsamInfrastructureNemApi()
    {
        // Os testes de regra (fora de .Arquitetura) não podem tocar Infrastructure nem Api: continuam sem I/O.
        Types().That().ResideInNamespace(Regex(_nsTestes))
            .And().DoNotResideInNamespace(Regex($"{_nsTestes}.Arquitetura"))
            .Should().NotDependOnAny(Types().That().ResideInNamespace(Regex(_nsInfra)).Or().ResideInNamespace(Regex(_nsApi)))
            .WithoutRequiringPositiveResults() // Infrastructure ainda sem tipos; UnitTests/Common continua avaliado
            .Check(_arquitetura);
    }

    // ResideInNamespace recebe expressão regular: casa o namespace e os filhos dele.
    private static string Regex(string ns) => $"^{System.Text.RegularExpressions.Regex.Escape(ns)}(\\..*)?$";

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
