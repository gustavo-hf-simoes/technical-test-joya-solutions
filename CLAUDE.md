# Mini Gestão de Pedidos — Joya Solutions

Cadastro e consulta de pedidos de clientes. .NET 8 + Angular 19 + SQL Server 2022, tudo em Docker.

**Antes de implementar qualquer coisa, leia [docs/spec.md](docs/spec.md)** (regras `RN-xx`, contratos, modelo de dados, critérios `CA-xx`). A spec é a fonte de verdade técnica. Se o código precisar divergir dela, pare e proponha a alteração da spec primeiro (`PROP-xx`). O critério final de aceite é o enunciado literal da vaga, em [docs/requisitos/enunciado.md](docs/requisitos/enunciado.md).

## Esteira SDD

O trabalho segue a esteira por etapas descrita em [docs/esteira/README.md](docs/esteira/README.md) (decisão `HL-01` no plano). O estado corrente está em [docs/esteira/status.md](docs/esteira/status.md).

- A **sessão principal é o orquestrador**: use a skill **`/esteira`**. Ela delega cada etapa ao agente dono em `.claude/agents/`: `product-owner`, `arquiteto`, `backend`, `frontend`, `ciberseguranca` e `qa`.
- O orquestrador **roda os comandos do gate por conta própria** e pede as decisões humanas.
- Cada agente só escreve nos próprios caminhos. Os hooks `guard-paths.sh` e `guard-bash.sh` garantem isso.
- **Agentes nunca fazem commit.** O orquestrador propõe um commit por tarefa do backlog e só o faz com autorização.
- Achados (`ARQ`, `SEC`, `QA`) voltam ao dono. O mesmo revisor que abriu o achado confere o relatório **e** o código. Depois de 2 rodadas sem solução, o achado vai para o desenvolvedor.
- **Modelo por complexidade:** cada agente tem modelo e esforço no frontmatter, e cada tarefa do backlog tem `C1`/`C2`/`C3` (Haiku/Sonnet/Opus). Os gates são conferidos pelo agente `verificador`. Ver [ADR-0001](docs/adr/0001-roteamento-de-modelos.md).
- **Auditoria:** cada passo entra no diário [docs/ia/diario.md](docs/ia/diario.md), com o pedido, a saída, os `HL`/`CG` e o modelo.
- Skills vendorizadas (ver [.claude/skills/VENDORED.md](.claude/skills/VENDORED.md)) são material de apoio. Em conflito, **este arquivo e a spec prevalecem**.

## Arquitetura

Clean Architecture enxuta. As dependências apontam sempre para o `Core`.

```text
backend/src/
  Joya.Pedidos.Core/            Domínio + casos de uso + portas. Sem EF, sem ASP.NET, sem SqlClient.
    Common/                     Result<T>, Error, PaginaResponse<T>, Periodo
    Pedidos/ Relatorios/ Clientes/ Produtos/     Uma pasta por feature: entidade, Request,
                                                 Response, Validator, Service, portas, erros
  Joya.Pedidos.Infrastructure/  EF Core 8 (DbContext, configurações, repositórios, UnitOfWork)
  Joya.Pedidos.Api/             Controllers, filtros, Problem Details, composição (Program.cs)
backend/tests/
  Joya.Pedidos.UnitTests/         Regras e validações, sem I/O (xUnit + NSubstitute + Shouldly)
  Joya.Pedidos.IntegrationTests/  Endpoints contra SQL Server real (Testcontainers + Respawn)
database/                       Scripts SQL — ÚNICA fonte de verdade do schema (sem migrations)
frontend/                       Angular 19 standalone
```

Fluxo de uma requisição: `Controller` → `ValidationFilter` (FluentValidation) → `Service` (regras, devolve `Result<T>`) → portas → `Infrastructure`. O controller só traduz `Result<T>` em HTTP.

Para criar ou alterar um endpoint, use a skill **`/new-endpoint`**.

## Regras de ouro

