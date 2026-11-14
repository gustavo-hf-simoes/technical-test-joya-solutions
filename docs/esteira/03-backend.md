# Relatório da etapa 3 — Backend (`backend`)

## Rodada 1 — 2026-11-13 — US-02 (Esqueleto)

### Tarefas

| Tarefa | Status | Mensagem de commit prevista | Arquivos principais |
|---|---|---|---|
| T-02.1 | Pronta | `chore(backend): cria a solução .NET 8 em Clean Architecture` | `backend/Joya.Pedidos.sln`, `backend/Directory.Build.props`, `backend/global.json`, `backend/.editorconfig`, `backend/src/*/*.csproj`, `backend/src/Joya.Pedidos.Api/Program.cs`, `backend/tests/*/*.csproj` |
| T-02.2 | Pronta | `feat(core): adiciona Result, Error e PaginaResponse` | `backend/src/Joya.Pedidos.Core/Common/{Result,Error,TipoErro,PaginaResponse}.cs`, `backend/tests/Joya.Pedidos.UnitTests/Common/ResultTests.cs` |

Notas de design:
- `Core` não referencia nada; `Infrastructure` e `Api` referenciam o `Core`; a `Api` também a `Infrastructure`; UnitTests referencia o `Core`; IntegrationTests, a `Api`.
- `Error` expõe `Code`, `Message`, `Tipo` (`TipoErro`: Validacao, NaoEncontrado, RegraDeNegocio, Conflito, conforme a arquitetura) e `Extensoes`; fábricas `Validation`, `NotFound`, `Unprocessable`, `Conflict` e `With(chave, valor)`, como na skill `new-endpoint`.
- `Directory.Build.props` usa `AnalysisLevel=latest` (conjunto padrão). O modo `recommended` exigiria renomear `Error` (CA1716) e tirar `Failure` de `Result<T>` (CA1000), ambos nomes fixados pela spec e pela arquitetura. Nenhum warning foi silenciado.
- `.editorconfig` desliga só o CA1707 (sublinhado em nome de membro) para `tests/**/*.cs`, porque a convenção `Metodo_Cenario_Resultado` do CLAUDE.md o exige.
- `Program.cs` mínimo (só `AddControllers`/`MapControllers`, `partial class Program` para `WebApplicationFactory`). Pacotes: só xunit, runner, Test.Sdk, NSubstitute e Shouldly. `coverlet` não incluído. Os projetos de teste só aparecem na solução, sem teste de integração ainda.
- `Periodo` fica para a história de consultas (a spec não o exige aqui).
- Bug achado pelo próprio teste e corrigido: `Result<int>.Value` não lançava na falha porque `T?` de tipo valor não é nulo; agora `Value` checa `IsSuccess`.

### Testes criados ou alterados

| Teste | Tipo | Cobre |
|---|---|---|
| `Failure_ErroInformado_CarregaCodigoSemExcecao` | unit | US-02 "Tipos comuns" |
| `PaginaResponse_TotalItems_CalculaTotalPages` (5 casos) | unit | spec 5.3 (totalPages) |
| `ConversaoImplicita_Valor_RetornaSucesso`, `ConversaoImplicita_Erro_RetornaFalha`, `Match_Falha_ExecutaRamoDoErro`, `With_ExtensaoInformada_PreservaOriginalEAcrescentaCopia` | unit | comportamento de `Result`/`Error` |

### Verificações executadas

| Comando | Resultado | Resumo da saída |
|---|---|---|
| `dotnet build backend/Joya.Pedidos.sln` | Passou | `0 Aviso(s) 0 Erro(s)` |
| `dotnet test backend/tests/Joya.Pedidos.UnitTests` | Passou | `Aprovado: 10, Falha: 0` |
| `dotnet format backend/Joya.Pedidos.sln --verify-no-changes` | Passou | código de saída 0 |
| `dotnet test ...IntegrationTests` | Não executado | ainda sem testes (US-02 não os exige) |

### Evidência no banco (MCP `mssql`)

Não se aplica a esta história.

### Desvios em relação à spec

| PROP | Texto atual da spec | Proposta | Motivo |
|---|---|---|---|
| — | — | — | — |

### Bloqueios e perguntas

- —

## Resposta a achados

| Achado | Rodada | O que foi feito | Onde | Teste que prova | Status declarado |
|---|---|---|---|---|---|
| BE-07 (gate, `!` null-forgiving) | 2 | Removido `default!`; `var resultado = Should.NotThrow(() => Result<int>.Failure(erro));`. Varredura em `backend/**/*.cs` não achou outro `!` null-forgiving. | `backend/tests/Joya.Pedidos.UnitTests/Common/ResultTests.cs:13` | `Failure_ErroInformado_CarregaCodigoSemExcecao` | Corrigido |

