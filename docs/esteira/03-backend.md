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
