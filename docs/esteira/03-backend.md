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

## Rodada 1 — 2026-11-13 — US-04, delegação 1 de 2 (T-04.1, T-04.2, T-04.3)

### Tarefas

| Tarefa | Status | Mensagem de commit prevista | Arquivos do commit |
|---|---|---|---|
| T-04.1 | Implementada; **integração não executada** (bloqueio de disco, abaixo) | `feat(db): cria o schema de clientes, produtos e pedidos` | `database/01-schema.sql`, `database/init.sh`, `backend/tests/Joya.Pedidos.IntegrationTests/Joya.Pedidos.IntegrationTests.csproj`, `backend/tests/Joya.Pedidos.IntegrationTests/Infra/{SqlServerFixture,SqlServerCollection,Sql}.cs`, `backend/tests/Joya.Pedidos.IntegrationTests/Banco/SchemaTests.cs` |
| T-04.2 | Implementada; integração não executada | `feat(db): cria os índices das consultas de listagem e ranking` | `database/02-indices.sql`, `backend/tests/Joya.Pedidos.IntegrationTests/Banco/IndicesTests.cs` |
| T-04.3 | Implementada; integração não executada | `feat(db): cria logins de menor privilégio para a API e o MCP` | `database/03-logins.sql`, `backend/tests/Joya.Pedidos.IntegrationTests/Banco/LoginsTests.cs` |

Notas de design:
- **`init.sh`** (bash, `set -euo pipefail`, LF). Sem argumento, aplica todos os `database/NN-*.sql` em ordem (`LC_ALL=C`). Com argumento (`01-schema` ou `01-schema.sql`), aplica só as etapas pedidas, sempre ordenadas, e recusa caminho e etapa inexistente. Usa `sqlcmd -C -I -b -l 30` (DB-04). A senha do `sa` vai por `SQLCMDPASSWORD`, fora da linha de comando. `MSSQL_HOST` tem padrão `localhost`; o `db-init` da T-04.5 passará `db`. As senhas `APP_SQL_PASSWORD` e `MCP_SQL_PASSWORD` vão por `-v` (ADR-0003), só para o script que as referencia, e são obrigatórias nele. Senha com aspa simples é recusada, porque quebraria o literal `N'...'`. Sem `sleep`: a espera é do compose (`service_healthy`) e do Testcontainers.
- **Idempotência.** O `01` cria banco e tabelas só se faltarem, e o `ALTER DATABASE ... READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE` só roda se o RCSI estiver desligado, então rodar de novo não derruba conexões. O `02` cria cada índice só se faltar. O `03` cria login e usuário se faltarem; se o login existe e a senha mudou (`PWDCOMPARE`), troca a senha; e religa o usuário ao login (`ALTER USER ... WITH LOGIN`), caso o login tenha sido recriado.
- **Logins.** `joya_app`: `GRANT SELECT, INSERT, UPDATE ON SCHEMA::dbo` + `DENY DELETE ON SCHEMA::dbo`. `claude_mcp`: `db_datareader` + `VIEW DEFINITION`, `SHOWPLAN`, `VIEW DATABASE STATE` + `DENY INSERT, UPDATE, DELETE ON SCHEMA::dbo`. Os `DENY` são defesa em profundidade dentro do que a spec §6.5 já diz (sem DELETE; somente leitura) e não acrescentam permissão. `CHECK_POLICY = ON`, `CHECK_EXPIRATION = OFF`. Nenhuma senha no script (DB-07 vazio).
- **Fixture (ADR-0003).** `SqlServerFixture` sobe `mcr.microsoft.com/mssql/server:2022-latest` (constante `Imagem`), copia `database/` para `/database` (`WithResourceMapping`) e roda `bash /database/init.sh <etapas>` por `ExecAsync`. Código diferente de 0 falha o fixture com `stderr` e `stdout`. As etapas são **descobertas** na pasta (`NN-*.sql`), menos `04-carga`. Assim, cada commit fica verde sozinho (T-04.1 só com `01`, T-04.2 com `01-02`, T-04.3 com `01-03`), e a carga fica fora do fixture (ADR-0003 item 2). As senhas de `sa`, `joya_app` e `claude_mcp` são geradas a cada execução (`RandomNumberGenerator`). O fixture expõe `ConnectionStringSa`, `ConnectionStringApp`, `ConnectionStringMcp`, `ExecutarInitAsync` (para os testes `Carga_*` da T-04.4) e `ResetAsync` (Respawn com `sa`, só as 4 tabelas de `dbo`, `WithReseed = true`). Coleção única `SqlServer` e `DisableTestParallelization` (item 5).
- **A `WebApplicationFactory` ainda não entra.** A API não tem banco nem configuração até a US-05; o fixture já expõe `ConnectionStringApp` para quando ela entrar (ADR-0003 item 3).
- **Pacotes.** `Testcontainers.MsSql` 4.16.0, `Respawn` 7.0.0 e `Microsoft.EntityFrameworkCore.SqlServer` 8.0.34, todos da lista aprovada (BE-17). O Respawn 7 não traz driver, e o `Microsoft.Data.SqlClient` vem do EF Core SqlServer. A alternativa, Respawn 6.2.1, puxaria o SqlClient 4.0.5, antigo. Quando a Infrastructure referenciar o EF, a versão deve ser a mesma 8.0.34.
- O `.gitattributes` já força `eol=lf` em `*.sh` (DB-08): `git check-attr eol database/init.sh` → `lf`, e `file` confirma que não há CRLF. Nada a ajustar.

### Testes criados ou alterados

| Teste | Tipo | Cobre | Commit |
|---|---|---|---|
| `Schema_ScriptsAplicados_CriaTabelasChavesEConstraints` | integração | spec §6.2 (tabelas, colunas e tipos, PK/UQ com colunas e clusterização, FKs `NO_ACTION` confiáveis, CHECKs, DEFAULTs) e §4.3 (RCSI) | T-04.1 |
| `Schema_EstoqueNegativo_RejeitadoPeloCheck` | integração | RN-08, `CK_Produtos_Estoque` (erro 547) | T-04.1 |
| `Schema_ProdutoRepetidoNoPedido_RejeitadoPelaChavePrimaria` | integração | RN-03, `PK_ItensPedido` (erro 2627) | T-04.1 |
| `Scripts_AplicadosDuasVezes_NaoFalhamNemDuplicamObjetos` | integração | RNF-04, idempotência do `init.sh` (saída 0, `stderr` vazio, mesma contagem de objetos) | T-04.1 |
| `Indices_ScriptAplicado_ExistemIndicesDaSpec` | integração | spec §6.3: os 4 índices com chaves, direção, `INCLUDE`, `UNIQUE` e filtro, e nenhum índice a mais | T-04.2 |
| `Indices_ChaveIdempotenciaRepetida_RejeitadaENulasPermitidas` | integração | RN-14, `UX_Pedidos_ChaveIdempotencia` (erro 2601; dois `NULL` aceitos) | T-04.2 |
| `LoginApp_TentaDelete_PermissaoNegada` | integração | spec §6.5 e RN-12 (erro 229; a linha continua lá) | T-04.3 |
| `LoginApp_SelectInsertUpdate_Permitidos` | integração | spec §6.5 (prova que a negação não vem de um login sem acesso nenhum) | T-04.3 |
| `LoginApp_TentaDdl_PermissaoNegada` | integração | spec §6.5, sem DDL (erro 262) | T-04.3 |
| `LoginMcp_TentaEscrever_PermissaoNegada` (6 casos: INSERT nas 4 tabelas, UPDATE, DELETE) | integração | spec §6.5, cenário "Menor privilégio" da US-04 (erro 229; dados intactos) | T-04.3 |
| `LoginMcp_TentaDdl_PermissaoNegada` | integração | spec §6.5 (erro 262) | T-04.3 |
| `LoginMcp_LeituraDefinicaoEstadoEPlano_Permitidos` | integração | spec §6.5: `SELECT`, `VIEW DEFINITION`, `VIEW DATABASE STATE` e `SHOWPLAN` | T-04.3 |

### Verificações executadas

| Comando | Resultado | Resumo da saída |
|---|---|---|
| `dotnet build backend/Joya.Pedidos.sln` | Passou | `0 Aviso(s) 0 Erro(s)` |
| `dotnet test backend/tests/Joya.Pedidos.UnitTests` | Passou | `Aprovado: 19, Com falha: 0` |
| `dotnet format backend/Joya.Pedidos.sln --verify-no-changes` | Passou | código de saída 0, depois de renomear as constantes privadas para `_camelCase` (regra IDE1006 do `.editorconfig`) |
| `dotnet test backend/tests/Joya.Pedidos.IntegrationTests` | **Não executado** | o disco `C:` chegou a 0 MB livres na 1ª execução, logo depois do `docker pull` da imagem do SQL Server, e o Docker Desktop parou (`dockerDesktopLinuxEngine` não encontrado) |
| `bash -n database/init.sh` | Passou | sintaxe ok (shellcheck não instalado) |
| Greps do checklist: DB-03, DB-04, DB-05, DB-07, BE-07, BE-17 | Passou | DB-03, DB-07 e BE-07 vazios; DB-04 e DB-05 presentes; pacotes dentro da lista |

As listas esperadas no catálogo, como o texto normalizado das definições de CHECK, DEFAULT e filtro (por exemplo `([Estoque]>=(0))`), seguem o formato que o SQL Server costuma devolver, mas **ainda não foram conferidas contra um servidor**. A primeira execução real pode exigir ajuste de texto nessas listas, nunca de regra.

### Evidência no banco (MCP `mssql`)

Não coletada. O banco do compose só nasce na T-04.5, e o Docker parou por falta de disco. Evidência prevista para a delegação 2: `search_objects` (tabelas e índices), `fn_my_permissions` como `claude_mcp` e `explain_sql` de Q1, Q4 e Q8.

### Desvios em relação à spec

| PROP | Texto atual da spec | Proposta | Motivo |
|---|---|---|---|
| — | — | — | Nenhum desvio no que foi implementado |

### Bloqueios e perguntas

- **Bloqueio de ambiente.** O disco `C:` está cheio (119 GB, 24 MB livres) e o Docker Desktop parou. O desenvolvedor precisa liberar espaço em `C:` ou mover o *disk image location* do Docker Desktop para `D:` (317 GB livres). O agente não apagou nada. Depois disso, rodar `dotnet test backend/tests/Joya.Pedidos.IntegrationTests`.
- **Pergunta para a Parte 4 (T-10.x), sem impacto agora.** `sys.dm_exec_query_stats` exige `VIEW SERVER PERFORMANCE STATE`, uma permissão de servidor que a spec §6.5 não dá ao `claude_mcp`. Há duas saídas. A primeira é medir pelo Query Store (`sys.query_store_runtime_stats`), que só exige `VIEW DATABASE STATE`, vem ligado por padrão no SQL Server 2022 e não muda a spec. A segunda é abrir a `PROP-14` para acrescentar `VIEW SERVER PERFORMANCE STATE` ao `claude_mcp`. Sugestão: Query Store, que mantém o menor privilégio.

## Resposta a achados (US-04, delegação 1)

| Achado | O que foi feito | Onde | Teste que prova |
|---|---|---|---|
| Falha do gate de integração (correção, rodada 1): 15/17, `SqlException` 468 "Cannot resolve collation conflict between Latin1_General_CI_AS_KS_WS and SQL_Latin1_General_CP1_CI_AS in concat operator" | Cada coluna de catálogo (`name`, `type`, `*_desc`, `definition`, `filter_definition`, `OBJECT_NAME`, colunas do `INFORMATION_SCHEMA` e as listas do `STRING_AGG`) passou a entrar no `CONCAT` com `COLLATE DATABASE_DEFAULT`. As asserções e as listas esperadas não mudaram: nenhum texto de catálogo precisou de ajuste, o que também confere contra o SQL Server 2022 real a ressalva acima sobre o formato de CHECK, DEFAULT e filtro | `backend/tests/Joya.Pedidos.IntegrationTests/Banco/SchemaTests.cs`, `.../Banco/IndicesTests.cs` | `Schema_ScriptsAplicados_CriaTabelasChavesEConstraints`, `Indices_ScriptAplicado_ExistemIndicesDaSpec`; `dotnet test backend/tests/Joya.Pedidos.IntegrationTests`: `Aprovado: 17, Com falha: 0`; build `0 Aviso(s)`; `dotnet format --verify-no-changes` código 0 |

---

## US-04 — Banco de dados (delegação 2: T-04.4 e T-04.5)

### Tarefas concluídas

| Tarefa | Mensagem de commit prevista | Status |
|---|---|---|
| T-04.4 | `feat(db): adiciona carga de dados de exemplo` | Pronta (sem commit) |
| T-04.5 | `feat(docker): sobe o SQL Server e aplica os scripts automaticamente` | Pronta (sem commit) |

### Arquivos alterados

