---
name: new-endpoint
description: Cria ou altera um endpoint REST no backend .NET 8 seguindo o padrão do projeto (Request DTO → FluentValidation → Service com Result<T> → Controller → teste unitário → teste de integração). Use sempre que for pedido um novo endpoint, rota, action de controller ou caso de uso exposto por HTTP em backend/.
argument-hint: "<VERBO> <rota> — <o que o endpoint faz>   ex.: POST /pedidos — cria um pedido com itens"
---

# Novo endpoint no padrão do projeto

Pedido: **$ARGUMENTS**

Siga as etapas **na ordem**. Cada etapa produz um arquivo (ou um trecho) e deixa o build verde — o hook `post-edit` compila e roda os testes unitários a cada edição, então um erro aparece na etapa que o causou.

## Etapa 0 — Contrato antes de código

1. Localize o endpoint em `docs/spec.md` (seção 5). Anote: rota, parâmetros, schema de resposta, códigos HTTP, regras `RN-xx` e critérios `CA-xx` envolvidos.
2. **Se o endpoint não estiver na spec, pare.** Proponha ao desenvolvedor o trecho a acrescentar (contrato + regras + ao menos um critério de aceite) e só continue depois da aprovação. Código sem spec não entra.
3. Decida a feature (pasta em `Joya.Pedidos.Core/`): `Pedidos`, `Relatorios`, `Clientes`, `Produtos` — ou uma nova, com nome no plural.
4. Se houver consulta nova ao banco, confira pelo MCP `mssql` (`search_objects`) quais índices existem para as colunas filtradas e ordenadas. Índice faltando é alteração em `database/01_schema.sql`, feita antes do código.

## Etapa 1 — Request e Response (`Core/<Feature>/`)

`sealed record`, imutável, sem lógica, sem atributos de validação. Um arquivo por tipo.

```csharp
namespace Joya.Pedidos.Core.Pedidos;

/// <summary>Corpo de POST /pedidos (spec 5.2).</summary>
public sealed record CriarPedidoRequest(int ClienteId, IReadOnlyList<CriarPedidoItemRequest>? Itens);

public sealed record CriarPedidoItemRequest(int ProdutoId, int Quantidade);
```

- Escrita: `<Verbo><Recurso>Request`. Leitura com parâmetros de query string: `<Verbo><Recurso>Query`. Resposta: `<Recurso>Response`.
- Coleções como `IReadOnlyList<T>`. Coleção que pode vir ausente no JSON é anulável (`?`) e a ausência é tratada no validator — não confie em valor padrão.
- Dinheiro é `decimal`. Datas de filtro são `DateOnly`. Instantes são `DateTime` em UTC.
- **Nunca** exponha uma entidade como resposta nem aceite uma entidade como entrada.
- O Request não carrega nada que o servidor deva decidir (preço, data, total, id).

## Etapa 2 — Validator (`Core/<Feature>/<Request>Validator.cs`)

Só validação **estrutural**: o que pode ser decidido olhando apenas para a requisição. Nada de banco aqui.

```csharp
using FluentValidation;

namespace Joya.Pedidos.Core.Pedidos;

public sealed class CriarPedidoValidator : AbstractValidator<CriarPedidoRequest>
{
    public const int MaximoItens = 50;
    public const int QuantidadeMaxima = 10_000;

    public CriarPedidoValidator()
    {
        RuleFor(r => r.ClienteId).GreaterThan(0);

        RuleFor(r => r.Itens)                                    // RN-01
            .NotEmpty().WithMessage("O pedido deve ter ao menos um item.")
            .Must(itens => itens is null || itens.Count <= MaximoItens)
            .WithMessage($"O pedido pode ter no máximo {MaximoItens} itens.");

        RuleForEach(r => r.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.ProdutoId).GreaterThan(0);
            item.RuleFor(i => i.Quantidade)                      // RN-02
                .InclusiveBetween(1, QuantidadeMaxima)
                .WithMessage($"A quantidade deve estar entre 1 e {QuantidadeMaxima}.");
        });

        RuleFor(r => r.Itens)                                    // RN-03
            .Must(itens => itens is null || itens.Select(i => i.ProdutoId).Distinct().Count() == itens.Count)
            .WithMessage("Um produto não pode aparecer em mais de um item do pedido.");
    }
}
```

- Comente cada regra com o `RN-xx` correspondente.
- Mensagens em português, voltadas ao usuário final.
- Limites são constantes públicas no validator (os testes usam as constantes, não números mágicos).
- Não registre nada: `AddValidatorsFromAssemblyContaining<…>()` já descobre o validator, e o `ValidationFilter` global o executa antes da action. Falha vira `400` com `code: "VALIDACAO"` — **o controller não valida nada**.

## Etapa 3 — Erros de negócio (`Core/<Feature>/<Recurso>Errors.cs`)