## Rodada 1 — 2026-11-13 — US-03 (T-03.1 e T-03.2)

### Tarefas

| Tarefa | Status | Mensagem de commit prevista | Arquivos |
|---|---|---|---|
| T-03.1 | Pronta | `ci: compila e roda os testes unitários a cada push e PR` | `.github/workflows/ci.yml` |
| T-03.2 | Pronta | `test(arq): transforma regras do CLAUDE.md em testes de arquitetura (HL-08)` | `backend/tests/Joya.Pedidos.UnitTests/Arquitetura/RegrasDeArquiteturaTests.cs`, `backend/tests/Joya.Pedidos.UnitTests/Joya.Pedidos.UnitTests.csproj` |

T-03.1: job `backend` (ubuntu-24.04, `permissions: contents: read` herdado do arquivo), `actions/setup-dotnet` fixada por SHA (`0f4a354d…`, v6.1.0, commit resolvido via `gh api`) com `global-json-file: backend/global.json`; `dotnet build` (Release), `dotnet format --verify-no-changes`, `dotnet test` dos unitários. Sem `${{ }}` em `run`. O job `config` não foi alterado. O workflow só será provado verde no PR (verificação manual do backlog).

T-03.2: pacote `TngTech.ArchUnitNET.xUnit` 0.13.5 (aprovado, HL-08); UnitTests passa a referenciar `Infrastructure` e `Api` só para carregar assemblies.

### Testes criados

9 testes em `RegrasDeArquiteturaTests`, com os nomes do backlog. Mecanismos: reflexão (`GetReferencedAssemblies`, `GetTypes`) para pacotes, DTOs, entidades, `CancellationToken` e sealed; ArchUnitNET para dependências entre tipos, controllers, sealed e testes de regra.

Tratamento de regras sem alvo hoje (explícito no código):
- ArchUnitNET reprova regra sem objetos ("requires positive evaluation"). Usei `WithoutRequiringPositiveResults()` em: Core→Infra/Api (o conjunto proibido, Infrastructure, não tem tipos), Infrastructure→Api, Controllers (não existem), Classes sealed e TestesDeRegra. Nos dois primeiros e no de sealed, uma checagem por reflexão complementar roda sem depender disso.
- `Entidades_Propriedades_NaoTemSetterPublico`: `Pedido`/`ItemPedido`/`Cliente`/`Produto` ainda não existem, então hoje passa sem verificar nada. Está declarado em comentário; entra em vigor sozinho quando as entidades nascerem (casa por nome). `init` conta como setter público.
- `MetodosAsync_...`: ainda não há método `*Async`; vale quando aparecer. Exclui implementações de interfaces de fora de `Joya.Pedidos` (ex.: `TryHandleAsync`).
- `Dtos_Declaracao_SaoSealedRecord` tem alvo real (`PaginaResponse<T>`) e exige não vazio; `Error` não tem sufixo de DTO.
- `Program` é a exceção declarada em `Classes_Declaracao_SaoSealed`.
- `ResideInNamespace` desta versão recebe regex; helper `Regex(ns)` casa o namespace e filhos.

### Verificações

| Comando | Resultado |
|---|---|
| `dotnet build backend/Joya.Pedidos.sln` | Passou, 0 avisos, 0 erros |
| `dotnet test backend/tests/Joya.Pedidos.UnitTests` | Passou, 19 aprovados (10 anteriores + 9) |
| `dotnet format ... --verify-no-changes` | Passou |
| YAML do `ci.yml` | `yaml.safe_load` ok, jobs `config` e `backend` |
| Integração | Não executado (fora do escopo) |

Violação provada localmente e revertida: acrescentei `Microsoft.EntityFrameworkCore` 8.0.10 ao `Core.csproj` e uma classe usando `DbContext`. `Core_Dependencias_NaoReferenciaEfAspNetNemSqlClient` falhou com `proibidas ["Microsoft.EntityFrameworkCore"]` (8 dos 9 passaram). Depois restaurei o `.csproj`, apaguei a classe, e os 19 testes voltaram a passar.

### Evidência no banco (MCP `mssql`)
Não se aplica.

### Desvios em relação à spec
Nenhum (sem PROP).

### Bloqueios e perguntas
- A regra de dependência Core→Infrastructure/Api pelo ArchUnitNET fica vacuamente verdadeira até a Infrastructure ter tipos; coberta também por `GetReferencedAssemblies`.

## Resposta a achados (US-03)
Nenhum achado recebido nesta rodada.