- T-04.4: `database/04-carga.sql`; `backend/tests/Joya.Pedidos.IntegrationTests/Banco/CargaTests.cs`.
- T-04.5: `docker-compose.yml`, `.env.example`.
- O fixture (`Infra/`) **não foi alterado**: ver a decisão abaixo.

### Decisão: como o fixture trata a carga

O fixture continua sem aplicar `04-carga` (ADR-0003, item 2), então os testes de schema, índices e logins seguem vendo o banco vazio. `CargaTests` aplica a carga por conta própria, por `ExecutarInitAsync(["04-carga"])` (o mesmo `init.sh`), e chama `ResetAsync` no `InitializeAsync` e no `DisposeAsync`, deixando as quatro tabelas vazias para as demais classes. Os testes rodam em série (`DisableTestParallelization`), então não há corrida.

### Conteúdo da carga e idempotência

12 clientes (2 pares de homônimos, 2 inativos, 1 sem pedidos, 1 só com 2025), 15 produtos (3 inativos, 1 esgotado, 1 com saldo 3) e 34 pedidos (11 em 2025, 23 em 2026) com 68 itens, incluindo as fronteiras `2025-12-31T23:59:59.999`, `2026-01-01T00:00:00`, `2026-02-28T23:59:59.999` e `2026-03-01T00:00:00`. `PrecoUnitario` copia `Preco` e `ValorTotal` é a soma dos itens. A carga é histórica: não debita estoque. Clientes e produtos entram pela chave natural (`Email`, `Sku`), e os pedidos só entram se `Pedidos` estiver vazia (rodar de novo, ou com pedidos já criados pela API, não toca em nada).

### Testes criados

| Teste | Tipo | Critério |
|---|---|---|
| `Carga_AplicadaDuasVezes_NaoDuplicaLinhas` | integração | RNF-04 (contagem das 4 tabelas igual após a 2ª aplicação) |
| `Carga_ComPedidoJaGravado_NaoInsereOsPedidosDeExemplo` | integração | RNF-04 |
| `Carga_Aplicada_ContemHomonimosInativosEPedidosDe2025E2026` | integração | CA-33, CA-35 |
| `Carga_Aplicada_ValorTotalIgualASomaDosItens` | integração | RN-09 |

### Verificações executadas

| Comando | Resultado | Resumo |
|---|---|---|
| `dotnet build backend/Joya.Pedidos.sln` | Passou | `0 Aviso(s) 0 Erro(s)` |
| `dotnet test backend/tests/Joya.Pedidos.UnitTests` | Passou | `Aprovado: 19` |
| `dotnet test backend/tests/Joya.Pedidos.IntegrationTests` | Passou | `Aprovado: 21` (17 anteriores + 4 de carga) |
| `dotnet format backend/Joya.Pedidos.sln --verify-no-changes` | Passou | código 0 |
| `docker compose config --quiet` | Passou | sem aviso |
| `docker compose up -d db db-init` (2 vezes) | Passou | `db` healthy; `db-init` `Exited (0)` nas duas (a 2ª rodou os 4 scripts sem erro) |

### Evidência no banco

**O MCP `mssql` não autenticou** (`AUTH_FAILED`, ver PROP-04-01 e bloqueio abaixo). A evidência foi colhida com `docker exec ... sqlcmd -I` no contêiner `db`, usando cada login real, com as mesmas consultas que iriam ao MCP.

- Contagens (como `sa`): `Clientes 12 · Produtos 15 · Pedidos 34 · Itens 68 · clientes inativos 2 · produtos inativos 3 · pedidos 2025: 11 · pedidos 2026: 23`; homônimos `João Souza 2`, `Maria Silva 2`; acentos íntegros (UTF-8 pelo `sqlcmd`); `is_read_committed_snapshot_on = 1`.
- `claude_mcp`: `SELECT COUNT(*) FROM dbo.Pedidos` = 34; `INSERT` 0, `SHOWPLAN` 1, `VIEW DATABASE STATE` 1; `DELETE FROM dbo.Pedidos` → `Msg 229 ... DELETE permission was denied`.
- `joya_app`: `SELECT` 1, `INSERT` 1, `UPDATE` 1, `DELETE` 0; `DELETE FROM dbo.Pedidos` → `Msg 229`.

O banco ficou de pé: `docker compose ps` mostra `db` healthy e `db-init` `Exited (0)`.

### Desvios em relação à spec

| PROP | Texto atual | Proposta | Motivo |
|---|---|---|---|
| PROP-04-01 | `.mcp.json` usa `MCP_SQL_PASSWORD` com padrão `Claude_Mcp_2026` | Trocar o padrão no `.mcp.json` (fora dos meus caminhos) para `Mcp_Leitura_2026`, igual ao do compose | O SQL Server recusa senha que contém o nome do login (erro 33064, confirmado: `Joya_App_2026` para `joya_app` e `Claude_Mcp_2026` para `claude_mcp` falham; `Pedidos_Api_2026` e `Mcp_Leitura_2026` passam). O `db-init` falhou na 1ª tentativa por isso. Os padrões do compose agora são `Pedidos_Api_2026` e `Mcp_Leitura_2026` |

### Bloqueios e perguntas

- O orquestrador/desenvolvedor precisa alterar o padrão em `.mcp.json` (`MCP_SQL_PASSWORD`) para `Mcp_Leitura_2026` e reiniciar o MCP. Enquanto isso, o MCP não conecta. Alternativa: exportar `MCP_SQL_PASSWORD=Mcp_Leitura_2026` antes de iniciar o Claude Code.
- A senha `sa` padrão do compose é `Joya_Dev_Sa_2026` (só desenvolvimento).

### Resposta a achados (delegação 2)

Nenhum achado recebido.

## Rodada 1 — 2026-11-13 — US-05, delegação 1 (T-05.1 e T-05.2)

### Tarefas

| Tarefa | Status | Mensagem de commit prevista | Arquivos principais |
|---|---|---|---|
| T-05.1 | Pronta | `feat(api): padroniza erros em Problem Details com code e traceId` | `backend/src/Joya.Pedidos.Api/Erros/ErroInternoExceptionHandler.cs`, trechos `T-05.1` do `Program.cs`, `Joya.Pedidos.Api.csproj` (Swashbuckle), `backend/tests/Joya.Pedidos.IntegrationTests/Api/{ApiFactory,RotasDeTesteController,PipelineDeErrosTests}.cs`, `IntegrationTests.csproj` (Mvc.Testing) |
| T-05.2 | Pronta | `feat(api): adiciona health checks de liveness e readiness` | `backend/src/Joya.Pedidos.Api/Saude/BancoDeDadosHealthCheck.cs`, trechos `T-05.2` do `Program.cs`, `Core/Common/IVerificadorDeBanco.cs`, `Infrastructure/{DependencyInjection,Saude/VerificadorDeBanco}.cs`, `Infrastructure.csproj`, `IntegrationTests/Api/HealthTests.cs` |

Separação no `Program.cs` (marcada por comentários `// --- T-05.1 ---` e `// --- T-05.2 ---`):
- T-05.1: `ClearProviders`/`AddJsonConsole`, `AddProblemDetails` (acrescenta `traceId`), `AddExceptionHandler`, `AddSwaggerGen`, `UseExceptionHandler`, `UseStatusCodePages` e o bloco do Swagger (Development, Docker, Testing).
- T-05.2: `AddInfrastructure()`, `AddHealthChecks` e os dois `MapHealthChecks`.
- Os `using` e o `Program.cs` compartilhado: para separar os commits, o `git add -p` resolve; os dois blocos não se entrelaçam. Os `.csproj` também dividem: Swashbuckle e Mvc.Testing são da T-05.1; `Microsoft.EntityFrameworkCore.SqlServer` na Infrastructure é da T-05.2.

Decisões de design:
- `/health/ready` usa uma porta `IVerificadorDeBanco` (Core) implementada na Infrastructure com `SqlConnection` + `SELECT 1`, e o `IHealthCheck` na Api só a chama. A arquitetura (§2 item 4) fala em `DbContext.Database.CanConnectAsync`, mas o `DbContext` só nasce na T-06.4. Quando ele existir, basta trocar o corpo do `VerificadorDeBanco`; o contrato HTTP não muda. Não é divergência da spec (§5.x só exige "verifica conexão com o SQL Server"), por isso sem PROP.
- Connection string só por configuração (`ConnectionStrings:Default`, variável `ConnectionStrings__Default`), lida na hora do uso. Sem valor, `ready` devolve 503 e `live` continua 200.
- `Microsoft.EntityFrameworkCore.SqlServer 8.0.34` na Infrastructure (já aprovado, BE-17) para trazer o SqlClient. Pacotes adicionados dentro da lista BE-17: `Swashbuckle.AspNetCore 6.9.0` (Api) e `Microsoft.AspNetCore.Mvc.Testing 8.0.11` (testes). Nenhum pacote novo fora da lista, nenhum pacote de health check.
- O 500 é escrito via `IProblemDetailsService`, sem passar a exceção ao contexto: o corpo nunca leva mensagem nem stack trace, em qualquer ambiente. A exceção vai para o log JSON. O `UseExceptionHandler()` vale também em Development (sem *developer exception page*).
- 415: vem do próprio MVC (`[ApiController]` com `[FromBody]`), em `application/problem+json` com `traceId`. `UseStatusCodePages` converte também 404/405 sem corpo.
- Rotas só de teste (`/_teste/excecao`, `/_teste/eco`) ficam num controller do projeto de testes, adicionado por `AddApplicationPart` na `ApiFactory`; nada disso existe no assembly da API.
- O Swagger sobe em Development, `Docker` e `Testing` (PROP-12). O nome `Docker` para o ambiente fica a confirmar na T-13.1/T-15.2 (compose).

### Testes criados

| Teste | Tipo | Cobre |
|---|---|---|
| `Excecao_NaoTratada_Retorna500ProblemDetailsSemStackTrace` | integração | CA-38, spec 5.1 (500) |
| `Post_ContentTypeTexto_Retorna415` | integração | spec 5.1 (415) |
| `Post_ContentTypeJson_Retorna200` | integração | controle do teste do 415 |
| `Swagger_AmbienteDeTeste_PublicaODocumento` | integração | spec 5.1 / PROP-12 |
| `HealthLive_ProcessoNoAr_Retorna200` | integração | CA-36 (com banco inalcançável: liveness independe dele) |
| `HealthReady_BancoDisponivel_Retorna200` | integração | CA-36 (SQL Server real, login `joya_app`) |
| `HealthReady_BancoIndisponivel_Retorna503` | integração | CA-36 (`127.0.0.1:1`, recusa imediata; não derruba o contêiner) |

### Verificações executadas

| Comando | Resultado | Resumo da saída |
|---|---|---|
| `dotnet build backend/Joya.Pedidos.sln` | Passou | `0 Aviso(s) 0 Erro(s)` |
| `dotnet test backend/tests/Joya.Pedidos.UnitTests` | Passou | `Aprovado: 19` (inclui as regras de arquitetura) |
| `dotnet test backend/tests/Joya.Pedidos.IntegrationTests` | Passou | `Aprovado: 28` (21 anteriores + 7 novos) |
| `dotnet format backend/Joya.Pedidos.sln --verify-no-changes` | Passou | código 0 |

### Evidência no banco (MCP `mssql`)

Não se aplica: as tarefas não gravam nem consultam dados. A readiness foi provada contra o SQL Server do Testcontainers (200) e contra um endereço recusado (503).

### Desvios em relação à spec

Nenhum. PROP: nenhuma.

### Bloqueios e perguntas

- Nenhum. Para o compose (T-13.1/T-15.2): a API precisa de `ConnectionStrings__Default` (login `joya_app`) e `ASPNETCORE_ENVIRONMENT=Docker` para expor o Swagger.

### Resposta a achados (US-05, delegação 1)

Nenhum achado recebido.

## Rodada 1 — 2026-11-13 — US-05, delegação 2 (T-05.3)

### Tarefas

| Tarefa | Status | Mensagem de commit prevista | Arquivos principais |
|---|---|---|---|
| T-05.3 | Pronta | `feat(api): limita tamanho do corpo e taxa de criação de pedidos` | `backend/src/Joya.Pedidos.Api/Limites/{LimitesHttp,LimiteDeTaxaOptions,LimiteCriarPedidoPolicy,LimiteDeCorpoFilter}.cs`, `backend/src/Joya.Pedidos.Api/Erros/RequisicaoHttpInvalidaExceptionHandler.cs`, `backend/src/Joya.Pedidos.Api/appsettings.json` (novo), trechos `T-05.3` do `Program.cs`, `backend/tests/Joya.Pedidos.IntegrationTests/Api/{LimitesHttpTests,ApiFactory,RotasDeTesteController}.cs` |