Um membro por falha prevista na spec (catálogo da seção 5.1). O tipo do erro define o status HTTP em um único lugar (`ErrorMapping` na Api).

```csharp
namespace Joya.Pedidos.Core.Pedidos;

public static class PedidoErrors
{
    public static Error ClienteNaoEncontrado(int clienteId) =>
        Error.Unprocessable("CLIENTE_NAO_ENCONTRADO", $"O cliente {clienteId} não existe.");

    public static Error EstoqueInsuficiente(IReadOnlyList<ItemSemEstoque> itens) =>
        Error.Conflict("ESTOQUE_INSUFICIENTE", $"Não há estoque suficiente para {itens.Count} item(ns) do pedido.")
             .With("itens", itens);
}
```

| Fábrica | HTTP | Uso |
|---|---|---|
| `Error.NotFound` | 404 | O recurso **da URL** não existe |
| `Error.Unprocessable` | 422 | O corpo referencia algo inexistente ou em estado inválido |
| `Error.Conflict` | 409 | Conflito com o estado atual (pode dar certo depois) |

Se você sentir necessidade de um quarto tipo, pergunte antes de criar.

## Etapa 4 — Service (`Core/<Feature>/`)

Interface + implementação `sealed` com *primary constructor*. É aqui que moram as regras que dependem de dados.

```csharp
namespace Joya.Pedidos.Core.Pedidos;

public interface IPedidoService
{
    Task<Result<PedidoResponse>> CriarAsync(CriarPedidoRequest request, Guid? chaveIdempotencia, CancellationToken ct);
}

public sealed class PedidoService(
    IClienteRepository clientes,
    IProdutoRepository produtos,
    IEstoqueRepository estoque,
    IPedidoRepository pedidos,
    IUnitOfWork unitOfWork,
    TimeProvider relogio) : IPedidoService
{
    public async Task<Result<PedidoResponse>> CriarAsync(
        CriarPedidoRequest request, Guid? chaveIdempotencia, CancellationToken ct)
    {
        var cliente = await clientes.ObterPorIdAsync(request.ClienteId, ct);
        if (cliente is null)
            return PedidoErrors.ClienteNaoEncontrado(request.ClienteId);     // RN-05

        // … demais regras, na ordem definida na spec 4.2 …
    }
}
```

Regras do service:

- Retorna **sempre** `Result<T>` (ou `Result`). Falha prevista é `return <Recurso>Errors.X(...)` — a conversão implícita cuida do resto. **Não lance exceção para regra de negócio.**
- `CancellationToken ct` é o último parâmetro e é repassado a **toda** chamada assíncrona.
- Depende apenas de **portas** (interfaces declaradas no `Core`). Precisa de um dado novo? Acrescente o método à porta e implemente em `Infrastructure/` — o service nunca vê `DbContext`.
- Hora atual só por `relogio.GetUtcNow()`.
- Escrita que toca mais de uma tabela roda dentro de `unitOfWork.ExecutarEmTransacaoAsync(...)`; devolver falha de dentro do delegate faz rollback.
- Estoque: somente `IEstoqueRepository.TentarBaixarAsync`, em ordem crescente de `ProdutoId` (spec 4.3). Nunca leia o saldo para decidir em memória.
- Leitura: a porta de consulta devolve o DTO já projetado; o service valida/normaliza parâmetros (ex.: `Periodo.Criar(dataInicio, dataFim)`) e delega.
- Registre o service em `Core/DependencyInjection.cs` como `Scoped`.

## Etapa 5 — Controller (`Api/Controllers/<Recursos>Controller.cs`)

O controller é uma casca: recebe, chama **um** método do service, traduz o resultado. Sem `if` de regra de negócio, sem `try/catch`, sem acesso a repositório.

```csharp
namespace Joya.Pedidos.Api.Controllers;

[ApiController]
[Route("pedidos")]
[Produces("application/json")]
public sealed class PedidosController(IPedidoService service) : ControllerBase
{
    /// <summary>Cria um pedido com itens, debitando o estoque de forma atômica.</summary>
    [HttpPost]
    [ProducesResponseType<PedidoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CriarAsync(
        [FromBody] CriarPedidoRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid? chaveIdempotencia,
        CancellationToken ct)
    {
        var resultado = await service.CriarAsync(request, chaveIdempotencia, ct);

        return resultado.Match(
            pedido => CreatedAtAction(nameof(ObterPorIdAsync), new { id = pedido.Id }, pedido),
            erro => erro.ToProblem(HttpContext));
    }
}
```

