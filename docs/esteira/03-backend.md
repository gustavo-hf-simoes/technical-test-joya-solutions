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