Separação no `Program.cs` para o commit por tarefa (`HL-24`):
- o bloco `// --- T-05.3: limites contra abuso ---`, antes do `Build()`;
- o bloco `// --- T-05.3 ---` (`UseForwardedHeaders` e `UseRateLimiter`), antes do `MapControllers`;
- a linha `AddExceptionHandler<RequisicaoHttpInvalidaExceptionHandler>()`, marcada `// T-05.3`, dentro do bloco da T-05.1 (precisa vir antes do handler do 500);
- os `using` de `Joya.Pedidos.Api.Limites`, `Microsoft.AspNetCore.HttpOverrides` e `Microsoft.AspNetCore.Mvc`.

Na `ApiFactory`, são da T-05.3 a constante `PermissoesNoAmbienteDeTeste`, a chave `LimiteDeTaxa:CriarPedido:Permissoes` e os `using` `System.Globalization` e `Joya.Pedidos.Api.Limites`. No `RotasDeTesteController`, são da T-05.3 a action `CriarPedido` (`POST /_teste/pedidos`) e os `using` de `Limites` e `RateLimiting`.

Decisões de design:
- **413, caminho escolhido (ADR-0002, item 1).** O teste `PostPedidos_CorpoAcimaDoLimite_Retorna413` foi escrito primeiro e ficou vermelho (200) só com `KestrelServerOptions.Limits.MaxRequestBodySize`. A opção 1 (`[RequestSizeLimit]` na action) **também** ficou vermelha: o `TestServer` do .NET 8 não expõe `IHttpMaxRequestBodySizeFeature`, e o filtro do MVC só registra no log que não aplicou o limite. Segui a **opção 2**: `LimiteDeCorpoFilter`, filtro de recurso que rejeita `Content-Length > LimitesHttp.CorpoMaximoBytes` antes do *model binding*. Ele lança a mesma `BadHttpRequestException(413)` que o Kestrel lança. O novo `RequisicaoHttpInvalidaExceptionHandler`, registrado antes do `ErroInternoExceptionHandler`, converte as duas origens no mesmo Problem Details: `status 413`, `traceId` e nenhum `code`, como no catálogo da spec 5.1. Um `400` de corpo malformado no nível HTTP sai com `code = VALIDACAO`. A mensagem da exceção só vai para o log.
- **Filtro global, e não só na action.** O filtro está em `MvcOptions.Filters` (global), espelhando o limite global do Kestrel. Hoje só o `POST /pedidos` recebe corpo, e assim a T-06.4 não precisa lembrar de mais um atributo. O ADR fala em filtro "no POST"; o global cobre o POST e não muda o contrato, por isso sem PROP. O `[RequestSizeLimit]` foi retirado: seria redundante com o Kestrel (mesmo valor) e inerte no `TestServer`.
- **Corpo *chunked*.** Sem `Content-Length`, quem barra é o Kestrel (`MaxRequestBodySize = 32 768`), como previsto no ADR. O `TestServer` não aplica esse limite, então o teste `Kestrel_LimiteDoCorpo_IgualAoCorpoMaximo` prova a configuração lendo `IOptions<KestrelServerOptions>` do host. A prova ponta a ponta com o Kestrel real fica para o E2E ou o compose (sugestão ao `qa`: `POST` *chunked* de 33 KiB → `413`).
- **429.** `AddRateLimiter` com a política nomeada `LimitesHttp.PoliticaCriarPedido` (`"criar-pedido"`), implementada em `LimiteCriarPedidoPolicy : IRateLimiterPolicy<string>`:
  - `FixedWindowRateLimiter` por `RemoteIpAddress`; sem IP, a partição é `"desconhecido"`;
  - `QueueLimit = 0`;
  - valores de `LimiteDeTaxa:CriarPedido:{Permissoes,JanelaSegundos}`, com padrão 10 a cada 10 s no código e no `appsettings.json`, validados no *start*;
  - o `OnRejected` da política escreve Problem Details `429` com `code = LIMITE_DE_TAXA`, `traceId` e `Retry-After` (lido de `MetadataName.RetryAfter` do *lease*, com a janela como reserva).
- **IP atrás de proxy (ADR-0002, item 3).** `ForwardedHeadersOptions` com `XForwardedFor`, `ForwardLimit` vindo de `Proxy:ForwardedLimit` (padrão 1, validado ≥ 1) e `KnownNetworks`/`KnownProxies` vazios. `UseForwardedHeaders` vem antes de `UseRateLimiter`. O teste `PostPedidos_IpsDistintos_TemCotasSeparadas` prova a partição por IP via `X-Forwarded-For`. O override `Proxy__ForwardedLimit=2` do deploy fica para a T-17.x (compose de produção).
- **HL-19.** A `ApiFactory` sobe `Permissoes` para `100 000` em todos os testes. O teste do 429 usa `WithWebHostBuilder` com `Permissoes = 2` e janela de 60 s, para que a cota não renove no meio do teste. O teste `PostPedidos_AmbienteDeTesteComRajadaParalela_NaoRetorna429` dispara 20 `POST`s simultâneos, como o CA-10, e confere que nenhum recebe 429.

**Como a T-06.4 liga a política no `POST /pedidos`:**
1. Na action `PedidosController.CriarAsync`, acrescentar `[EnableRateLimiting(LimitesHttp.PoliticaCriarPedido)]` (`using Microsoft.AspNetCore.RateLimiting;` e `using Joya.Pedidos.Api.Limites;`). O limite de corpo já vale, porque o filtro é global. Recomendado: `[ProducesResponseType<ProblemDetails>(413)]` e `(429)`, para o Swagger documentar os dois status.
2. Em `LimitesHttpTests`, trocar a constante `_rotaCriarPedido` de `"/_teste/pedidos"` para `"/pedidos"` e o corpo de teste para um `CriarPedidoRequest`. O 413 continua decidido antes do *model binding*. No 429, as duas primeiras respostas deixam de ser `200`: ou o teste semeia cliente e produto, ou confere `!= 429` nelas.
3. Remover a action `CriarPedido` do `RotasDeTesteController`.

Nenhum nome de teste muda; a spec e o backlog continuam valendo.

### Testes criados ou alterados

| Teste | Tipo | Cobre |
|---|---|---|
| `PostPedidos_CorpoAcimaDoLimite_Retorna413` | integração | RNF-06, ADR-0002 item 1 (`413` em Problem Details com `traceId`, sem detalhe de exceção) |
| `PostPedidos_CorpoNoLimite_Retorna200` | integração | controle do 413: exatamente 32 768 bytes passam |
| `Kestrel_LimiteDoCorpo_IgualAoCorpoMaximo` | integração | ADR-0002 item 1: proteção contra corpo *chunked* configurada no Kestrel |
| `PostPedidos_RajadaAcimaDoLimite_Retorna429` | integração | RNF-06, HL-19, ADR-0002 item 2 (`429`, `code = LIMITE_DE_TAXA`, `traceId`, `Retry-After` entre 1 e 60 s) |
| `PostPedidos_IpsDistintos_TemCotasSeparadas` | integração | ADR-0002 itens 2 e 3 (partição por IP via `X-Forwarded-For`) |
| `Post_RotaSemPolitica_NaoSofreLimiteDeTaxa` | integração | ADR-0002 item 2 (a política só vale onde é aplicada) |
| `PostPedidos_AmbienteDeTesteComRajadaParalela_NaoRetorna429` | integração | HL-19 (20 `POST`s paralelos, como o CA-10, sem 429) |
| `ApiFactory` (alterado) | infraestrutura de teste | HL-19: `LimiteDeTaxa:CriarPedido:Permissoes = 100000` |

### Verificações executadas

| Comando | Resultado | Resumo da saída |
|---|---|---|
| `dotnet build backend/Joya.Pedidos.sln` | Passou | `0 Aviso(s) 0 Erro(s)` |
| `dotnet test backend/tests/Joya.Pedidos.UnitTests` | Passou | `Aprovado: 19` |
| `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~Limite"` | Passou | `Aprovado: 7`. Antes da correção, o 413 ficou vermelho com o limite do Kestrel e com `[RequestSizeLimit]` |
| `dotnet test backend/tests/Joya.Pedidos.IntegrationTests` | Passou | `Aprovado: 35` (28 anteriores + 7 novos) |
| `dotnet format backend/Joya.Pedidos.sln --verify-no-changes` | Passou | código 0 |
| Grep do BE-23 em `backend/src/Joya.Pedidos.Api` | Parcial, como esperado | `MaxRequestBodySize`, `AddRateLimiter`, `UseForwardedHeaders` e `ForwardedLimit` presentes; `UseForwardedHeaders` vem antes de `UseRateLimiter`. O grep de `EnableRateLimiting` em `Controllers/` só fecha na T-06.4 |

### Evidência no banco (MCP `mssql`)

Não se aplica: a tarefa não grava nem consulta dados.

### Desvios em relação à spec

Nenhum. PROP: nenhuma.

Observação para o revisor de arquitetura: o checklist BE-23 procura o literal `EnableRateLimiting("criar-pedido")`. A T-06.4 usará `EnableRateLimiting(LimitesHttp.PoliticaCriarPedido)`, cujo valor é `"criar-pedido"`. Se o grep literal for obrigatório, basta ajustar o comando do checklist (arquivo do `arquiteto`).

### Bloqueios e perguntas

- Nenhum bloqueio. Pendências registradas para outras tarefas:
  - a T-06.4 liga a política (roteiro acima);
  - a T-17.x define `Proxy__ForwardedLimit=2` no compose de produção;
  - a prova do 413 *chunked* com o Kestrel real cabe ao E2E ou ao compose.

### Resposta a achados (US-05, delegação 2)

Nenhum achado recebido.

## Rodada 1 — 2026-11-13 — US-06 (delegação 1: T-06.1, T-06.2 e T-06.3, só o Core)

### Tarefas

| Tarefa | Status | Mensagem de commit prevista | Arquivos principais |
|---|---|---|---|
| T-06.1 | Pronta | `feat(core): modela pedido e itens com valor total em decimal (RN-11)` | `Core/Pedidos/{Pedido,ItemPedido}.cs`, `UnitTests/Pedidos/PedidoTests.cs` |
| T-06.2 | Pronta | `feat(core): valida estrutura do pedido (RN-01 a RN-04)` | `Core/Pedidos/{CriarPedidoRequest,CriarPedidoItemRequest,CriarPedidoValidator}.cs`, `Core/Joya.Pedidos.Core.csproj` (FluentValidation 11.11.0), `UnitTests/Pedidos/CriarPedidoValidatorTests.cs` |
| T-06.3 | Pronta | `feat(core): orquestra a criação do pedido (RN-05 a RN-08, RN-13)` | `Core/Pedidos/{PedidoService,IPedidoService,PedidoErrors,IPedidoRepository,PedidoResponse,PedidoItemResponse,ItemSemEstoque}.cs`, `Core/Clientes/*`, `Core/Produtos/*`, `Core/Estoque/IEstoqueRepository.cs`, `Core/Common/IUnitOfWork.cs`, `UnitTests/Apoio/*`, `UnitTests/Pedidos/PedidoServiceTests.cs` |

Arquivos de mais de uma tarefa: nenhum. Dependência de ordem: o validator (T-06.2) usa as constantes `Pedido.MaximoItens` e `ItemPedido.QuantidadeMinima/Maxima` (T-06.1), então a T-06.2 só compila depois da T-06.1. O `csproj` do Core é só da T-06.2. Os arquivos de teste de cada tarefa são separados.

### Portas definidas no Core (contrato para T-06.4 e T-06.5)

| Porta (namespace) | Método | Devolve |
|---|---|---|
| `IClienteRepository` (`Core.Clientes`) | `ObterAsync(int id, ct)` | `ClienteResumo?` (`Id`, `Nome`, `Ativo`); `null` se não existir |
| `IProdutoRepository` (`Core.Produtos`) | `ObterAtivosAsync(IReadOnlyCollection<int> ids, ct)` | `IReadOnlyList<ProdutoResumo>` (`Id`, `Nome`), só ativos |
| `IProdutoRepository` | `ObterEstoquesAsync(IReadOnlyCollection<int> ids, ct)` | `IReadOnlyDictionary<int,int>` (id para estoque atual), só para a mensagem do 409 |
| `IEstoqueRepository` (`Core.Estoque`) | `TentarBaixarAsync(int produtoId, int quantidade, ct)` | `decimal?`: preço do `OUTPUT inserted.Preco`, ou `null` com 0 linhas |
| `IPedidoRepository` (`Core.Pedidos`) | `AdicionarAsync(Pedido pedido, ct)` | `Result<int>` com o id gerado; `PedidoErrors.ChaveIdempotenciaDuplicada()` na violação de `UX_Pedidos_ChaveIdempotencia` |
| `IUnitOfWork` (`Core.Common`) | `ExecutarEmTransacaoAsync<T>(Func<CancellationToken, Task<Result<T>>> operacao, ct)` com `where T : notnull` | o `Result<T>` da operação; commit em `Success`, rollback em `Failure` |

