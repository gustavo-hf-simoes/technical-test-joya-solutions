# Refatoração estrutural: relatório final

| Campo | Valor |
|---|---|
| Data | 2026-11-15 |
| Branch | `chore/refatoracao-estrutural`, a partir da `main` em `1da52e3` |
| Decisões humanas | `HL-33`: executar EST-01 a EST-06 e aprovar PROP-15 a PROP-18. `HL-34`: renomear a pasta do health check de `Saude/` para `Health/`. `HL-35`: pastas técnicas em inglês, features em português, nenhum nome pela estrutura da prova |
| Diagnóstico e proposta | [revisoes/refatoracao-estrutural-1.md](revisoes/refatoracao-estrutural-1.md) (`arquiteto`) |
| Re-revisão | [revisoes/refatoracao-estrutural-2.md](revisoes/refatoracao-estrutural-2.md) (`arquiteto`) |
| Execução | `backend` (EST-01 a EST-05, PROP-17, ARQ-01, `HL-34`, `HL-35`), `frontend` (EST-06), orquestrador (`git mv`, PROP-15 e PROP-18) |

## 1. Resumo executivo

A estrutura já estava saudável. Os namespaces batiam com as pastas nos 5 projetos .NET, as dependências apontavam para o `Core` (provado pelo ArchUnitNET) e o frontend seguia o style guide do Angular. Por isso não houve mudança de arquitetura, só ajustes locais:

- **Arquivos que escondiam tipos.** Quatro arquivos guardavam interface e implementação juntas. Eles foram divididos (EST-01).
- **Lógica no lugar errado.** A adaptação do `400` do *model binding* saiu do `Program.cs` e foi para `Api/Errors/` (EST-03).
- **Peças fora da feature.** A porta de saúde saiu do `Common` (EST-02), os ajudantes dos testes de integração foram reunidos em `Fixtures/` (EST-04 e EST-05) e o rótulo de cliente do frontend foi para `pedidos/` (EST-06).
- **Nomes de pasta (`HL-35`).** Pastas técnicas em inglês (`Errors`, `Limits`, `Persistence`, `Fixtures`, `Fakes`, `Database`, `Architecture`, `Health`), features do domínio em português. `Parte4` virou `Database/RankingQuery` nos testes e `database/ranking-query` nos scripts; `Catalogo` foi dividido em `Clientes/` e `Produtos/`. A regra está no `CLAUDE.md` e na spec (§2).

Três regras que antes eram só convenção passaram a quebrar o build ou a revisão:

- **Namespace igual à pasta:** `IDE0130` como warning, que vira erro com `TreatWarningsAsErrors` (PROP-17).
- **Um tipo de topo por arquivo:** item BE-28 do checklist de arquitetura.
- **Nomes de pasta:** item BE-29 do checklist de arquitetura.

Comportamento, contratos HTTP, SQL, rotas e telas não mudaram. Não há projeto novo, dependência nova, arquivo apagado nem pasta vazia.

## 2. Diagnóstico da estrutura anterior