1. **Zero warnings.** `TreatWarningsAsErrors` está ligado. Nunca use `#pragma warning disable`, `<NoWarn>` ou `!` (null-forgiving) para silenciar — corrija a causa.
2. **Imutabilidade.** DTOs são `sealed record` com propriedades `init`/posicionais. Entidades têm setters privados e nascem por construtor ou fábrica que protege os invariantes. Coleções expostas como `IReadOnlyList<T>`.
3. **Assíncrono de ponta a ponta com `CancellationToken`.** Todo método que faz I/O é `async`, termina em `Async` e recebe `CancellationToken ct` como último parâmetro, repassado até o EF. Nunca `.Result`, `.Wait()` ou `async void`.
4. **Estoque só é debitado pela instrução atômica** de `IEstoqueRepository.TentarBaixarAsync` (spec 4.3). Nunca leia o estoque para decidir em memória (*check-then-act*).
5. **Preço do item vem do banco no momento da baixa.** Nunca do corpo da requisição (RN-09).
6. **Falha de negócio é `Result.Failure(Erro)`, não exceção.** Exceção é para o inesperado e vira `500` no `IExceptionHandler`.
7. **Dinheiro é `decimal`** (`DECIMAL(18,2)` no banco). Nunca `double`/`float`.
8. **Datas em UTC.** Use o `TimeProvider` injetado, nunca `DateTime.Now`/`UtcNow` direto. Filtro de período sempre como intervalo semiaberto sobre a coluna (`>= inicio AND < fimExclusivo`) — nunca função sobre a coluna.
9. **Schema muda em `database/*.sql`**, depois no mapeamento EF. Não crie migrations.
10. **Consultas de leitura** usam `AsNoTracking()` e projetam direto para o DTO. Sem `Include` para montar resposta, sem N+1.
11. **Teste junto com o código.** Regra nova → teste unitário no mesmo commit, nomeado pelo critério (`CA-xx`) quando houver.
12. **Sem dependências novas** sem justificar no plano. Não use MediatR, AutoMapper, FluentAssertions nem Moq (ver decisões D3 e D11 em `docs/plan.md`).

## Nomenclatura

O domínio é em **português**; sufixos técnicos em **inglês**.

| Elemento | Padrão | Exemplo |
|---|---|---|
| Entidade | Substantivo singular | `Pedido`, `ItemPedido` |
| Request / Query | `<Verbo><Recurso>Request`, `<Verbo><Recurso>Query` | `CriarPedidoRequest`, `ListarPedidosQuery` |
| Response | `<Recurso>Response` | `PedidoResponse`, `PedidoResumoResponse` |
| Validator | `<Request>Validator` | `CriarPedidoValidator` |
| Service | `I<Recurso>Service` / `<Recurso>Service` | `PedidoService.CriarAsync` |
| Porta | `I<Recurso>Repository` | `IEstoqueRepository` |
| Erros | Classe estática `<Recurso>Errors`, código em `MAIUSCULAS_COM_UNDERSCORE` | `PedidoErrors.EstoqueInsuficiente` → `ESTOQUE_INSUFICIENTE` |
| Controller | Plural | `PedidosController` |
| Teste | `Metodo_Cenario_ResultadoEsperado` | `CriarAsync_ClienteInexistente_RetornaClienteNaoEncontrado` |
| Campo privado | `_camelCase` | `_itens` |
| SQL | Tabelas no plural, `PascalCase`; `PK_`, `FK_<Filha>_<Pai>`, `IX_<Tabela>_<Colunas>`, `UQ_`/`UX_`, `CK_`, `DF_` | `IX_Pedidos_ClienteId_DataPedido` |
| Angular | Arquivo `kebab-case`, um componente por arquivo | `pedido-lista.component.ts` |

**C# 12:** *file-scoped namespaces*, *primary constructors* para injeção de dependência, *collection expressions* (`[]`), `sealed` por padrão, `required` em vez de construtores longos em DTOs.

## Frontend