Notas de design:
- `IPedidoService.CriarAsync(CriarPedidoRequest request, CancellationToken ct)` devolve `Result<PedidoResponse>`. Sem `chaveIdempotencia` e sem `ResultadoCriacao`: a idempotência é a T-12.1, que estende a assinatura. `Pedido` já tem `ChaveIdempotencia` (coluna do schema) e `Pedido.Criar(..., Guid? chaveIdempotencia = null)`.
- Pedido e itens são gravados pelo `AdicionarAsync`; o service monta o `PedidoResponse` em memória (id vem do `Result<int>`, nomes de `ClienteResumo`/`ProdutoResumo`). Itens da resposta saem em ordem crescente de `ProdutoId`.
- Fluxo do 409: dentro da transação o service tenta todas as baixas (spec 4.3 item 4), acumula as falhas e devolve `Failure` (rollback). Depois do rollback lê `ObterEstoquesAsync` e monta `ItemSemEstoque` (`ProdutoId`, `ProdutoNome`, `QuantidadeSolicitada`, `EstoqueDisponivel`). Extensões do `Error`: `produtoIds` (422) e `itens` (409), como na spec 5.2.
- `ClienteResumo`/`ProdutoResumo` são `sealed record`; não se chamam `Cliente`/`Produto` para não cair na regra "entidade sem setter público" dos testes de arquitetura.
- Entidades: `Pedido.Criar`/`ItemPedido.Criar` lançam `ArgumentException` se violados os invariantes (erro de programação, a validação estrutural vem antes). `DataPedido` é truncada em ms e UTC dentro de `Pedido.Criar`. `ItemPedido.Subtotal` é calculada (a T-06.4 deve ignorá-la no mapeamento EF). Não há construtor sem parâmetros: a T-06.4 mapeia por construtor privado ou por campo (`_itens`); `DataPedido` lida do banco volta com `Kind=Unspecified` (ajustar com conversor).
- Relógio de teste: `UnitTests/Apoio/RelogioFalso` (subclasse de `TimeProvider`) em vez de `FakeTimeProvider`, para não acrescentar o pacote `Microsoft.Extensions.TimeProvider.Testing` (fora da lista). `UnidadeDeTrabalhoFalsa` executa a operação e conta commits e rollbacks.
- O registro do service em DI não foi feito: a regra de arquitetura proíbe `Microsoft.Extensions.DependencyInjection` no Core (a skill citava `Core/DependencyInjection.cs`). Fica para a T-06.4/T-06.6, na Api/Infrastructure.
- `PedidoErrors.ChaveIdempotenciaDuplicada()` foi antecipada (`Conflict`, `CHAVE_IDEMPOTENCIA_DUPLICADA`) porque a porta `AdicionarAsync` a cita (arquitetura, fluxo de criação); o tratamento (reapresentação) é da T-12.1/T-12.2.
- `FluentValidation` 11.11.0 no Core (BE-17, aprovado). Nenhum outro pacote novo.

### Testes criados ou alterados

| Teste | Tipo | Cobre |
|---|---|---|
| `Criar_ItensValidos_CalculaValorTotalEmDecimal` | unit | RN-11, CA-01 (359,70) |
| `Criar_PrecosComCentavos_SomaSemErroDeBinario`, `Criar_InstanteComTicks_TruncaDataEmMilissegundosEmUtc`, `Criar_InstanteComOffset_ConverteParaUtc` | unit | RN-11, RN-13 |
| `Criar_SemItens_Lanca`, `Criar_MaisDeCinquentaItens_Lanca`, `Criar_CinquentaItens_Aceita`, `Criar_ProdutoRepetido_Lanca`, `Criar_ClienteNaoPositivo_Lanca`, `CriarItem_*` | unit | invariantes de RN-01 a RN-04 |
| `Validate_ItensVazios_RetornaErroNoCampoItens`, `Validate_ItensAusentes_RetornaErroNoCampoItens` | unit | CA-02, RN-01 |
| `Validate_QuantidadeForaDoLimite_RetornaErroNaQuantidade` (0, -5, 10001), `Validate_QuantidadeNosLimites_NaoTemErros` | unit | CA-03, RN-02 |
| `Validate_ProdutoRepetido_RetornaErroDeDuplicidade` | unit | CA-04, RN-03 |
| `Validate_MaisDeCinquentaItens_RetornaErroNoCampoItens`, `Validate_CinquentaItens_NaoTemErros` | unit | RN-01 |
| `Validate_ClienteNaoPositivo_*`, `Validate_ProdutoNaoPositivo_*`, `Validate_PedidoValido_NaoTemErros` | unit | RN-04 |
| `CriarAsync_PedidoValido_RetornaPedidoComPrecoDoBancoETotal` | unit | CA-01, RN-09, RN-11 |
| `CriarAsync_ClienteInexistente_RetornaClienteNaoEncontrado` | unit | CA-05 |
| `CriarAsync_ClienteInativo_RetornaClienteInativo` | unit | CA-06 |
| `CriarAsync_ProdutosInvalidos_RetornaTodosOsIdsInvalidos` | unit | CA-07 |
| `CriarAsync_EstoqueInsuficiente_RetornaConflitoSemGravar`, `CriarAsync_VariosItensSemEstoque_ListaTodosOsItens` | unit | CA-08, spec 4.3 item 4 |
| `CriarAsync_ItensForaDeOrdem_BaixaEmOrdemCrescenteDeProdutoId` (`Received.InOrder`) | unit | spec 4.3 item 3 |
| `CriarAsync_PedidoValido_UsaDataDoTimeProviderEmUtc`, `CriarAsync_RelogioComTicks_TruncaDataPedidoEmMilissegundos` | unit | RN-13, HL-20 |
| `CriarAsync_ClienteInexistenteEProdutoInvalido_...Primeiro`, `CriarAsync_ClienteInativoEProdutoInvalido_...Primeiro`, `CriarAsync_ProdutoInvalido_NaoTentaBaixarEstoque` | unit | ordem da spec 4.2 |
| `CriarAsync_AdicionarFalha_RepassaOErroEFazRollback`, `CriarAsync_TokenInformado_RepassaParaAsPortas` | unit | ADR-0004, regra de ouro 3 |

Os testes de arquitetura `Entidades_Propriedades_NaoTemSetterPublico` e `MetodosAsync_Assinatura_...` agora têm alvos reais (Pedido, ItemPedido e os métodos `*Async` do Core) e passam.

### Verificações executadas

| Comando | Resultado | Resumo da saída |
|---|---|---|
| `dotnet build backend/Joya.Pedidos.sln` | Passou | `0 Aviso(s) 0 Erro(s)` |
| `dotnet test backend/tests/Joya.Pedidos.UnitTests` | Passou | `Aprovado: 64, Falha: 0` (inclui arquitetura) |
| `dotnet test backend/tests/Joya.Pedidos.IntegrationTests` | Passou | `Aprovado: 35` (sem testes novos: nada de HTTP ou banco nesta delegação) |
| `dotnet format backend/Joya.Pedidos.sln --verify-no-changes` | Passou | código 0 |

### Evidência no banco (MCP `mssql`)

Não se aplica: só o Core, sem I/O.

### Desvios em relação à spec

Nenhum. PROP: nenhuma.

### Bloqueios e perguntas

- Nenhum.

### Resposta a achados (US-06, delegação 1)

Nenhum achado recebido.

## Rodada 1 — 2026-11-14 — US-06 (delegação 2: T-06.4 e T-06.5, Infrastructure)

### Tarefas

| Tarefa | Status | Mensagem de commit prevista | Arquivos principais |
|---|---|---|---|
| T-06.4 | Pronta | `feat(infra): mapeia o schema existente com EF Core` | `Infrastructure/Persistencia/{PedidosDbContext,UnitOfWork,ConversorDataUtc}.cs`, `Infrastructure/Persistencia/Configuracoes/{Cliente,Produto,Pedido,ItemPedido}Configuracao.cs`, `Infrastructure/Clientes/{Cliente,ClienteRepository}.cs`, `Infrastructure/Produtos/{Produto,ProdutoRepository}.cs`, `Infrastructure/Pedidos/PedidoRepository.cs`, `Infrastructure/Saude/VerificadorDeBanco.cs`, `Infrastructure/DependencyInjection.cs`, `IntegrationTests/Persistencia/{InfraestruturaReal,Arranjo,MapeamentoTests,UnitOfWorkTests,LeiturasTests}.cs` |
| T-06.5 | Pronta | `feat(infra): debita estoque com instrução atômica (RN-08, RN-09)` | `Infrastructure/Estoque/EstoqueRepository.cs`, `IntegrationTests/Persistencia/EstoqueRepositoryTests.cs` |

**Separação por tarefa (HL-24).** Trechos de uma tarefa em arquivo de outra, todos marcados com o comentário `// T-06.x`:

| Arquivo | Dono | Trecho de outra tarefa | Efeito se o trecho for commitado junto |
|---|---|---|---|
| `Core/Pedidos/Pedido.cs` (sem commit, da T-06.1) | T-06.1 | `// T-06.4`: construtor privado de 4 parâmetros `(clienteId, dataPedido, chaveIdempotencia, valorTotal)` | Inofensivo para a T-06.1: é privado e não muda invariante nem teste. O EF Core não liga parâmetro de construtor a navegação, então a materialização precisa de um construtor sem a lista de itens, e os itens entram pelo campo `_itens`. Sugestão: commitar junto com a T-06.1, ou tirar as 6 linhas do commit da T-06.1 e devolvê-las no da T-06.4. |
| `Infrastructure/DependencyInjection.cs` | T-06.4 | `// T-06.5`: os dois `using ...Estoque` e o registro `IEstoqueRepository → EstoqueRepository` | **Retirar obrigatoriamente do commit da T-06.4**: sem o `EstoqueRepository.cs`, o build quebra. |
| `Infrastructure/Persistencia/PedidosDbContext.cs` | T-06.4 | `// T-06.5`: `DefaultTypeMapping<decimal>().HasPrecision(18, 2)` (tipo do escalar `OUTPUT inserted.Preco`) | Inofensivo na T-06.4: só define o mapeamento padrão dos escalares `decimal`. |
| `IntegrationTests/Persistencia/Arranjo.cs` | T-06.4 | `EstoqueAsync`, usado só pelos testes da T-06.5 | Inofensivo. |

**Registros adiados para a T-06.6 (Api).** O `AddInfrastructure` registra, todos como `Scoped`:
- o `PedidosDbContext`;
- o `IUnitOfWork`;
- os quatro repositórios;
- o `IVerificadorDeBanco`.

Ele **não** registra o `IPedidoService` nem o `TimeProvider.System`. Os dois ficam no `Program.cs` da Api, como pedem a arquitetura (§3, item 6) e o BE-08, que espera exatamente um `TimeProvider.System` em `backend/src/Joya.Pedidos.Api`.

Notas de design:
- **Contexto.** O `PedidosDbContext` usa `UseSqlServer`, e a connection string `ConnectionStrings:Default` só é lida quando o contexto é criado, porque o `ApiFactory` injeta a configuração depois. Não há repetição automática no provedor nem estratégia de execução (ADR-0004, HL-23, BE-22).
- **Mapeamento sobre o schema.** Foi escrito à mão sobre `database/01-schema.sql`:
  - nomes de tabela, PK e FK iguais aos do banco (`PK_*`, `FK_ItensPedido_Pedidos`, `FK_ItensPedido_Produtos`, `FK_Pedidos_Clientes`), com `OnDelete(NoAction)`;
  - tamanho e unicode de cada coluna iguais aos do banco, com `decimal(18,2)` e `datetime2(3)`;
  - nenhum índice declarado no modelo (BE-18): os índices vivem só em `02-indices.sql`;
  - `Clientes.CriadoEm` e `Produtos.CriadoEm` ficam fora do mapeamento, porque têm `DEFAULT` e a aplicação não as lê.
- **ItemPedido.** `PedidoId` é propriedade de sombra, já que a entidade do Core não conhece o pai. `Subtotal` é ignorada. `Itens` usa `PropertyAccessMode.Field` sobre `_itens`.
- **ConversorDataUtc.** Todo `DateTime` lido volta com `Kind = Utc` (spec 6.4, JSON com `Z`). Na gravação o valor não muda, porque a entidade já o truncou em milissegundos (HL-20).
- **Entidades da Infrastructure.** `Cliente` e `Produto` existem só para mapear FK e consultas, com setters privados. O Core continua só com `ClienteResumo` e `ProdutoResumo`.
- **Leituras.** Usam `AsNoTracking()` e projetam direto para o DTO do Core (BE-12, regra de ouro 10). `ObterAtivosAsync` e `ObterEstoquesAsync` passam os ids num único parâmetro JSON (`OPENJSON`, EF 8), o que dá um plano único para qualquer quantidade de itens.
- **UnitOfWork.** Abre a transação com `BeginTransactionAsync(ReadCommitted)`, com RCSI ligado no banco.
  - `Success`: `CommitAsync`.
  - `Failure`: `RollbackAsync` e depois `ChangeTracker.Clear()`.
  - Exceção: `ChangeTracker.Clear()` e propaga; o `DisposeAsync` da transação desfaz o que foi feito.