A tabela completa (problema, caminho atual e proposto, fonte, impacto e risco) está no §2 de [refatoracao-estrutural-1.md](revisoes/refatoracao-estrutural-1.md#2-diagnóstico). Em resumo:

| ID | Problema |
|---|---|
| EST-01 | `IClienteService.cs`, `IProdutoService.cs`, `PedidoConsultaService.cs` e `RelatorioService.cs` tinham 2 tipos cada. `ClienteService` e `ProdutoService` não eram encontráveis pelo nome do arquivo |
| EST-02 | `IVerificadorDeBanco`, porta da feature de saúde, estava em `Core/Common` |
| EST-03 | `ErrosDoModelState` e `NormalizarChave` (QA-04) moravam no `Program.cs`, que deve só compor |
| EST-04 | Os ajudantes da integração estavam espalhados em `Infra/`, `Api/` e `Persistencia/`, pastas que não dizem o que guardam |
| EST-05 | `RelatorioConsultasTests` testa o `Infrastructure` sem HTTP, mas estava em `Relatorios/` |
| EST-06 | `core/rotulo-cliente.ts` é regra de apresentação usada só por `pedidos/` |

A árvore de referência do pedido (`Persistence/`, `Repositories/`, `features/`) **não** foi adotada, porque trocaria a organização por feature por pastas por tipo (§7).

## 3. Árvore final

Mostra só o que mudou ou orienta a navegação. A árvore completa e comentada está no `CLAUDE.md`, seção Arquitetura.

```text
backend/src/
  Joya.Pedidos.Core/
    Clientes/      IClienteService.cs, ClienteService.cs (novo), ...
    Common/        Error, IUnitOfWork, PaginaResponse, Periodo, PeriodoRegras, Result, TipoErro
    Estoque/       IEstoqueRepository
    Pedidos/       IPedidoConsultaService.cs (novo), PedidoConsultaService.cs, IPedidoService.cs, PedidoService.cs, ...
    Produtos/      IProdutoService.cs, ProdutoService.cs (novo), ...
    Relatorios/    IRelatorioService.cs (novo), RelatorioService.cs, ...
    Health/        IVerificadorDeBanco.cs (movido de Common/)
  Joya.Pedidos.Infrastructure/
    Health/                      VerificadorDeBanco.cs (era Saude/)
    Persistence/Configurations/  era Persistencia/Configuracoes/
  Joya.Pedidos.Api/
    Health/                      BancoDeDadosHealthCheck.cs (era Saude/)
    Errors/ProblemasHttp.cs      agora com ErrosDoModelState e NormalizarChave (era Erros/)
    Limits/                      era Limites/
    Program.cs                   só composição
backend/tests/Joya.Pedidos.IntegrationTests/
  Fixtures/       ApiFactory, Arranjo, RotasDeTesteController, Sql, SqlServerCollection, SqlServerFixture
  Api/ Pedidos/ Relatorios/
  Clientes/       ClientesEndpointTests (era Catalogo/CatalogoTests)
  Produtos/       ProdutosEndpointTests (dividido de CatalogoTests)
  Database/       era Banco/; RankingQuery/ era Parte4/ (ScriptsParte4 → RankingQueryScripts)
  Persistence/    era Persistencia/; + RelatorioConsultasTests (movido de Relatorios/)
backend/tests/Joya.Pedidos.UnitTests/
  Fakes/          era Apoio/
  Architecture/   era Arquitetura/
database/
  ranking-query/  era parte4/ (SQL intacto)
frontend/src/app/
  core/           api/, uuid.ts
  pedidos/        + rotulo-cliente.ts e rotulo-cliente.spec.ts (movidos de core/)
  testing/
```

## 4. Movimentações (origem → destino)

| Origem | Destino | Item |
|---|---|---|
| `backend/src/Joya.Pedidos.Core/Common/IVerificadorDeBanco.cs` | `backend/src/Joya.Pedidos.Core/Health/IVerificadorDeBanco.cs` | EST-02, `HL-34` |
| `backend/src/Joya.Pedidos.Api/Saude/BancoDeDadosHealthCheck.cs` | `backend/src/Joya.Pedidos.Api/Health/BancoDeDadosHealthCheck.cs` | `HL-34` |
| `backend/src/Joya.Pedidos.Infrastructure/Saude/VerificadorDeBanco.cs` | `backend/src/Joya.Pedidos.Infrastructure/Health/VerificadorDeBanco.cs` | `HL-34` |
| `backend/tests/Joya.Pedidos.IntegrationTests/Infra/Sql.cs` | `.../IntegrationTests/Apoio/Sql.cs` | EST-04 |
| `.../IntegrationTests/Infra/SqlServerCollection.cs` | `.../IntegrationTests/Apoio/SqlServerCollection.cs` | EST-04 |
| `.../IntegrationTests/Infra/SqlServerFixture.cs` | `.../IntegrationTests/Apoio/SqlServerFixture.cs` | EST-04 |
| `.../IntegrationTests/Api/ApiFactory.cs` | `.../IntegrationTests/Apoio/ApiFactory.cs` | EST-04 |
| `.../IntegrationTests/Api/RotasDeTesteController.cs` | `.../IntegrationTests/Apoio/RotasDeTesteController.cs` | EST-04 |
| `.../IntegrationTests/Persistencia/Arranjo.cs` | `.../IntegrationTests/Apoio/Arranjo.cs` | EST-04 |
| `.../IntegrationTests/Relatorios/RelatorioConsultasTests.cs` | `.../IntegrationTests/Persistencia/RelatorioConsultasTests.cs` | EST-05 |
| `backend/src/Joya.Pedidos.Api/Erros/` | `backend/src/Joya.Pedidos.Api/Errors/` | `HL-35` |
| `backend/src/Joya.Pedidos.Api/Limites/` | `backend/src/Joya.Pedidos.Api/Limits/` | `HL-35` |
| `backend/src/Joya.Pedidos.Infrastructure/Persistencia/` (e `Configuracoes/`) | `.../Infrastructure/Persistence/` (e `Configurations/`) | `HL-35` |
| `.../IntegrationTests/Apoio/` | `.../IntegrationTests/Fixtures/` | `HL-35` |
| `.../IntegrationTests/Banco/` | `.../IntegrationTests/Database/` | `HL-35` |
| `.../IntegrationTests/Persistencia/` | `.../IntegrationTests/Persistence/` | `HL-35` |
| `.../IntegrationTests/Parte4/` (`ScriptsParte4.cs`) | `.../IntegrationTests/Database/RankingQuery/` (`RankingQueryScripts.cs`) | `HL-35` |
| `.../IntegrationTests/Catalogo/CatalogoTests.cs` | `.../IntegrationTests/Clientes/ClientesEndpointTests.cs` e `.../Produtos/ProdutosEndpointTests.cs` | `HL-35` |
| `.../UnitTests/Apoio/` | `.../UnitTests/Fakes/` | `HL-35` |
| `.../UnitTests/Arquitetura/` | `.../UnitTests/Architecture/` | `HL-35` |
| `database/parte4/` | `database/ranking-query/` | `HL-35` |
| `frontend/src/app/core/rotulo-cliente.ts` | `frontend/src/app/pedidos/rotulo-cliente.ts` | EST-06 |
| `frontend/src/app/core/rotulo-cliente.spec.ts` | `frontend/src/app/pedidos/rotulo-cliente.spec.ts` | EST-06 |

Divisões de arquivo (EST-01), com o código copiado sem reescrita:

| Arquivo original | Fica com | Arquivo novo |
|---|---|---|
| `Core/Clientes/IClienteService.cs` | `IClienteService` | `ClienteService.cs` |
| `Core/Produtos/IProdutoService.cs` | `IProdutoService` | `ProdutoService.cs` |
| `Core/Pedidos/PedidoConsultaService.cs` | `PedidoConsultaService` | `IPedidoConsultaService.cs` |
| `Core/Relatorios/RelatorioService.cs` | `RelatorioService` | `IRelatorioService.cs` |

Movimento de código (EST-03): `ErrosDoModelState` (público) e `NormalizarChave` (privado) saíram de `Api/Program.cs` e foram para `Api/Erros/ProblemasHttp.cs` (hoje `Api/Errors/`, `HL-35`). O `Program.cs` passou a chamar `ProblemasHttp.ErrosDoModelState`.

A pasta `IntegrationTests/Infra/` ficou vazia depois dos `git mv` e foi removida.

## 5. Namespaces e referências

- `Joya.Pedidos.Core.Common` → `Joya.Pedidos.Core.Health` para `IVerificadorDeBanco` (EST-02 e `HL-34`). Pela `HL-34`, `Joya.Pedidos.Api.Saude` e `Joya.Pedidos.Infrastructure.Saude` viraram `.Health`. O `using` foi ajustado em `Api/Program.cs`, `Api/Health/BancoDeDadosHealthCheck.cs`, `Infrastructure/Health/VerificadorDeBanco.cs` e `Infrastructure/DependencyInjection.cs`. Este último continua com `Core.Common` por causa do `IUnitOfWork`. Rotas (`/health/live`, `/health/ready`) e nomes de classe não mudaram.
- `Joya.Pedidos.IntegrationTests.Infra`, `.Api` (para os ajudantes) e `.Persistencia` (para `Arranjo`) → `Joya.Pedidos.IntegrationTests.Apoio`. Foram ajustados os `using` de 23 arquivos de teste, e os que deixaram de ser usados saíram.
- `RelatorioConsultasTests`: `.Relatorios` → `.Persistencia` (hoje `.Persistence`).
- **`HL-35`:** os namespaces seguem as pastas novas: `Api.Errors`, `Api.Limits`, `Infrastructure.Persistence(.Configurations)`, `IntegrationTests.Fixtures`, `.Database`, `.Database.RankingQuery`, `.Persistence`, `.Clientes`, `.Produtos`, `UnitTests.Fakes` e `UnitTests.Architecture`. A *theory* `GetCatalogo_AtivoInvalido_Retorna400` virou `GetClientes_AtivoInvalido_Retorna400` e `GetProdutos_AtivoInvalido_Retorna400`, com os mesmos casos; a rastreabilidade (ENU-L037) cita o segundo. Os caminhos `database/parte4` foram trocados no README, no agente `backend`, no `plan.md`, no backlog, no desenho, no QA, no `mcp-uso.md` e no roteiro do vídeo. O diário e as revisões ficam com os nomes antigos, porque registram o que existia na época.
- Frontend: `'../core/rotulo-cliente'` → `'./rotulo-cliente'` em `pedido-form.component.ts` e `pedido-lista.component.ts`. O import do spec é relativo e não mudou.
- **ARQ-01** (achado da re-revisão, severidade Baixa): `Apoio/Arranjo.cs` ficou com um `using` do próprio namespace, sobra da troca mecânica. O `backend` removeu a linha.
- Documentação com caminhos que ficaram desatualizados: `CLAUDE.md` (árvore, PROP-15), skill `/new-endpoint` (PROP-18) e `02-arquitetura.md` (PROP-16 e BE-28). O README não cita nenhum caminho movido (`grep` sem resultado) e por isso não foi alterado.

## 6. Decisões tomadas

| Decisão | Justificativa |
|---|---|
| EST-01 a EST-06 (`HL-33`) | Um conceito por arquivo, porta junto da feature, composição separada de lógica e ajudantes nomeados pelo papel. As fontes são a Microsoft (*Common web application architectures*, *Architectural principles*, *Develop ASP.NET Core MVC apps*), o style guide do Angular e as decisões do projeto. Detalhe no §2 da rodada 1 |
| PROP-15: árvore do `CLAUDE.md` igual à real | A árvore antiga omitia `Estoque/`, a pasta do health check, `Limites/`, `Apoio/` e a regra de organização dos testes de integração |
| PROP-16: portas e entidades no `02-arquitetura.md` | O desenho passou a listar `IPedidoConsultas`, `IRelatorioConsultas`, `IClienteRepository`, `IProdutoRepository` e as entidades reais |
| PROP-17: `IDE0130` como warning | O namespace igual à pasta deixa de depender de revisão. Foi comprovado que ele dispara na linha de comando: os arquivos movidos falharam com `error IDE0130` antes de o namespace ser trocado |
| `HL-34`: pasta `Health/` em vez de `Saude/` | *Health check* é infraestrutura técnica, não domínio de negócio. Pela convenção do `CLAUDE.md`, termo técnico fica em inglês. A pasta `Saude/` existia desde a US-05 (`b786353`); o EST-02 tinha seguido esse nome, e o desenvolvedor pediu a troca nas 3 camadas |
| `HL-35`: pastas técnicas em inglês | A regra de idioma da spec e do `CLAUDE.md` só falava de sufixos de classe, então os agentes traduziram as pastas técnicas (`Apoio`, `Banco`, `Erros`...). `Parte4` seguia a seção do enunciado, e não o conteúdo; `Catalogo` era um agrupamento sem par no Core. A regra passou a cobrir pastas e namespaces, e o BE-29 a verifica |
| PROP-18: skill `/new-endpoint` | Corrige o script de índices (`02-indices.sql`) e separa interface e implementação no exemplo, alinhado ao BE-28 |
| BE-28 no checklist | Comando `git grep` que lista arquivos de `backend/src` com mais de um tipo de topo. Sai vazio hoje e, contra o `HEAD` anterior, lista exatamente os 4 arquivos do EST-01 |

## 7. Decisões preservadas

A lista completa com o motivo de cada uma está no §3 da rodada 1. As principais:

- **Três projetos** (`Core`, `Infrastructure`, `Api`), sem `Application` separado (D1).
- **Pastas por feature** no `Core`, espelhadas no `Infrastructure`. Inclui `Estoque/`, que isola a baixa atômica (D7, regra de ouro 4).
- **Sem `Persistence/` e `Repositories/` por tipo** no `Infrastructure`, nem `Filters/` no `Api`. Separar por tipo quebraria o espelho por feature.
- **Nomes mistos PT/EN**, como manda a convenção do `CLAUDE.md`.
- **No frontend**, `core/api/` concentra o HTTP (ADR-0005), e não há invólucro `features/` enquanto existir só uma feature com telas.
- **Sem alteração** em `database/` (scripts e ordem), `e2e/`, `.github/`, compose, Dockerfiles e `nginx.conf`.
- **Documentos históricos** (`03-backend.md`, `04-frontend.md`, `06-qa.md`, `diario.md` e outros) mantêm os caminhos da época em que foram escritos.

## 8. Comandos executados (resultado real, rodados pelo orquestrador)

| Comando | Resultado |
|---|---|
| `dotnet build backend/Joya.Pedidos.sln` | 0 aviso, 0 erro (antes e depois do ARQ-01) |
| `dotnet format backend/Joya.Pedidos.sln --verify-no-changes` | Saída 0 (antes e depois do ARQ-01) |
| `dotnet test backend/tests/Joya.Pedidos.UnitTests` | 113/113, inclusive os 9 de arquitetura |
| `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "Categoria!=Medicao"` | 130/130 |
| `dotnet test ... --list-tests` | Unit 113 e Integration 131, iguais à linha de base de antes da refatoração |
| Busca por silenciamento (`(default\|null)!`, `!.`, `pragma warning`, `NoWarn`) | Nenhum resultado |
| BE-28 (`git grep` de tipos de topo por arquivo) | Vazio |
| `git grep CreatedAtAction -- backend/src` | Vazio |
| Namespaces antigos (`IntegrationTests.Infra`, `Core.Common` para `IVerificadorDeBanco`) | Nenhum resultado (re-revisão do `arquiteto`) |
| `git diff --stat -- database` | Vazio: scripts e ordem inalterados |
| `git diff --check` | Saída 0 |
| `npm --prefix frontend run lint` | Limpo |
| `npm --prefix frontend run build` | OK |
| `npm --prefix frontend test -- --watch=false --browsers=ChromeHeadless` | 34/34 |
| `cd e2e && npx tsc --noEmit` | Saída 0 |
| `docker compose config -q` | Saída 0 |
| `docker compose up -d --build --wait`, `curl` e `npm --prefix e2e test` | `db`, `api` e `frontend` healthy; `db-init` com saída 0; `/health/live` e `/health/ready` 200; `:4200/pedidos`, `:4200/api/clientes` e `:8080/swagger` 200; E2E 2/2. Sem `down -v` |
| `python3 .github/scripts/validar_rastreabilidade.py` | 0 erro |
| `python .github/scripts/validar_config.py` | Configuração válida |
| `bash .github/scripts/testar_hooks.sh` | Todos os casos passaram |
| Regate da `HL-35` (BE-29, namespaces antigos, build, unitários, integração, `--list-tests`, format, busca, BE-28, validadores, compose, E2E) | BE-29 e namespaces antigos vazios; 0 aviso; 113/113 e 130/130; contagem 113/131; `database/` só com comentários e prosa; validadores ok; compose healthy, as 5 rotas 200 (`/health/live`, `/health/ready`, `:4200/pedidos`, `:4200/api/clientes`, `/swagger`) e E2E 2/2 |

## 9. Gates não executados

| Gate | Motivo |
|---|---|
| `npm --prefix frontend ci` | Não rodado: `package.json` e `package-lock.json` não mudaram, e o `node_modules` instalado é o mesmo do último gate aprovado. O CI roda `npm ci` no PR |
| Integração com a categoria `Medicao` | Excluída por `HL-27`: mede tempo de consulta com carga grande e não depende de pastas ou namespaces |
| Comparação do contrato OpenAPI (BE-24) | `docs/contratos/openapi.json` não existe. A T-11.2 está fora do escopo (`HL-28`). As rotas não mudaram, e os controllers ficaram fora do diff |

## 10. Riscos residuais e melhorias futuras

**Riscos residuais**

- `IDE0130` passa a quebrar o build de quem criar um arquivo com namespace diferente da pasta. É o efeito desejado, mas é uma regra nova para quem contribui.
- O BE-28 é um `git grep` com regex: ele não pega um tipo declarado com modificadores fora da lista (por exemplo `file sealed record`). O risco é baixo, porque o comando foi validado contra o `HEAD` anterior.

**Fora do escopo** (§7 da rodada 1)

1. Gerenciamento central de pacotes (`Directory.Packages.props`).
2. Errata no `plan.md` para os nomes antigos dos scripts SQL e o `Directory.Packages.props`, que ele cita mas não existem.
3. T-11.2 (contrato OpenAPI exportado) e BE-24, ainda bloqueados pela `HL-28`.
4. Testes unitários para `ClienteService` e `ProdutoService`, hoje cobertos só pela integração.
5. Feature `clientes/` ou `produtos/` no frontend, caso surjam telas próprias.