- Rota exatamente como na spec, em minúsculas, **sem** prefixo `/api` (quem acrescenta é o Nginx).
- `CancellationToken ct` em toda action.
- Um `[ProducesResponseType]` por status documentado na spec — é o que aparece no Swagger.
- Códigos de sucesso: `POST` que cria → `201` com `CreatedAtAction` (gera o `Location`); `GET` → `200`; coleção vazia é `200` com `[]`, nunca `404`.
- Query string com vários parâmetros: `[FromQuery] ListarPedidosQuery query`.
- Falha → sempre `erro.ToProblem(HttpContext)`. Não monte `ProblemDetails` à mão.

## Etapa 6 — Testes unitários (`tests/Joya.Pedidos.UnitTests/<Feature>/`)

Obrigatório, no mesmo commit do service. Dois arquivos:

**`<Request>ValidatorTests.cs`** — um teste por regra estrutural, incluindo os limites (0, 1, máximo, máximo + 1).

**`<Recurso>ServiceTests.cs`** — portas substituídas com NSubstitute, asserções com Shouldly:

```csharp
public sealed class PedidoServiceTests
{
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    // … demais portas …
    private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 11, 13, 14, 0, 0, TimeSpan.Zero));

    [Fact] // CA-05
    public async Task CriarAsync_ClienteInexistente_RetornaClienteNaoEncontrado()
    {
        _clientes.ObterPorIdAsync(9999, Arg.Any<CancellationToken>()).Returns((Cliente?)null);
        var request = new CriarPedidoRequest(9999, [new CriarPedidoItemRequest(12, 1)]);

        var resultado = await CriarSut().CriarAsync(request, chaveIdempotencia: null, CancellationToken.None);

        resultado.IsFailure.ShouldBeTrue();
        resultado.Error.Code.ShouldBe("CLIENTE_NAO_ENCONTRADO");
        await _estoque.DidNotReceiveWithAnyArgs().TentarBaixarAsync(default, default, default);
    }
}
```

Cobertura mínima por endpoint:

- [ ] Caminho feliz, verificando o **conteúdo** do resultado (não só `IsSuccess`).
- [ ] Um teste para **cada** erro de `<Recurso>Errors` que o método pode devolver.
- [ ] Efeitos colaterais que **não** podem acontecer na falha (`DidNotReceive`): nenhuma baixa, nenhum `INSERT`.
- [ ] A **ordem** das regras quando a spec define uma.
- [ ] Nome `Metodo_Cenario_ResultadoEsperado` e comentário com o `CA-xx`.

Não teste o controller isoladamente: ele não tem lógica; quem o cobre é o teste de integração.

## Etapa 7 — Teste de integração (`tests/Joya.Pedidos.IntegrationTests/<Feature>/`)

Exercita HTTP → SQL Server real (Testcontainers), com o schema de `database/01_schema.sql`.

- [ ] Um teste do caminho feliz conferindo **status, cabeçalhos e corpo**, e depois o **estado no banco**.
- [ ] Um teste por status de erro do contrato, conferindo o `code` do Problem Details.
- [ ] Para listagens: ordenação, limites de página e fronteiras de período.
- [ ] Dados criados pelos *builders* do próprio teste; nunca dependa da carga de exemplo.

Roda com `dotnet test backend/tests/Joya.Pedidos.IntegrationTests` (não é coberto pelo hook).

## Etapa 8 — Conferência final

Antes de dizer que terminou:

- [ ] `dotnet build backend/Joya.Pedidos.sln` — zero warnings.
- [ ] `dotnet test backend/Joya.Pedidos.sln` — tudo verde (unitários **e** integração).
- [ ] `dotnet format backend/Joya.Pedidos.sln --verify-no-changes`.
- [ ] O contrato real (Swagger em `/swagger`) bate com a spec 5: rota, parâmetros, status, nomes de propriedades.
- [ ] Pelo MCP `mssql`: os dados gravados estão corretos e, em consulta nova, o plano usa *seek* no índice esperado.
- [ ] `Core` continua sem referência a EF Core, ASP.NET ou SqlClient.
- [ ] Mensagem de commit sugerida ao desenvolvedor: `feat(api): expõe <VERBO> <rota>` (não faça o commit sem ser pedido).

## O que esta skill proíbe

| Não faça | Faça |
|---|---|
| Validar no controller ou com `[Required]`/`[Range]` | Validator do FluentValidation |
| `throw new XxxException` para regra de negócio | `return <Recurso>Errors.Xxx(...)` |
| Injetar `DbContext` em service ou controller | Porta no `Core`, implementação em `Infrastructure` |
| Devolver entidade do EF | DTO `…Response` projetado na consulta |
| `Include(...)` + mapeamento em memória para listagem | `Select` direto para o DTO com `AsNoTracking()` |
| `DateTime.UtcNow` | `TimeProvider` |
| Método assíncrono sem `CancellationToken` | `ct` até a última chamada |
| Entregar o endpoint sem teste | Etapas 6 e 7 |