- **PedidoRepository.AdicionarAsync.** Grava pedido e itens num único `SaveChanges`. Só vira `PedidoErrors.ChaveIdempotenciaDuplicada()` a `DbUpdateException` cuja `SqlException` tem número 2601 ou 2627 e cita `UX_Pedidos_ChaveIdempotencia` (ADR-0004, item 3). Qualquer outra falha de banco continua sendo exceção.
- **VerificadorDeBanco.** Usa o mesmo `PedidosDbContext` (`Database.CanConnectAsync`), sem conexão ADO.NET própria. O CA-36 continua verde.
- **EstoqueRepository.TentarBaixarAsync (T-06.5).**
  - Chama `Database.SqlQuery<decimal>` com `FormattableString`: cada `{valor}` vira um parâmetro `@pN`.
  - O texto é exatamente o da spec 4.3, com `OUTPUT inserted.Preco AS Value`, materializado por `ToListAsync` sem composição.
  - Roda no `DbContext` do escopo e, portanto, na transação corrente do `IUnitOfWork`.
  - Tem duas guardas contra erro de programação: quantidade ≤ 0 lança `ArgumentOutOfRangeException` (uma quantidade negativa somaria estoque), e baixa fora de transação lança `InvalidOperationException` (RN-10).

### Testes criados ou alterados

Todos os testes são de integração, contra o SQL Server do Testcontainers:
- a Infrastructure é montada pelo próprio `AddInfrastructure`, com `ValidateOnBuild`;
- a aplicação usa o login de menor privilégio `joya_app`;
- o Arrange e o Assert usam `sa`, com SQL parametrizado.

| Teste | Tipo | Cobre |
|---|---|---|
| `Mapeamento_ModeloEf_ColunasIguaisAoSchema` | integração | spec 6.2: modelo relacional do EF (tabela, coluna, tipo, nulidade e nomes de PK e FK) igual ao catálogo, exceto as duas colunas `CriadoEm` |
| `Mapeamento_PedidoGravado_LeIgualAoSchema` | integração | spec 6.4, RN-11, RN-13 e HL-20: grava pelo EF, confere pelo SQL puro (`2026-11-14T12:34:56.123`, `40.99`, chave e itens) e relê pelo EF (`Kind = Utc`, itens e subtotais) |
| `AdicionarAsync_ChaveIdempotenciaRepetida_RetornaChaveIdempotenciaDuplicada` | integração | RN-14, ADR-0004 item 3 |
| `AdicionarAsync_SemChaveIdempotencia_GravaVariosPedidos` | integração | RN-14 (índice filtrado) |
| `AdicionarAsync_OutraFalhaDeBanco_ContinuaExcecao` | integração | ADR-0004 item 3 (violação de FK, erro 547, continua exceção) |
| `ExecutarEmTransacaoAsync_OperacaoSucesso_ConfirmaAlteracoes` | integração | RN-10 |
| `ExecutarEmTransacaoAsync_OperacaoFalha_DesfazAlteracoes` | integração | RN-10 (o INSERT já enviado é desfeito e o `ChangeTracker` fica vazio) |
| `ExecutarEmTransacaoAsync_OperacaoLanca_DesfazEPropaga` | integração | RN-10 |
| `ExecutarEmTransacaoAsync_DepoisDeFalha_MesmoEscopoGravaDeNovo` | integração | RN-10 (sem o `Clear()`, o pedido desfeito seria reenviado) |
| `ObterAsync_ClienteExistente_RetornaResumoComAtivo`, `ObterAsync_ClienteInexistente_RetornaNulo` | integração | RN-05, RN-06 |
| `ObterAtivosAsync_AtivosInativosEInexistentes_RetornaSoAtivosOrdenados` | integração | RN-07 |
| `ObterEstoquesAsync_Ids_RetornaEstoqueAtualInclusiveDeInativo` | integração | spec 5.2 (corpo do 409) |
| `TentarBaixarAsync_EstoqueSuficiente_DebitaERetornaPreco` | integração | RN-08, RN-09 |
| `TentarBaixarAsync_EstoqueInsuficiente_NaoDebita` | integração | RN-08, CA-08 (no nível do repositório) |
| `TentarBaixarAsync_ConsomeTodoEstoque_ZeraEstoque` | integração | CA-09 (no nível do repositório) |
| `TentarBaixarAsync_ProdutoInativo_NaoDebita`, `TentarBaixarAsync_ProdutoInexistente_RetornaNulo` | integração | RN-07 garantida pela própria instrução |
| `TentarBaixarAsync_PrecoDeTabelaAlterado_RetornaPrecoVigente` | integração | RN-09, CA-11 (no nível do repositório) |
| `TentarBaixarAsync_TransacaoDesfeita_EstoqueVolta` | integração | RN-10 |
| `TentarBaixarAsync_ForaDeTransacao_LancaENaoDebita` | integração | RN-10 |
| `TentarBaixarAsync_QuantidadeNaoPositiva_LancaENaoAlteraEstoque` (0 e -3) | integração | RN-02 (defesa em profundidade) |
| `TentarBaixarAsync_BaixasConcorrentes_NuncaDebitaAlemDoEstoque` | integração | spec 4.3, RN-08: 20 baixas paralelas (20 contextos e 20 transações) sobre estoque 5 resultam em 5 preços, 15 nulos e estoque 0. O CA-10 via HTTP é da T-06.7. |

Separação: `EstoqueRepositoryTests.cs` pertence só à T-06.5, e os demais arquivos de teste, à T-06.4.

### Verificações executadas

| Comando | Resultado | Resumo da saída |
|---|---|---|
| `dotnet build backend/Joya.Pedidos.sln` | Passou | `0 Aviso(s) 0 Erro(s)` |
| `dotnet test backend/tests/Joya.Pedidos.UnitTests` | Passou | `Aprovado: 64, Falha: 0`, incluindo os testes de arquitetura (classes `sealed`, `CancellationToken` por último, Infrastructure sem ASP.NET Core) |
| `dotnet test backend/tests/Joya.Pedidos.IntegrationTests` | Passou | `Aprovado: 59, Falha: 0` (35 anteriores e 24 novos). `HealthTests` (CA-36) verdes com o verificador novo |
| `dotnet format backend/Joya.Pedidos.sln --verify-no-changes` | Passou | código 0 |

Greps do checklist (arquitetura §8), com `git grep --untracked`:

| Item | Resultado |
|---|---|
| BE-06, BE-07 (pragma/NoWarn/SuppressMessage e `!`), BE-08 (`DateTime.Now/UtcNow`), BE-09, BE-10, BE-11 (`CancellationToken.None` em `src`), BE-13 (`.Include(` em `src`), BE-14b (`*Raw`), BE-18, BE-19, BE-22a | vazio |
| BE-12 | só `Infrastructure/Estoque/EstoqueRepository.cs`, como esperado: o `SqlQuery` não rastreia |
| BE-14a | só `Infrastructure/Estoque/EstoqueRepository.cs` |
| BE-15 | **3 linhas, todas falso positivo.** `EstoqueRepository.cs:30` e `:34` são o próprio SQL da instrução atômica (`SET Estoque = Estoque - {quantidade}` e `AND Estoque >= {quantidade}`), executado pelo SQL Server e não pelo C#. `Core/Pedidos/PedidoErrors.cs:23` casa com `ItemSemEstoque>` (arquivo da delegação 1). Sugestão ao arquiteto: excluir `EstoqueRepository.cs` do grep e usar `\bEstoque\s*(...)`. |
| BE-17 | só pacotes da lista; nenhum pacote novo nesta delegação |
| BE-22b | só `Pedidos/PedidoRepository.cs` |

### Evidência no banco (MCP `mssql`)

**Não executada.** O MCP respondeu `AUTH_FAILED` ("Authentication failed for source 'joya'") em `explain_sql` e em `search_objects`, embora o contêiner `joya-pedidos-db-1` estivesse saudável. A senha do login `claude_mcp` que o MCP usa não confere com a do banco local. A correção depende do arquivo de variáveis de ambiente local, que este agente não lê.

Consultas previstas para quando o acesso voltar:

```sql
-- Q7: baixa atômica. Esperado: Clustered Index Update com Seek em PK_Produtos (Id), predicado residual Ativo/Estoque.
UPDATE dbo.Produtos SET Estoque = Estoque - 1 OUTPUT inserted.Preco AS Value WHERE Id = 1 AND Ativo = 1 AND Estoque >= 1;
-- Q9: cliente. Esperado: Clustered Index Seek em PK_Clientes.
SELECT Id, Nome, Ativo FROM dbo.Clientes WHERE Id = 1;
-- Q10: produtos ativos. Esperado: OPENJSON + Nested Loops com Seek em PK_Produtos.
SELECT p.Id, p.Nome FROM dbo.Produtos p WHERE p.Id IN (SELECT CAST([value] AS int) FROM OPENJSON(N'[1,2,3]')) AND p.Ativo = 1 ORDER BY p.Id;
```

Até lá, o efeito real no banco fica provado pelos testes de integração acima: contagens, estoque e linhas gravadas conferidos por SQL com `sa`.

### Desvios em relação à spec

Nenhum. PROP: nenhuma.

### Bloqueios e perguntas

- O MCP `mssql` responde `AUTH_FAILED` (ver acima). O desenvolvedor precisa conferir a senha do `claude_mcp` na configuração local do compose e do MCP.
- O commit da T-06.4 precisa sair sem as linhas `// T-06.5` do `DependencyInjection.cs` (ver a tabela de separação).

### Resposta a achados (US-06, delegação 2)

Nenhum achado recebido.

---

## Rodada 1 — 2026-11-14 — US-06 (delegação 3: T-06.6, `POST /pedidos`)

### Tarefa concluída

| Tarefa | Mensagem de commit prevista |
|---|---|
| T-06.6 | `feat(api): expõe POST /pedidos` |

### Arquivos

- Novos: `Api/Controllers/PedidosController.cs`, `Api/Erros/ProblemasHttp.cs` (`Error.ToProblem` e 400 `VALIDACAO`), `Api/Erros/ValidationFilter.cs`, `IntegrationTests/Pedidos/PostPedidosTests.cs`.
- Alterados: `Api/Program.cs` (filtro global, `InvalidModelStateResponseFactory`, `TimeProvider.System`, `IPedidoService`, `IValidator<CriarPedidoRequest>`), `IntegrationTests/Api/LimitesHttpTests.cs`, `IntegrationTests/Api/RotasDeTesteController.cs` (action `CriarPedido` removida; `eco` e `excecao` mantidas).

### Testes (nome → CA)

`PostPedidos_PedidoValido_Retorna201ComLocationEDebitaEstoque` → CA-01; `_SemItens_Retorna400SemGravar` → CA-02; `_QuantidadeInvalida_Retorna400` (0, -1, 10001) → CA-03; `_ProdutoRepetido_Retorna400` → CA-04; `_ClienteInexistente_Retorna422` → CA-05; `_ClienteInativo_Retorna422` → CA-06; `_ProdutosInvalidos_Retorna422ComIds` → CA-07; `_EstoqueInsuficiente_Retorna409SemGravarNada` → CA-08; `_ConsomeTodoEstoque_Retorna201EZeraEstoque` → CA-09; `_PrecoNoCorpo_IgnoraEUsaPrecoDeTabela` → CA-12; `_RegrasEmConflito_Retorna400Primeiro` → CA-14; extras: `_JsonMalformado_Retorna400`, `_TipoInvalidoNoCorpo_Retorna400`. Cada um confere status, `Content-Type`, `code`, `traceId` e o estado no banco (pedidos, itens, estoque) por SQL com `sa`.

`LimitesHttpTests` agora mira `/pedidos` com `CriarPedidoRequest` válido. Como o banco é inalcançável nesses testes, a fábrica troca o `IPedidoService` por um dublê NSubstitute que aceita o pedido (esperam `201` no lugar de `200`). O corpo de exatamente 32 KiB é um pedido válido com propriedade extra de preenchimento.

### Verificações

| Comando | Resultado |
|---|---|
| `dotnet build backend/Joya.Pedidos.sln` | 0 aviso, 0 erro |
| `dotnet test backend/tests/Joya.Pedidos.UnitTests` | 64 aprovados |
| `dotnet test backend/tests/Joya.Pedidos.IntegrationTests` | 74 aprovados (59 anteriores + 15 casos de `PostPedidos`) |
| `dotnet format ... --verify-no-changes` | limpo (após um `dotnet format` que ajustou espaços) |

### Evidência no banco (MCP `mssql`)

Não consultado (CG-05, `AUTH_FAILED` conhecido). O efeito real fica provado pelos testes de integração, que leem `dbo.Pedidos`, `dbo.ItensPedido` e `dbo.Produtos` como `sa` depois de cada requisição.