- Componentes **standalone** com `ChangeDetectionStrategy.OnPush`. Sem `NgModule`.
- Estado com **Signals** (`signal`, `computed`); `input()`/`output()`; `inject()` em vez de construtor.
- Controle de fluxo novo (`@if`, `@for` com `track`). Sem `*ngIf`/`*ngFor`.
- **Reactive Forms tipados** (`NonNullableFormBuilder`). Sem `any`.
- **Tailwind CSS** nos templates; sem CSS por componente, salvo necessidade real.
- HTTP só nos serviços `*Api` em `core/api/`, com caminhos relativos `/api/...`. Componentes não conhecem URLs.
- Moeda e datas formatadas em `pt-BR`.

## Comandos

Execute a partir da raiz do repositório.

```bash
# Build (falha com qualquer warning)
dotnet build backend/Joya.Pedidos.sln

# Testes unitários (rápidos, sem Docker)
dotnet test backend/tests/Joya.Pedidos.UnitTests

# Testes de integração (exigem Docker em execução — Testcontainers sobe o SQL Server)
dotnet test backend/tests/Joya.Pedidos.IntegrationTests

# Um teste específico
dotnet test backend/Joya.Pedidos.sln --filter "FullyQualifiedName~CriarAsync_ClienteInexistente"

# Lint / formatação do backend
dotnet format backend/Joya.Pedidos.sln --verify-no-changes

# Frontend
npm --prefix frontend ci
npm --prefix frontend run lint
npm --prefix frontend run build
npm --prefix frontend test -- --watch=false --browsers=ChromeHeadless

# Aplicação completa (banco com schema e carga, API e frontend)
docker compose up                # http://localhost:4200 · API em http://localhost:8080/swagger
docker compose up --build        # só para reconstruir as imagens depois de mudar o código (HL-15)

# Somente o banco (para rodar a API pelo dotnet e para o MCP)
docker compose up -d db db-init

# Recriar o banco do zero (APAGA o volume de dados)
docker compose down -v
```

## Automação do Claude neste repositório

- **Hook** ([.claude/hooks/post-edit.sh](.claude/hooks/post-edit.sh)): após cada edição em arquivo do backend, roda `dotnet build` e os testes unitários. Se falhar, a saída volta para você — corrija antes de seguir. Durante uma alteração que atravessa vários arquivos, uma falha intermediária é esperada; o que importa é terminar verde.
- **Hooks de limite dos agentes** ([guard-paths.sh](.claude/hooks/guard-paths.sh), [guard-bash.sh](.claude/hooks/guard-bash.sh)): são `PreToolUse` declarados no frontmatter de cada agente. O primeiro bloqueia escrita fora dos caminhos do agente. O segundo bloqueia `git` que altera histórico ou árvore, leitura de `.env`, `curl | sh` e instalação global. Só valem depois que a pasta é marcada como confiável.
- **Skill** `/new-endpoint`: passo a passo e gabaritos para um endpoint no padrão do projeto.
- **Skill** `/esteira`: orquestração das etapas, dos gates, dos achados e dos commits.
- **Skills vendorizadas** (`dotnet-webapi`, `optimizing-ef-core-queries`, `testcontainers-dotnet`, `angular-developer`, `security-review`, `gha-security-review` e as de qualidade de testes): procedência e auditoria em [VENDORED.md](.claude/skills/VENDORED.md).
- **MCP `mssql`** ([.mcp.json](.mcp.json)): acesso **somente leitura** ao SQL Server local. Use para conferir o schema real contra a spec, verificar o efeito de um endpoint nos dados e analisar planos de execução. Requer `docker compose up -d db db-init`. Não tente escrever pelo MCP: alterações de dados e schema vão em `database/*.sql`.

## Commits

Conventional Commits, mensagem em português, um commit por tarefa do plano: `feat(core): valida estrutura do pedido (RN-01 a RN-04)`. Tipos: `feat`, `fix`, `test`, `docs`, `chore`, `perf`, `refactor`, `ci`. **Nunca** faça squash, `--amend` em commit já existente, rebase ou force-push: o histórico é avaliado. Só faça commit quando solicitado.