### Pontos de atenção e desvios

1. **`ValidationFilter` e `ToProblem` não existiam.** O pedido dizia "ValidationFilter existente", mas só havia o desenho na arquitetura. Criei os dois (`Api/Erros/`), sem pacote novo: o filtro resolve `IValidator<T>` pelo DI e o validator é registrado à mão em `Program.cs` (`AddValidatorsFromAssemblyContaining` exigiria o pacote `FluentValidation.DependencyInjectionExtensions`). Cada novo request com validator precisa de uma linha de registro.
2. **`Location` sem `CreatedAtAction`.** O `GET /pedidos/{id}` só chega na T-08.2. O controller usa `Created("/pedidos/{id}", pedido)`, igual à spec 5.2. Quando a T-08.2 existir, pode trocar por `CreatedAtAction`.
3. **Problem Details pelo `IProblemDetailsService`.** O `[Produces("application/json")]` do controller sobrescrevia o tipo de mídia de um `ObjectResult`; por isso `ToProblem` devolve um `IActionResult` que escreve pelo serviço (`application/problem+json` e `traceId`, como os handlers de exceção). O `errors` do 400 vai como extensão de um `ProblemDetails` simples, porque o serviço não serializa a subclasse `ValidationProblemDetails`.
4. **Tipo `type`/`title`.** `type` é `/erros/<code-em-kebab>` e `title` é genérico por tipo de erro; a spec mostra títulos específicos (ex.: "Estoque insuficiente"). O `code` e o `detail` seguem a spec. Se o título específico for exigido, vira PROP (o `Error` não tem título).
5. `Idempotency-Key` não está ligado: `IPedidoService.CriarAsync` ainda não recebe a chave (US-12).
6. Swagger real e `docs/contratos/openapi.json` ficam para a T-11.2; não conferi o JSON publicado nesta rodada.

PROP-xx: nenhuma.

### Resposta a achados (US-06, delegação 3)

| Achado | O que foi feito | Onde | Teste que prova |
|---|---|---|---|
| Correção do orquestrador: item 4 (`title`) contraria a spec 5.2 | Mapa `code` → `title` em `ProblemasHttp` (`PRODUTO_NAO_ENCONTRADO`, `ESTOQUE_INSUFICIENTE`, `CLIENTE_NAO_ENCONTRADO`, `CLIENTE_INATIVO`, `CHAVE_IDEMPOTENCIA_DUPLICADA`); o título genérico por tipo fica de fallback. O item 4 acima está superado. | `Api/Erros/ProblemasHttp.cs` | asserções de `title` e `type` nos `PostPedidos_*` de 409 e 422 |
| Nomes das extensões `produtoIds` e `itens` | Já saíam em camelCase; agora o teste de CA-08 confere a ordem e os nomes exatos `produtoId, produtoNome, quantidadeSolicitada, estoqueDisponivel`, e o de CA-07 confere `produtoIds`. Antes só se lia cada propriedade por nome. | `PostPedidosTests` | `PostPedidos_EstoqueInsuficiente_Retorna409SemGravarNada`, `PostPedidos_ProdutosInvalidos_Retorna422ComIds` |

---

## Rodada 1 — 2026-11-14 — US-06 (delegação 4: T-06.7, concorrência)

### Tarefas

| Tarefa | Status | Mensagem de commit prevista | Arquivos principais |
|---|---|---|---|
| T-06.7 | Pronta | `test(api): prova que o estoque não fica negativo sob concorrência (CA-10)` | `backend/tests/Joya.Pedidos.IntegrationTests/Pedidos/ConcorrenciaTests.cs` (único arquivo da tarefa, novo) |

Nenhum arquivo de produção mudou nesta delegação (HL-24: o commit da T-06.7 leva só o arquivo de teste).

### Como a simultaneidade é garantida (largada real)

Duas barreiras, uma em cada camada:

1. **Aplicação.** As 20 tarefas são criadas paradas num `TaskCompletionSource` e soltas juntas por um único `SetResult()`. Um `HttpClient` do `ApiFactory`; o `TestServer` processa cada requisição no *thread pool*.
2. **Banco.** Antes da largada, o teste abre uma transação como `sa` e segura `SELECT … WITH (UPDLOCK, ROWLOCK)` na linha do produto (ou dos dois produtos). Só solta (`ROLLBACK`) quando conta, em `sys.dm_exec_requests` + `sys.dm_exec_sessions`, **20** requisições do login `joya_app` em espera `LCK_M_*`. O teste afirma `enfileiradas == 20`.

Consequência: as 20 requisições já passaram pelas leituras (cliente, catálogo, sob `READ_COMMITTED_SNAPSHOT`) e estão dentro da própria transação, paradas no mesmo `UPDATE`, antes de qualquer baixa. A disputa pela linha é total e não depende da sorte do agendador. Uma implementação *check-then-act* vende acima do estoque de forma determinística nesse arranjo (ver mutação 2). Os prazos não são "timeout às cegas": 15 s para enfileirar (abaixo do timeout de comando do EF, 30 s) e 60 s de teto só para a suíte nunca travar; o observado é ~0,3 s por cenário.

### Testes criados

| Teste | Tipo | Cobre |
|---|---|---|
| `PostPedidos_VinteRequisicoesSimultaneas_EstoqueNuncaFicaNegativo(5, 1, 5, 0)` | integração | CA-10 literal: estoque 5, 20 × 1 unidade → 5 × `201`, 15 × `409 ESTOQUE_INSUFICIENTE`, estoque final 0, 5 pedidos com o produto |
| `PostPedidos_VinteRequisicoesSimultaneas_EstoqueNuncaFicaNegativo(7, 2, 3, 1)` | integração | CA-10 com sobra: 20 × 2 unidades sobre estoque 7 → 3 × `201`, 17 × `409` com `estoqueDisponivel` 1, estoque final 1 |
| `PostPedidos_DoisProdutosEmOrdensOpostas_SemDeadlockNemEstoqueNegativo` | integração | CA-10 + spec 4.3 itens 2, 3 e 4: metade dos corpos `[A, B]`, metade `[B, A]`; A (estoque 5) é o gargalo, B (estoque 8) sobra → 5 × `201`, 15 × `409` listando só A com saldo 0, A = 0, B = 3, 10 itens, zero `500` |

Asserções de cada cenário: `enfileiradas == 20`; todo status é `201` ou `409` (nenhum `500`); contagem exata de `201` e `409`; `estoqueInicial − estoqueFinal == aceitos × quantidade`; estoque final igual ao esperado e `≥ 0`; cada `409` é `application/problem+json`, `code` `ESTOQUE_INSUFICIENTE`, um item com `produtoId`, `quantidadeSolicitada` e `estoqueDisponivel` corretos; cada `201` é `application/json` com `valorTotal = quantidade × preço`. No banco, como `sa`: os ids de `dbo.Pedidos` são exatamente os ids devolvidos nos `201`; `dbo.ItensPedido` tem uma linha por aceite com produto, quantidade e preço de tabela (RN-09); nenhum pedido com `ValorTotal` diferente da soma dos itens (RN-11) nem sem itens; no cenário de dois produtos, todo pedido tem exatamente 2 itens e o estoque de B só caiu pelos aceites (RN-10: as baixas de B dos recusados foram desfeitas).

### Prova de que o teste pega a regressão (mutações temporárias, revertidas)

Cada mutação foi aplicada, o filtro `ConcorrenciaTests` rodado e o arquivo restaurado (conferido por `diff` contra a cópia). Nenhuma ficou no código.

| Mutação | Onde | Resultado do teste |
|---|---|---|
| 1. Tirar `AND Estoque >= {quantidade}` do `UPDATE` | `EstoqueRepository` | 3/3 falham: a 6ª baixa viola `CK_Produtos_Estoque` → `500` ("respostas … should satisfy 201 ou 409") |
| 2. *Check-then-act*: `SELECT Estoque`, compara em C#, `UPDATE SET Estoque = @novo` | `EstoqueRepository` | 3/3 falham: `aceitos.Count should be 5 but was 20` e `should be 3 but was 20` (venda de 20 unidades com estoque 5, sem a CHECK perceber) |
| 3. Ignorar a ordem por `ProdutoId` (`OrderBy(_ => 0)`) | `PedidoService` | `DoisProdutosEmOrdensOpostas` falha: 5 vítimas de *deadlock* (SqlException 1205, sessões 63, 64, 69, 70 e 72) viram `500`, sem *retry* (ADR-0004). Os dois cenários de um produto continuam verdes, como esperado |

### Verificações executadas

| Comando | Resultado | Resumo da saída |
|---|---|---|
| `dotnet build backend/Joya.Pedidos.sln` | Passou | `0 Aviso(s) 0 Erro(s)` |
| `dotnet test backend/tests/Joya.Pedidos.UnitTests` | Passou | 64 aprovados |
| `dotnet test backend/tests/Joya.Pedidos.IntegrationTests` (3 execuções seguidas) | Passou 3/3 | 77 aprovados em cada (74 anteriores + 3 casos novos); 9 s, 20 s, 21 s |
| `dotnet test … --filter "FullyQualifiedName~ConcorrenciaTests"` (5 execuções extras) | Passou 5/5 | cenários de um produto 237–358 ms; dois produtos ~2 s (primeiro da classe, inclui subir o host) |
| `dotnet format backend/Joya.Pedidos.sln --verify-no-changes` | Passou | exit 0 (após renomear as constantes privadas para `_requisicoes` e `_preco`, IDE1006) |

### Evidência no banco (MCP `mssql`)

Não consultado (CG-05, `AUTH_FAILED` conhecido; e o cenário roda no contêiner do Testcontainers, não no banco do compose que o MCP enxerga). A evidência de banco está dentro do próprio teste, lida como `sa` no SQL Server real:

```sql
-- largada: sessões da API paradas no lock, antes de soltá-lo (afirmado = 20)
SELECT COUNT(*) FROM sys.dm_exec_requests AS r
JOIN sys.dm_exec_sessions AS s ON s.session_id = r.session_id
WHERE s.login_name = N'joya_app' AND r.wait_type LIKE N'LCK[_]M[_]%';

-- coerência: pedidos gravados = ids dos 201; total = soma dos itens
SELECT STRING_AGG(CAST(Id AS varchar(12)), ',') WITHIN GROUP (ORDER BY Id) FROM dbo.Pedidos;
SELECT COUNT(*) FROM dbo.Pedidos AS p
WHERE p.ValorTotal <> ISNULL((SELECT SUM(i.Quantidade * i.PrecoUnitario) FROM dbo.ItensPedido AS i WHERE i.PedidoId = p.Id), -1);
```

Resultado: 20 sessões enfileiradas em todas as execuções; estoque final 0 (CA-10), 1 (sobra) e A = 0 / B = 3 (dois produtos); zero divergências.

### Desvios em relação à spec

| PROP | Texto atual da spec | Proposta | Motivo |
|---|---|---|---|
| — | — | — | Nenhum. O comportamento bate com a spec 4.3 e o CA-10; nenhum *deadlock* nem `500` com o código atual. |

### Bloqueios e perguntas

- Nenhum bloqueio.
- Observação para QA/segurança: o arranjo usa `sa` só para Arrange/Assert e para a trava de largada, como os demais testes; a API continua como `joya_app`.

### Resposta a achados (US-06, delegação 4)

Nenhum achado recebido.

## US-07 — Catálogo para os seletores (T-07.1)

- Commit previsto: `feat(api): expõe clientes e produtos ativos para os seletores`.
- Arquivos: `Core/Clientes/{ClienteResponse,IClienteService}.cs`, `Core/Produtos/{ProdutoResponse,IProdutoService}.cs`, `ListarAsync(bool? ativo, ct)` nas portas e repositórios, `Api/Controllers/{Clientes,Produtos}Controller.cs`, registro em `Program.cs`.
- Consulta: `AsNoTracking`, `Where(ativo == null || Ativo == ativo)`, `OrderBy(Nome).ThenBy(Id)`, projeção direta para o DTO. `ativo` não booleano vira 400 `VALIDACAO` pelo `InvalidModelStateResponseFactory` existente.
- Testes (`IntegrationTests/Catalogo/CatalogoTests.cs`): `GetClientes_FiltroAtivo_RetornaSoAtivosOrdenadosPorNome` e `GetProdutos_FiltroAtivo_...` (CA-35), `GetClientes_SemFiltroAtivo_RetornaTodos`, `GetClientes_AtivoFalse_RetornaSoInativos`, `GetCatalogo_AtivoInvalido_Retorna400` (clientes e produtos, HL-18).
- Verificação: build 0 aviso, unitários, integração e `dotnet format` verdes.
- Evidência MCP: não coletada (CG-05, AUTH_FAILED). Sem índice novo: listagem de catálogo pequeno ordenada por `Nome`.
- Bloqueio: `git checkout -b us-07-catalogo` foi barrado pelo `guard-bash.sh`; o trabalho está na branch corrente (main), sem commit.
- Sem desvio da spec, sem PROP.

## US-08 e T-09.1 — Listagem de pedidos e validador do ranking

Um commit por tarefa; arquivos separáveis (paths em `backend/`).

| Tarefa | Commit previsto | Arquivos |
|---|---|---|
| T-08.1 | `feat(core): valida período e paginação da listagem (RN-15, RN-16)` | `src/Joya.Pedidos.Core/Common/Periodo.cs`, `Common/PeriodoRegras.cs`, `Pedidos/ListarPedidosQuery.cs`, `Pedidos/ListarPedidosQueryValidator.cs`; testes `UnitTests/Common/PeriodoTests.cs`, `UnitTests/Pedidos/ListarPedidosQueryValidatorTests.cs` |
| T-08.2 | `feat(api): expõe GET /pedidos com filtros e paginação` | `Core/Pedidos/{PedidoResumoResponse,IPedidoConsultas,PedidoConsultaService}.cs`, `Infrastructure/Pedidos/PedidoConsultas.cs`, `PedidosController.cs`, `Infrastructure/DependencyInjection.cs`, `Program.cs`; testes `UnitTests/Pedidos/PedidoConsultaServiceTests.cs`, `IntegrationTests/Pedidos/GetPedidosTests.cs`, `IntegrationTests/Persistencia/Arranjo.cs` (`InserirPedidoAsync`) |
| T-09.1 | `feat(core): exige período no relatório de top clientes (RN-17)` | `Core/Relatorios/{TopClientesQuery,TopClientesQueryValidator}.cs`; teste `UnitTests/Relatorios/TopClientesQueryValidatorTests.cs` |

Arquivo compartilhado: `Common/PeriodoRegras.cs` é da T-08.1 e a T-09.1 só o reutiliza (T-09.1 depende da T-08.1).

Linhas de arquivos tocados por uma tarefa só (T-08.2):
- `PedidosController.cs`: construtor ganha `IPedidoConsultaService consultas`, `using Joya.Pedidos.Core.Common`, a action `ListarAsync` (`[HttpGet]`) e o comentário do `Created` (agora cita T-11.1).
- `Program.cs`: duas linhas marcadas `// T-08.2` (service e `IValidator<ListarPedidosQuery>`).
- `Infrastructure/DependencyInjection.cs`: uma linha marcada `// T-08.2`.

Decisões:
- `Created("/pedidos/{id}")` mantido: o `GET /pedidos/{id}` é a T-11.1, não esta história. Trocar por `CreatedAtAction` fica para a T-11.1.
- Datas como `string?` no `Query` (arquitetura, decisão 4): `yyyy-MM-dd` estrito. `page=abc` ou `clienteId=abc` caem no `InvalidModelStateResponseFactory` (400 `VALIDACAO`).
- Serviço de leitura separado (`PedidoConsultaService`) para não mudar o construtor do `PedidoService`.
- Consulta: `AsNoTracking`, filtro `>= Inicio AND < FimExclusivo` sobre `DataPedido`, `COUNT` + página com `OFFSET/FETCH`, join com `Clientes` e `p.Itens.Count()` como subconsulta (sem `Include`, sem N+1). Página além do fim (inclusive `page=int.MaxValue`, que estouraria o `Skip`) devolve `200` com `items: []` sem consultar a página.
- Índices: os de `database/02-indices.sql` já cobrem; nenhuma alteração em `database/`.

Testes (nome → CA): `Criar_DataFimInclusiva_GeraFimExclusivoNoDiaSeguinte` (CA-17); `Validate_ParametrosInvalidos_RetornaErroDeValidacao` (CA-19); `Validate_SemDataInicio_RetornaErroDeValidacao` (CA-26); `ListarAsync_*` do serviço (CA-15, 17, 19); integração `GetPedidos_SemFiltro_RetornaPrimeiraPaginaOrdenada` (CA-15, com empate de data), `..._FiltroCliente_...` (CA-16), `..._FiltroPeriodo_IncluiAsDuasPontas` (CA-17, 28/02 23:59:59.999 e 01/04 00:00 fora), `..._PercorreTodasAsPaginas_NaoRepeteNemOmite` (CA-18), `..._ParametrosInvalidos_Retorna400` (CA-19, 11 casos), `..._ClienteSemPedidos_Retorna200Vazio` (CA-20), mais página além do fim e `quantidadeItens`.

Verificação: `dotnet build` 0 aviso, 0 erro; unitários 101 aprovados; integração 103 aprovados; `dotnet format --verify-no-changes` sem pendência.

Evidência MCP: não coletada (CG-05, AUTH_FAILED). O plano de execução (seek em `IX_Pedidos_DataPedido` / `IX_Pedidos_ClienteId_DataPedido`) fica para conferir quando o MCP voltar.

Pendência para a T-09.2: registrar `IValidator<TopClientesQuery>` no `Program.cs` (o validador existe, mas só passa a valer quando o endpoint existir).

Desvios da spec: nenhum, sem PROP.

### Resposta a achados (US-08, rodada 2)

- Gate: `default!` em `PedidoConsultaServiceTests.cs:53` (regra de ouro 1). Corrigido trocando por `Arg.Any<Periodo>()`; busca de silenciamento vazia, build 0 aviso, 101 unitários e `dotnet format` verdes (integração não rodada, a pedido).

## T-09.2 — `GET /relatorios/top-clientes`

| Tarefa | Commit previsto | Arquivos (em `backend/`) |
|---|---|---|
| T-09.2 | `feat(api): expõe GET /relatorios/top-clientes` | `src/Joya.Pedidos.Core/Relatorios/{TopClienteResponse,IRelatorioConsultas,RelatorioErrors,RelatorioService}.cs`, `src/Joya.Pedidos.Infrastructure/Relatorios/RelatorioConsultas.cs`, `src/Joya.Pedidos.Api/Controllers/RelatoriosController.cs`, `Infrastructure/DependencyInjection.cs` e `Api/Program.cs` (linhas `// T-09.2`, inclusive o `IValidator<TopClientesQuery>` pendente da T-09.1); testes `UnitTests/Relatorios/RelatorioServiceTests.cs`, `IntegrationTests/Relatorios/{GetTopClientesTests,RelatorioConsultasTests}.cs` |

Decisões:
- A porta recebe `DateTime inicio, DateTime fimExclusivo` não nulos: o ranking nunca roda sem limite de período. O serviço reaproveita `Periodo.Criar` e, se faltar uma ponta (o validator já barra antes), devolve `RelatorioErrors.PeriodoObrigatorio` (`400 VALIDACAO`).
- Consulta única, no banco: filtro semiaberto sobre `DataPedido`, `GROUP BY ClienteId`, `ORDER BY SUM DESC, ClienteId`, `TOP(5)` dentro da subconsulta e só depois o join com `Clientes` (5 seeks em `PK_Clientes`). Soma `Pedidos.ValorTotal`, sem tocar `ItensPedido`, então não há multiplicação de linhas. `posicao` é o índice das até 5 linhas já ordenadas.
- Defeitos da Parte 4 ausentes por construção, e provados por teste: agrupar por nome, `YEAR()` sobre a coluna, `LEFT JOIN` anulado pelo `WHERE`, `ORDER BY` crescente e sem `TOP`.
- Índices: o `IX_Pedidos_DataPedido (DataPedido DESC, Id DESC) INCLUDE (ClienteId, ValorTotal)` de `database/02-indices.sql` cobre a consulta (Q5). `database/` não foi alterado.

SQL gerado pelo EF, capturado no teste `TopClientesAsync_SqlGerado_UmComandoAgrupadoPorClienteSemFuncaoSobreData`. Ele exige um único comando e confere a forma da instrução:

```sql
SELECT [t].[ClienteId], [c].[Nome], [t].[ValorTotal], [t].[QuantidadePedidos]
FROM (
    SELECT TOP(@__p_2) [p].[ClienteId], COALESCE(SUM([p].[ValorTotal]), 0.0) AS [ValorTotal], COUNT(*) AS [QuantidadePedidos]
    FROM [dbo].[Pedidos] AS [p]
    WHERE [p].[DataPedido] >= @__inicio_0 AND [p].[DataPedido] < @__fimExclusivo_1
    GROUP BY [p].[ClienteId]
    ORDER BY COALESCE(SUM([p].[ValorTotal]), 0.0) DESC, [p].[ClienteId]
) AS [t]
INNER JOIN [dbo].[Clientes] AS [c] ON [t].[ClienteId] = [c].[Id]
ORDER BY [t].[ValorTotal] DESC, [t].[ClienteId]
```

Testes, com o critério de cada um:
- Integração: `GetTopClientes_OitoClientes_RetornaCincoEmOrdemDecrescente` (CA-21), `GetTopClientes_PedidosForaDoPeriodo_NaoContam` (CA-22; as duas pontas ao milissegundo), `GetTopClientes_Homonimos_NaoSomaNomesIguais` (CA-23), `GetTopClientes_Empate_OrdenaPorMenorClienteId` (CA-24; seis empatados, o sexto fica fora do TOP), `GetTopClientes_MenosDeCinco_RetornaSoQuemComprou` (CA-25, mais período vazio → `[]`), `GetTopClientes_SemPeriodo_Retorna400` (CA-26, 7 casos), `GetTopClientes_ValorTotal_IgualSomaDosItens` (CA-27; pedidos criados pelo `POST /pedidos`), `GetTopClientes_ClienteInativo_ApareceNoRanking` (RN-17) e `TopClientesAsync_SqlGerado_UmComandoAgrupadoPorClienteSemFuncaoSobreData` (RN-15/RN-17, forma do SQL).
- Unitários: `RelatorioServiceTests`, com 8 casos (fim exclusivo e tamanho 5, lista vazia, período incompleto, início maior que fim, repasse do `CancellationToken`).

Verificação: `dotnet build` com 0 aviso e 0 erro. 109 unitários aprovados. Integração: suíte completa com 118 aprovados (103 anteriores + 15 novos). `dotnet format --verify-no-changes` sem pendência. A busca por silenciamento voltou vazia.

Evidência MCP: não coletada, por causa do CG-05 (AUTH_FAILED). No lugar dela vale o SQL acima, capturado do log do EF contra o SQL Server real. O plano (*Index Seek* em `IX_Pedidos_DataPedido` → agregação → *Top N Sort* → seek em `PK_Clientes`) fica para conferir pelo `explain_sql` quando o MCP voltar.

Desvios da spec: nenhum, sem PROP.

---

## US-13 — Aplicação completa com um comando (T-13.1, T-13.2)

Arquivos: `backend/Dockerfile` e `backend/.dockerignore` (T-13.1); `docker-compose.yml` com os serviços `api` (T-13.1) e `frontend` (T-13.2).

- **API:** multi-stage `sdk:8.0` → `aspnet:8.0`, `USER app` (uid 1654), restore em camada própria. O `HEALTHCHECK` usa `bash` (`/dev/tcp`) e `grep` porque a imagem aspnet não traz curl nem wget; consulta `/health/ready`.
- **Compose:** `api` depende de `db` saudável e `db-init` concluído; conecta como `joya_app` (`ConnectionStrings__Default`, senha `${APP_SQL_PASSWORD:-...}`, `TrustServerCertificate=True` por causa do certificado autoassinado do contêiner). `ASPNETCORE_ENVIRONMENT=Docker` (o `Program.cs` já liga o Swagger nesse nome; nenhum código mudou). Porta `127.0.0.1:8080`. `frontend` depende de `api` saudável e publica `4200:8080`.
- **Nome do serviço:** `frontend`, como pediu a delegação; a arquitetura (§ serviços) o chama de `web`. Só nomenclatura, sem efeito no proxy (o Nginx só conhece o host `api`). Sugiro alinhar o texto da arquitetura.
- **Verificação** (`docker compose up --build -d`): `db` healthy, `db-init` Exited (0), `api` healthy, `frontend` healthy. `GET :4200/` 200; `GET :4200/api/health/live` 200 e `/api/health/ready` 200 (proxy real do Nginx até a API); `GET :4200/api/clientes` 200; `GET :8080/swagger/index.html` 200. `id -u`: api 1654, frontend 101. `dotnet build backend/Joya.Pedidos.sln`: 0 aviso, 0 erro. `docker compose down` (sem `-v`) ao final.
- **Não verificado:** clone limpo com `down -v` (o volume do `db` já existia; foi preservado por restrição de disco/tempo). O ciclo "subir de novo sem duplicar carga" já é coberto pelo `db-init` idempotente da US-04.
- Desvios da spec: nenhum, sem PROP.

### Resposta a achados (US-13, rodada 2)

- Gate: `db-init` saía 1 com Msg 904 num segundo `up` (JoyaPedidos ainda em recuperação). Feito: o healthcheck do `db` agora exige todos os bancos ONLINE (`docker-compose.yml`) e o `database/init.sh` espera o JoyaPedidos abrir, com retentativa limitada de 60 x 3 s, sem espera fixa e sem esperar nada no clone limpo (`DB_ID` nulo). O healthcheck sozinho não bastou, porque `sys.databases` já marca ONLINE antes do autostart; a espera no init.sh fecha a lacuna. Prova: `down` + `up -d` com volume existente subiu com db-init Exited (0), api e frontend saudáveis e `:4200/api/health/ready` 200, com 16 mensagens "aguardando" no log do db-init antes de passar. Um `up` anterior deu exit 137 (db-init Dead, contêiner morto pelo Docker sob carga) e passou ao repetir.

## US-10 — Parte 4: debug e otimização da query (T-10.1, T-10.2, T-10.3)

O detalhe está em `database/parte4/investigacao.md`: defeitos, hipóteses H1 a H6, planos antes e depois e medições.

| Tarefa | Arquivos | Commit previsto |
|---|---|---|
| T-10.1 | `database/parte4/01-query-original.sql`, `Parte4/QueryRankingTests.cs` (testes `QueryOriginal_*`), `Parte4/ScriptsParte4.cs`, `Parte4/PlanoDeExecucao.cs` | `test(db): reproduz os defeitos da query de ranking da Parte 4` |
| T-10.2 | `database/parte4/02-query-corrigida.sql`, `Parte4/QueryRankingTests.cs` (testes `QueryCorrigida_*`) | `perf(db): corrige e otimiza a query de ranking de 2026 (Parte 4)` |
| T-10.3 | `database/parte4/00-carga-volume.sql`, `03-variante-valor-total.sql`, `investigacao.md`, `Parte4/InvestigacaoTests.cs`, esta seção | `docs(db): documenta a investigação da query com plano de execução` |

Os caminhos `Parte4/` ficam em `backend/tests/Joya.Pedidos.IntegrationTests/`. `QueryRankingTests.cs` serve a T-10.1 e a T-10.2. No commit da T-10.1, ele entra só com os testes `QueryOriginal_*` e os auxiliares, ou inteiro na T-10.2, a critério do orquestrador.

**Testes de integração** (todos contra SQL Server real):

| Teste | Critério |
|---|---|
| `QueryOriginal_ClientesHomonimos_SomaIndevidamente` | RN-17 (defeito) |
| `QueryOriginal_Ordenacao_RetornaMenorTotalPrimeiro` | defeito de ordenação |
| `QueryOriginal_Plano_LeTodosOsPedidosComFuncaoSobreDataPedido` | RN-15 (defeito) |
| `QueryCorrigida_Ranking2026_CoincideComTopClientes` | CA-33 |
| `QueryCorrigida_ClientesHomonimos_UmaLinhaPorCliente` | CA-23 |
| `QueryCorrigida_FronteirasDoAno_ContaSoPedidosDe2026` | CA-22 |
| `QueryCorrigida_Empate_OrdenaPorMenorClienteId` | CA-24 |
| `Investigacao_CargaDeVolume_MedePlanosELeituras` | CA-33 com volume; trait `Categoria=Medicao`, cerca de 2 min |

**Verificações** (2026-11-14):

- `dotnet build`: 0 avisos, 0 erros.
- Testes unitários: 109/109.
- Testes de integração, suíte inteira: 126/126 em 2 min 38 s.
- `dotnet format --verify-no-changes`: exit 0.
- Grep de silenciamento: vazio.
- Grep DB-03 (`git grep --untracked`): vazio.

**Evidência.** O MCP `mssql` estava fora do ar (`AUTH_FAILED`, CG-05). O substituto autorizado foram as medições no SQL Server 2022 do Testcontainers:

- Login `claude_mcp`: `SHOWPLAN_XML`, `STATISTICS XML` e `STATISTICS IO`.
- Login `sa`: `sys.dm_exec_query_stats`.
- Volume: 5 000 clientes, 200 000 pedidos, 600 000 itens.
- Médias de 10 execuções com cache quente:

| Consulta | Leituras lógicas | CPU (ms) | Decorrido (ms) | DOP | Plano em `Pedidos` |
|---|---|---|---|---|---|
| Original | 4 779 | 1 229,0 | 714,6 | 3 | *Index Scan*, 200 000 linhas, 1 151 leituras |
| Corrigida (`02`) | 3 589 | 351,4 | 352,4 | 1 | *Index Seek* `IX_Pedidos_DataPedido`, 44 867 linhas, 248 leituras |
| Variante `ValorTotal` (`03`) | 311 | 84,5 | 84,6 | 1 | *Index Seek*, 248 leituras; não lê `ItensPedido` |

Nenhum índice novo, de acordo com a arquitetura Q12.

**Achados durante a rodada:**

- Com estatísticas herdadas de testes anteriores (Respawn), o CA-33 sobre a carga de exemplo mostrava *scan*. O teste agora atualiza as estatísticas depois da carga; detalhes na seção 9 de `investigacao.md`.
- A limpeza das 800 mil linhas de volume estoura o timeout de 30 s do Respawn. O `InvestigacaoTests` apaga antes, com timeout longo (`ScriptsParte4.RemoverDadosAsync`).

**Desvios da spec:** nenhum, sem PROP.

**Para o desenvolvedor:** a `03` é 4× mais rápida que a `02` e pede 5 MB de memória contra 81 MB. A `02` segue o backlog e o enunciado, porque soma os itens. A escolha para o README está na seção 8 de `investigacao.md`.

## US-11 — Consultar pedido por id (T-11.1)

**Tarefa pronta:** T-11.1. Commit previsto: `feat(api): expõe GET /pedidos/{id} com os itens`. A T-11.2 (contrato OpenAPI) fica para depois da US-12.

**Arquivos:** `Core/Pedidos/IPedidoConsultas.cs` (porta `ObterPorIdAsync`), `PedidoConsultaService.cs`, `PedidoErrors.cs` (`NaoEncontrado`, 404), `Infrastructure/Pedidos/PedidoConsultas.cs` (projeção única com a coleção de itens aninhada, `AsNoTracking`, sem `Include`; itens por `ProdutoId`), `Api/Controllers/PedidosController.cs` (action `GET {id:int}`; comentário do `Created` atualizado, `Location` relativo mantido).

**Testes:**
- Unitários: `ObterPorIdAsync_Inexistente_RetornaPedidoNaoEncontrado` (CA-34), `ObterPorIdAsync_Existente_RetornaOPedidoComItens` (CA-34).
- Integração (`GetPedidoPorIdTests`): `GetPedidoPorId_PrecoDeTabelaAlterado_MantemPrecoPraticado` (CA-11), `GetPedidoPorId_Inexistente_Retorna404` (CA-34, Problem Details com `code`), `GetPedidoPorId_AposCriacao_DataPedidoIgualAoRetornoDoPost` (RN-13, HL-20).

**Verificações:** build 0 avisos e 0 erros; unitários 111 aprovados; integração `--filter GetPedidoPorId` 3 aprovados; `dotnet format --verify-no-changes` limpo; `git grep CreatedAtAction -- backend/src` vazio. Suíte de integração completa e categoria Medicao não rodadas (HL-27).

**Evidência MCP:** não coletada nesta rodada (o banco de teste é efêmero, via Testcontainers). A consulta é por `PK_Pedidos` e `PK_ItensPedido` (spec 6.3). O plano de execução não foi medido.

**Desvios da spec:** nenhum, sem PROP.

## US-03 — Jobs de CI (T-03.3, T-03.4, T-03.5)

**Tarefas prontas** (alteração única em `.github/workflows/ci.yml`). Commits previstos: `ci: roda os testes de integração contra SQL Server`, `ci: valida lint, build e testes do frontend`, `ci: roda os testes end to end com Playwright`.

- `integracao`: suíte de integração com `--filter "Categoria!=Medicao"` (HL-27); o Docker do runner serve o Testcontainers. Limite de 20 min.
- `frontend`: `npm ci`, lint, testes em ChromeHeadless e build, Node 22 (igual ao `frontend/Dockerfile`), cache npm. Limite de 15 min.
- `e2e`: depende de `backend` e `frontend`; `docker compose up -d --build --wait frontend` com as senhas padrão de desenvolvimento do compose (sem arquivo de variáveis e sem segredo), `npm ci` e `npm test` em `e2e/` com o Chrome do runner. Relatório do Playwright e logs do compose só em falha; `docker compose down -v` ao final. Limite de 25 min.
- Segurança: `contents: read`, sem `pull_request_target`, sem dados do evento em `run:`, `persist-credentials: false`, actions fixadas por SHA de commit (`setup-node` v7.1.0 e `upload-artifact` v7.0.2, SHAs resolvidos pela API do GitHub), `timeout-minutes` em todos.

**Verificações:** YAML válido e `validar_config.py` executados localmente. Os jobs só se provam verdes no GitHub após o push; sem Docker local.

**Estimativa:** integração 6 a 10 min; frontend 3 a 5 min; e2e 8 a 12 min, depois de backend e frontend.

**Riscos:** `--wait` exige healthcheck na API e no frontend (existem); se o `db-init` (one-shot) atrapalhar o `--wait` no compose do runner, trocar por `up -d` e uma espera explícita. O filtro pressupõe o Trait `Categoria`, como em `InvestigacaoTests`.

**Desvios da spec:** nenhum, sem PROP.

### Resposta a achados (correção, rodada 1: ARQ-02, SEC-03, SEC-04)

HL-29: SEC-05 é risco aceito (logins não alterados). HL-27: só unitários rodaram; os testes de integração novos foram escritos mas não executados nesta rodada.

| Achado | O que foi feito | Onde | Teste que prova |
|---|---|---|---|
| SEC-03 | `dataFim = 9999-12-31` vira erro `DataFim` no validator (o dia seguinte não é representável); `Periodo.Criar` também devolve `Error.Validation` em vez de estourar `AddDays(1)` | `Core/Common/PeriodoRegras.cs`, `Core/Common/Periodo.cs` | Unit: `ListarPedidosQueryValidatorTests.Validate_DataFimNoMaximoDoCalendario_RetornaErroNoCampoDataFim`, `TopClientesQueryValidatorTests.Validate_DataFimNoMaximoDoCalendario_RetornaErroNoCampoDataFim`. Integração (não executados): caso `dataFim=9999-12-31` em `GetPedidosTests.GetPedidos_ParametrosInvalidos_Retorna400` e `?dataInicio=0001-01-01&dataFim=9999-12-31` em `GetTopClientesTests.GetTopClientes_SemPeriodo_Retorna400` |
| SEC-04 | Frontend publicado em `127.0.0.1:4200:8080`; comentário do cabeçalho atualizado. Nenhum outro serviço alterado | `docker-compose.yml` | `docker compose config -q` OK (sem subir nem derrubar). Para a US-17: deploy com `ASPNETCORE_ENVIRONMENT=Production` (sem Swagger) |
| ARQ-02 | Removido `WithoutRequiringPositiveResults()` de controllers e de "classes são sealed". Nas três regras `NotDependOnAny` ele é inerente ao ArchUnitNET (regra negativa não gera resultado positivo, o erro aparece mesmo com tipos existentes): mantido, com comentário correto e afirmação explícita de que os conjuntos avaliados têm tipos. Teste de entidades varre Core e Infrastructure e exige as quatro (Pedido, ItemPedido, Cliente, Produto); comentários desatualizados corrigidos | `UnitTests/Arquitetura/RegrasDeArquiteturaTests.cs` | Os 9 testes de arquitetura passam; o de entidades falha se alguma das quatro não for encontrada |

Verificações: `dotnet build` 0 avisos; unitários 113 verdes; `dotnet format --verify-no-changes` limpo; `docker compose config -q` OK.
Sem PROP (a faixa de datas não mudou a spec: só rejeita o valor não representável).

#### ARQ-02, rodada 2

A alegação da rodada 1 estava errada: na ArchUnitNET 0.13.x `ResideInNamespace` compara nome exato, então o seletor por regex estava vazio. Correção em `RegrasDeArquiteturaTests.cs`:
- Todos os seletores usam `ResideInNamespaceOrBelow`; removido o helper `Regex`.
- Removido `WithoutRequiringPositiveResults()` das três regras; removidas as asserções `ShouldNotBeEmpty` redundantes; comentário XML corrigido.
- Regra dos controllers: `NotDependOnAnyTypesThat()` para Infrastructure e, com `AndShould`, para `Microsoft.EntityFrameworkCore`. O assembly do EF Core agora é carregado no modelo.
- Prova de mutação: arquivo temporário `Api/Controllers/MutacaoTempController.cs` com propriedade do tipo `Infrastructure.Clientes.Cliente`. Resultado: `Controllers_Dependencias_NaoAcessamDbContext` vermelho ("MutacaoTempController does depend on ...Cliente"). Arquivo removido; 113 testes unitários verdes e `dotnet format` limpo.
