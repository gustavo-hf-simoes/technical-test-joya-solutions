# Plano de Implementação — Mini Gestão de Pedidos

| | |
|---|---|
| **Especificação** | [spec.md](spec.md) |
| **Convenções** | [CLAUDE.md](../CLAUDE.md) |
| **Origem deste plano** | Proposta gerada pelo Claude Code em sessão de planejamento, antes de qualquer código de implementação. As correções do desenvolvedor estão na [última seção](#ajustes-e-discordâncias-do-desenvolvedor-human-in-the-loop). |

Este documento tem duas partes com autores diferentes, e a separação é intencional:

1. **Proposta Inicial da IA** — o plano como o Claude o produziu. Não é reescrito depois; se uma decisão mudar, a mudança é registrada na parte 2.
2. **Ajustes e Discordâncias do Desenvolvedor** — o que foi alterado, rejeitado ou acrescentado por revisão humana, e por quê.

---

## Proposta Inicial da IA

### 1. Leitura dos critérios de avaliação

O plano foi dimensionado pelos pesos do enunciado, não pelo volume de código de cada parte.

| Critério | Peso | Como o plano responde | Evidência no repositório |
|---|---:|---|---|
| Uso estruturado do Claude (rules, hooks, skills, MCP) | **20%** | Configuração criada **antes** do código e usada em todas as fases: o hook valida cada edição, a skill gera os endpoints, o MCP inspeciona schema e planos de execução | `CLAUDE.md`, `.claude/`, `.mcp.json`, seção do README com exemplos reais de uso |
| Qualidade da spec e do plano (SDD) | **15%** | Spec com regras e critérios identificados (`RN-xx`, `CA-xx`) referenciados por testes e commits; plano com decisões, alternativas e discordâncias | `docs/spec.md`, `docs/plan.md` |
| Backend .NET e modelagem SQL | **15%** | Baixa de estoque atômica, transação única, índices justificados por consulta, logins de menor privilégio | `backend/`, `database/` |
| Testes unitários e de integração | **15%** | Unitários para regras e validações; integração contra SQL Server real (Testcontainers) aplicando os mesmos scripts de `database/`; teste de concorrência | `backend/tests/` |
| Debug e otimização da query | **10%** | Fase própria com medição antes/depois via MCP; a carga de dados é desenhada para **reproduzir** os defeitos | `database/parte4/`, README |
| Histórico de commits | **10%** | Um commit por tarefa do plano, Conventional Commits, sem squash; a tabela de cada fase já traz a mensagem prevista | `git log` |
| Docker | **5%** | Banco sobe já na Fase 1; compose completo com health checks e carga idempotente | `docker-compose.yml` |
| Frontend funcional | **5%** | Duas telas, sem biblioteca de estado; esforço deliberadamente contido | `frontend/` |
| README e senso crítico | **5%** | Alimentado ao longo do trabalho pela seção Human-in-the-Loop deste arquivo | `README.md` |
| Diferenciais | extra | Playwright (CA-30), GitHub Actions; deploy só se sobrar tempo | `e2e/`, `.github/workflows/` |

Duas consequências práticas: (a) **35% da nota vem de artefatos de processo** (SDD + uso do Claude), então eles são escritos primeiro e mantidos vivos; (b) o frontend vale o mesmo que o Docker — recebe o mínimo bem feito.

### 2. Decisões de arquitetura

| # | Decisão | Motivo | Alternativa descartada |
|---|---|---|---|
| D1 | **Clean Architecture enxuta em 3 projetos**: `Core` (domínio + casos de uso + portas), `Infrastructure` (EF Core), `Api` (HTTP) | A regra de dependência (tudo aponta para o `Core`) é preservada com o mínimo de cerimônia para 3 endpoints | 4 camadas (`Domain`/`Application` separados): projeto extra sem regra que o justifique. Projeto único: perde a fronteira que torna as regras testáveis sem banco |
| D2 | **Organização por feature** dentro do `Core` (`Pedidos/`, `Relatorios/`, `Clientes/`, `Produtos/`, `Common/`) | Tudo de um caso de uso fica junto (request, validator, service, erros) | Pastas por tipo técnico (`Services/`, `Validators/`, `Dtos/`) |
| D3 | **Services simples, sem MediatR e sem AutoMapper** | Ambos passaram a ter licença comercial; para 3 endpoints, um mediador é indireção pura. Mapeamento manual é explícito e verificado em compilação | CQRS com MediatR |
| D4 | **`Result<T>` para falhas de negócio**; exceções só para o inesperado | Falha de regra é fluxo normal: tipada, testável e mapeada para HTTP em um único lugar | Exceções de domínio capturadas por middleware |
| D5 | **Baixa de estoque por `UPDATE` condicional com `OUTPUT`** (spec 4.3) dentro de transação única | Verificação e baixa na mesma instrução; captura o preço no mesmo instante (RN-09); sem retry | Concorrência otimista com `rowversion` (exige retry, falha espúria em produto disputado); `SELECT … WITH (UPDLOCK)` (duas idas ao banco por item) |
| D6 | **EF Core 8 mapeando schema existente; sem migrations** | O enunciado exige scripts em `database/`. Duas fontes de schema divergem | Migrations como fonte e script gerado |
| D7 | **SQL bruto apenas na baixa de estoque** (`ExecuteSql`/`SqlQuery` parametrizado), isolado atrás de `IEstoqueRepository` | O EF não expõe `OUTPUT` em `ExecuteUpdate`; o restante é LINQ | Dapper para tudo: segunda ferramenta de acesso a dados sem necessidade |
| D8 | **Controllers** (não Minimal APIs) | Convenção pedida para o projeto; filtros e `ProducesResponseType` documentam o contrato no Swagger | Minimal APIs |
| D9 | **FluentValidation acionado por um filtro global próprio** | O pacote de auto-validação (`FluentValidation.AspNetCore`) está descontinuado; o filtro são ~30 linhas e resolve de forma assíncrona | Validação manual em cada action |
| D10 | **Testes de integração com Testcontainers + `WebApplicationFactory` + Respawn**, aplicando `database/*.sql` | Testa o schema real e os scripts reais; um contêiner por execução, reset rápido entre testes | Banco em memória/SQLite: não reproduz locks, `OUTPUT`, índices filtrados nem `DATETIME2` |
| D11 | **Shouldly + NSubstitute + xUnit** | FluentAssertions 8 e Moq têm ressalvas de licença/telemetria; evita ruído na avaliação | FluentAssertions, Moq |
| D12 | **Angular 19 standalone, Signals, `ChangeDetectionStrategy.OnPush`, Reactive Forms, Tailwind CSS** | Signals para estado de tela; `FormArray` tipado para os itens. Sem NgRx: duas telas | NgRx/SignalStore; `httpResource` e zoneless (experimentais na 19) |
| D13 | **Criação de pedido em rota própria** (`/pedidos/novo`), não em modal | Endereçável, foco e navegação por teclado triviais, E2E mais estável | Modal |
| D14 | **Nginx do frontend faz proxy de `/api` para a API** | Mesma origem: sem CORS no Docker e sem URL de API embutida no bundle | CORS aberto + URL absoluta em `environment.ts` |
| D15 | **Carga do banco por contêiner `db-init` efêmero** | A imagem oficial do SQL Server não executa scripts de inicialização; `depends_on: service_completed_successfully` encadeia a API sem `sleep` | Sobrescrever o entrypoint do SQL Server com script de espera |
| D16 | **MCP com login somente leitura (`claude_mcp`)** | No SQL Server, o modo read-only do DBHub é só um classificador de palavras-chave; a garantia real tem que estar no banco | Conectar o MCP como `sa` |

### 3. Estrutura do repositório

```text
.
├── CLAUDE.md                       # Convenções e comandos (carregado em toda sessão)
├── .mcp.json                       # Servidor MCP do SQL Server (escopo de projeto)
├── .claude/
│   ├── settings.json               # Hook + permissões
│   ├── hooks/post-edit.sh          # Build + testes unitários após edição no backend
│   ├── mcp/                        # Lançador e configuração do DBHub
│   └── skills/new-endpoint/SKILL.md
├── docs/{spec.md, plan.md}
├── database/
│   ├── 01_schema.sql               # Banco, tabelas, constraints, índices (idempotente)
│   ├── 02_seguranca.sql            # Logins joya_app e claude_mcp
│   ├── 03_carga.sql                # Dados de exemplo (idempotente)
│   ├── init.sh                     # Executado pelo contêiner db-init
│   └── parte4/                     # Query original, corrigida, índices e carga de volume
├── backend/
│   ├── Joya.Pedidos.sln
│   ├── Directory.Build.props       # net8.0, C# 12, nullable, warnings como erro
│   ├── Directory.Packages.props    # Versões centralizadas
│   ├── src/
│   │   ├── Joya.Pedidos.Core/
│   │   ├── Joya.Pedidos.Infrastructure/
│   │   └── Joya.Pedidos.Api/
│   └── tests/
│       ├── Joya.Pedidos.UnitTests/
│       └── Joya.Pedidos.IntegrationTests/
├── frontend/                       # Angular 19
├── e2e/                            # Playwright (diferencial)
├── .github/workflows/ci.yml        # (diferencial)
├── docker-compose.yml
└── README.md
```

### 4. Fases

Cada linha das tabelas é **um commit**. A coluna *Commit* traz a mensagem prevista (Conventional Commits; escopo = área). Uma fase só termina quando o critério de **Pronto** é verificado com o comando indicado — não quando o código "parece certo".

#### Fase 0 — Fundação do repositório

**Pré-requisitos da máquina** (verificado em 13/11/2026: o ambiente tem Node 24 e Git, mas **não tem .NET SDK nem Docker**): instalar .NET 8 SDK e Docker Desktop antes da Fase 1.

| # | Tarefa | Commit |
|---|---|---|
| 0.1 | `git init`, `.gitignore` (.NET, Node, `.env`, `.claude/settings.local.json`), `.gitattributes` com `*.sh text eol=lf` (scripts montados em contêiner Linux quebram com CRLF), `.editorconfig` | `chore: inicializa repositório com gitignore, gitattributes e editorconfig` |
| 0.2 | Especificação | `docs: adiciona especificação da feature (spec.md)` |
| 0.3 | Plano | `docs: adiciona plano de implementação proposto pela IA (plan.md)` |
| 0.4 | Convenções | `chore(claude): adiciona CLAUDE.md com convenções e comandos` |
| 0.5 | Hook | `chore(claude): adiciona hook de build e testes após edição no backend` |
| 0.6 | Skill | `chore(claude): adiciona skill new-endpoint` |
| 0.7 | MCP | `chore(claude): configura MCP do SQL Server (DBHub, login somente leitura)` |

**Pronto:** `git log --oneline` mostra 7 commits; `claude mcp list` enxerga o servidor `mssql`.

#### Fase 1 — Database

| # | Tarefa | Commit |
|---|---|---|
| 1.1 | `01_schema.sql`: banco `JoyaPedidos`, `READ_COMMITTED_SNAPSHOT ON`, tabelas, constraints e índices conforme spec 6. Idempotente (`IF DB_ID(...) IS NULL`, `IF OBJECT_ID(...) IS NULL`). `SET QUOTED_IDENTIFIER ON` explícito | `feat(db): cria schema com tabelas, constraints e índices` |
| 1.2 | `02_seguranca.sql`: logins/usuários `joya_app` (SELECT/INSERT/UPDATE) e `claude_mcp` (leitura + `SHOWPLAN` + `VIEW DATABASE STATE`); senhas por variável do `sqlcmd` (`-v`) | `feat(db): cria logins de menor privilégio para a API e para o MCP` |
| 1.3 | `03_carga.sql`: ~15 clientes (incluindo **2 homônimos**, **1 inativo**, **1 sem pedidos**), ~25 produtos (incluindo estoque 0, estoque 1 e 1 inativo), ~300 pedidos entre 2025 e 2027 com datas nas **fronteiras de ano**. Guardado por `IF NOT EXISTS (SELECT 1 FROM dbo.Clientes)` | `feat(db): adiciona carga de dados de exemplo` |
| 1.4 | `docker-compose.yml` apenas com `db` (health check via `sqlcmd`) e `db-init` (`sqlcmd -I -b -C`, executa os scripts em ordem). Senhas com valor padrão de desenvolvimento, sobrescrevíveis por `.env` | `feat(docker): sobe SQL Server com schema e carga automáticos` |
| 1.5 | **Verificação via MCP**: pedir ao Claude para listar tabelas, FKs e índices e conferir contra a spec 6; registrar o resultado no README | `docs(readme): registra inspeção do schema via MCP` |

**Pronto:** `docker compose up db db-init` termina com `db-init` em *exit 0*; executar de novo não falha nem duplica dados; pelo MCP, `search_objects` devolve as 4 tabelas e os 4 índices não clusterizados.

> O compose do banco nasce aqui, e não na fase de Docker, porque o MCP (20% da nota) e os testes de integração dependem de um SQL Server real desde o início.

#### Fase 2 — Backend Core (regras de negócio, sem I/O)

| # | Tarefa | Commit |
|---|---|---|
| 2.1 | Solução, 3 projetos + 2 de teste, `Directory.Build.props` (`net8.0`, `LangVersion 12`, `Nullable enable`, `TreatWarningsAsErrors`, `AnalysisLevel latest-recommended`), `Directory.Packages.props`, `global.json` | `chore(backend): cria solução com estrutura de projetos e build estrito` |
| 2.2 | `Common/`: `Result<T>`, `Error` (código, mensagem, tipo, metadados), `PaginaResponse<T>`, `Periodo` (converte datas inclusivas em intervalo semiaberto UTC — RN-15) + testes | `feat(core): adiciona Result, Error, paginação e Periodo` |
| 2.3 | Entidades `Cliente`, `Produto`, `Pedido`, `ItemPedido` (construtores que protegem invariantes; `Pedido.Criar` calcula `ValorTotal` — RN-11) + testes | `feat(core): modela entidades com invariantes do pedido` |
| 2.4 | `CriarPedidoRequest` + `CriarPedidoValidator` (RN-01…04) + testes de cada regra | `feat(core): valida estrutura do pedido (RN-01 a RN-04)` |
| 2.5 | Portas: `IClienteRepository`, `IProdutoRepository`, `IEstoqueRepository.TentarBaixarAsync`, `IPedidoRepository`, `IUnitOfWork.ExecutarEmTransacaoAsync` | `feat(core): define portas de persistência` |
| 2.6 | `PedidoService.CriarAsync`: ordem RN-05 → 06 → 07 → 08, itens em ordem de `ProdutoId`, coleta de todos os itens sem estoque, rollback em falha, preço vindo da baixa + testes com portas substituídas (CA-05…09, CA-11, CA-14) | `feat(core): implementa criação de pedido com regras de cliente, produto e estoque` |
| 2.7 | Idempotência (RN-14) no service + testes (CA-13) | `feat(core): torna criação de pedido idempotente por chave` |
| 2.8 | `ListarPedidosQuery` + validator, `TopClientesQuery` + validator, services de leitura delegando a portas de consulta + testes de validação | `feat(core): adiciona consultas de listagem e top clientes` |

**Pronto:** `dotnet build` sem warnings; `dotnet test backend/tests/Joya.Pedidos.UnitTests` verde; o `Core` não referencia EF Core, ASP.NET nem `Microsoft.Data.SqlClient`.

#### Fase 3 — Backend API (infraestrutura e HTTP)

| # | Tarefa | Commit |
|---|---|---|
| 3.1 | `PedidosDbContext` + configurações Fluent API espelhando o schema (PK composta, precisão decimal, conversor que marca `DateTime` como UTC) | `feat(infra): mapeia schema existente com EF Core` |
| 3.2 | `EstoqueRepository.TentarBaixarAsync` com `UPDATE … OUTPUT` parametrizado; `UnitOfWork` com transação explícita e **sem** *retry* automático do EF (`HL-23`, [ADR-0004](adr/0004-unidade-de-trabalho-sem-retentativa.md)) | `feat(infra): implementa baixa atômica de estoque e unidade de trabalho` |
| 3.3 | Repositórios de escrita e consultas de leitura com projeção direta para DTO (`AsNoTracking`, sem N+1); tratamento da violação de `UX_Pedidos_ChaveIdempotencia` (erros 2601/2627) como reapresentação | `feat(infra): implementa repositórios e consultas` |
| 3.4 | Host: DI, `ValidationFilter`, `IExceptionHandler` → Problem Details, mapeamento `Error` → HTTP em um único ponto, JSON `camelCase`, health checks, Swagger | `feat(api): configura pipeline, validação e tratamento de erros` |
| 3.5 | `PedidosController` (`POST`, `GET`, `GET {id}`) — **gerado com a skill `new-endpoint`** | `feat(api): expõe POST /pedidos, GET /pedidos e GET /pedidos/{id}` |
| 3.6 | `RelatoriosController` (`GET top-clientes`) — skill | `feat(api): expõe GET /relatorios/top-clientes` |
| 3.7 | `ClientesController`, `ProdutosController` — skill | `feat(api): expõe GET /clientes e GET /produtos` |

**Pronto:** com `docker compose up db db-init` no ar, `dotnet run --project backend/src/Joya.Pedidos.Api` responde `201` a um `POST /pedidos` válido pelo Swagger, e o MCP confirma a linha em `Pedidos`, as linhas em `ItensPedido` e o estoque debitado.

#### Fase 4 — Frontend Angular

| # | Tarefa | Commit |
|---|---|---|
| 4.1 | `ng new` (standalone, SCSS desligado, rotas), Tailwind, ESLint (`angular-eslint`), `strict` no TypeScript, proxy de desenvolvimento `/api` → `localhost:8080` | `chore(frontend): cria aplicação Angular com Tailwind e lint` |
| 4.2 | Modelos tipados espelhando a spec 5, `PedidosApi`/`ClientesApi`/`ProdutosApi`, interceptor que converte Problem Details em erro tipado | `feat(frontend): adiciona camada de acesso à API` |
| 4.3 | Tela de listagem: filtros sincronizados com a query string, tabela, paginação, estados de carregamento/vazio/erro (CA-28, CA-29) | `feat(frontend): adiciona listagem de pedidos com filtros e paginação` |
| 4.4 | Tela de novo pedido: `FormArray` tipado, total estimado por `computed`, validação (CA-31), tratamento de `409`/`422` por item (CA-32), `Idempotency-Key` gerada uma vez por formulário | `feat(frontend): adiciona criação de pedido` |
| 4.5 | Acessibilidade e acabamento: rótulos, foco no primeiro erro, `aria-live` nas mensagens, formatação `pt-BR` de moeda e data | `feat(frontend): melhora acessibilidade e formatação` |

**Pronto:** `npm run lint` e `npm run build` sem erros; fluxo CA-30 executado manualmente no navegador.

#### Fase 5 — Testes

Os testes unitários já nasceram junto com o código da Fase 2. Esta fase entrega o que exige infraestrutura.

| # | Tarefa | Commit |
|---|---|---|
| 5.1 | Fixture: contêiner SQL Server (Testcontainers), aplicação de `database/01_schema.sql`, `WebApplicationFactory` apontando para ele, Respawn entre testes, *builders* de dados | `test(integracao): adiciona fixture com SQL Server real` |
| 5.2 | `POST /pedidos`: CA-01, 02, 05, 06, 07, 08, 09, 11, 13 | `test(integracao): cobre criação de pedido` |
| 5.3 | **Concorrência (CA-10)**: 20 requisições paralelas, estoque 5 → exatamente 5 × `201`, 15 × `409`, estoque final 0 | `test(integracao): prova que o estoque não fica negativo sob concorrência` |
| 5.4 | `GET /pedidos`: CA-15…20 (inclui fronteiras do período) | `test(integracao): cobre listagem com filtros e paginação` |
| 5.5 | `GET /relatorios/top-clientes`: CA-21…27 (homônimos, empate, período) | `test(integracao): cobre relatório de top clientes` |
| 5.6 | Teste de arquitetura: `Core` não depende de `Infrastructure`/`Api` | `test: garante a regra de dependência entre camadas` |

**Pronto:** `dotnet test backend/Joya.Pedidos.sln` verde em máquina limpa com Docker; o teste 5.3 **falha** se a baixa atômica for trocada por *check-then-act* (verificar uma vez, de propósito, e registrar no README).

#### Fase 6 — Docker

| # | Tarefa | Commit |
|---|---|---|
| 6.1 | `backend/Dockerfile` multi-stage (`sdk:8.0` → `aspnet:8.0`), usuário não root, restore em camada própria | `feat(docker): adiciona imagem da API` |
| 6.2 | `frontend/Dockerfile` multi-stage (Node → Nginx sem root), `nginx.conf` com fallback de SPA e proxy `/api` | `feat(docker): adiciona imagem do frontend com proxy para a API` |
| 6.3 | Compose completo: `api` depende de `db-init` concluído; `web` depende de `api` saudável; health checks; `.env.example` | `feat(docker): orquestra banco, API e frontend em um comando` |

**Pronto:** em clone limpo, `docker compose up --build` deixa a aplicação utilizável em `http://localhost:4200` sem nenhum passo manual; `docker compose down && docker compose up` funciona; `docker compose down -v && docker compose up` recria tudo.

#### Fase 7 — Parte 4: debug da query

Hipóteses levantadas na leitura da query (a confirmar com medição — nenhuma delas vai para o README sem evidência):

| # | Hipótese | Tipo | Como confirmar |
|---|---|---|---|
| H1 | `GROUP BY c.Nome` soma clientes homônimos como se fossem um | **Resultado incorreto** | Carga com dois "João da Silva": a query original devolve uma linha com a soma |
| H2 | `ORDER BY Total` ordena de forma crescente: um "ranking" começa pelo pior cliente | **Resultado incorreto** | Resultado visível |
| H3 | `WHERE YEAR(p.DataPedido) = 2026` sobre tabela do lado opcional de um `LEFT JOIN` descarta as linhas `NULL`: os `LEFT JOIN` viram `INNER JOIN` disfarçados. A intenção fica ambígua e o otimizador perde opções | Correção/clareza | Comparar planos com `INNER JOIN` explícito |
| H4 | `YEAR(coluna)` não é sargável: impede *seek* em índice de `DataPedido` e força a leitura de todos os pedidos | **Lentidão** | Plano: *scan* → *seek* ao trocar por intervalo semiaberto |
| H5 | Sem índices de apoio (FK não gera índice no SQL Server): *scans* e *hash joins* sobre as tabelas inteiras | **Lentidão** | `logical reads` antes/depois |
| H6 | Junta `Clientes` antes de agregar; agregar por `ClienteId` e só então buscar o nome reduz as linhas que passam pelo join | Lentidão (menor) | Comparar planos |

| # | Tarefa | Commit |
|---|---|---|
| 7.1 | `database/parte4/00_carga_volume.sql`: gera volume (ordem de 200 mil pedidos / 600 mil itens) para que a diferença de plano apareça. **Fora** do `db-init` | `chore(db): adiciona carga de volume para análise da Parte 4` |
| 7.2 | `01_query_original.sql` + medição de base via MCP: plano, leituras lógicas, tempo (Query Store / `sys.dm_exec_query_stats`) | `docs(parte4): registra medição da query original` |
| 7.3 | `02_query_corrigida.sql`: intervalo semiaberto, `INNER JOIN`, `GROUP BY` por `Id`, `ORDER BY Total DESC`, agregação antes do join | `fix(parte4): corrige resultado da query de ranking` |
| 7.4 | `03_indices.sql` (se o schema da Fase 1 ainda não cobrir) + nova medição | `perf(parte4): otimiza query de ranking com índices de cobertura` |
| 7.5 | README: o que estava errado, como foi investigado, tabela antes/depois | `docs(readme): explica o debug da query da Parte 4` |

**Pronto:** o README traz números medidos (não estimados) de antes e depois, e a query corrigida devolve os homônimos em linhas separadas.

#### Fase 8 — Diferenciais

| # | Tarefa | Commit |
|---|---|---|
| 8.1 | Playwright: CA-30 (criar pedido e vê-lo na listagem) contra o compose | `test(e2e): cobre criação de pedido e listagem com Playwright` |
| 8.2 | GitHub Actions: build + lint + testes unitários + integração (o runner já tem Docker) + build do frontend | `ci: adiciona pipeline de build e testes` |
| 8.3 | Deploy online — **somente se as fases 0–7 e 9 estiverem concluídas** | `chore: publica aplicação` |

#### Fase 9 — README e entrega

| # | Tarefa | Commit |
|---|---|---|
| 9.1 | Como rodar com Docker e como rodar os testes | `docs(readme): documenta execução e testes` |
| 9.2 | Decisões técnicas (resumo de D1–D16 com o que mudou na revisão humana) | `docs(readme): registra decisões técnicas` |
| 9.3 | Uso do Claude: rules, hook, skill, MCP — com exemplos reais; o que funcionou e **o que precisou ser corrigido** (derivado da seção Human-in-the-Loop) | `docs(readme): relata o uso do Claude Code` |
| 9.4 | Revisão final contra o enunciado (lista de requisitos obrigatórios) e gravação do vídeo | — |

### 5. Riscos e mitigação

| Risco | Probabilidade | Mitigação |
|---|---|---|
| Índice filtrado falha na criação ou em `INSERT` porque o `sqlcmd` conecta com `QUOTED_IDENTIFIER OFF` | Alta se ignorado | `sqlcmd -I` no `db-init` **e** `SET QUOTED_IDENTIFIER ON` nos scripts |
| `db-init` roda antes de o SQL Server aceitar conexões | Alta | Health check real (`SELECT 1`) + `depends_on: condition: service_healthy` |
| Scripts `.sh` com CRLF (Windows) quebram no contêiner Linux | Alta nesta máquina | `.gitattributes` com `eol=lf` desde o commit 0.1 |
| EF devolve `DateTime` com `Kind=Unspecified` e o JSON sai sem `Z` | Média | Conversor de valor UTC no `DbContext` + asserção no teste CA-01 |
| Transação manual lança exceção com `EnableRetryOnFailure` | Média | Eliminado: o *retry* automático não é ligado (`HL-23`, [ADR-0004](adr/0004-unidade-de-trabalho-sem-retentativa.md)); reenvio seguro pela `Idempotency-Key` |
| Deadlock entre pedidos com os mesmos produtos | Baixa | Baixa em ordem de `ProdutoId` (spec 4.3) |
| Teste de concorrência intermitente | Média | Disparo simultâneo com barreira (`TaskCompletionSource`); asserção sobre o **invariante** (contagens e estoque final), não sobre a ordem |
| Testcontainers lento ou sem Docker | Média | Um contêiner por *collection*; Respawn em vez de recriar; CI cobre quem não tem Docker local |
| Hook torna a edição lenta | Média | Só dispara para arquivos do backend; build incremental; só testes unitários; desligável com `SKIP_POST_EDIT_HOOK=1` |
| `explain_sql` do DBHub não devolver plano útil no SQL Server | A verificar na Fase 1 | Alternativa por `SELECT` puro: Query Store e `sys.dm_exec_query_stats` + `sys.dm_exec_query_plan` |

### 6. Pontos em que a IA recomenda revisão humana

Decisões em que a proposta fez uma escolha defensável, mas em que existe uma alternativa razoável. São os candidatos naturais à seção seguinte.

1. **`ValorTotal` desnormalizado em `Pedidos`** (spec 6.4). Simplifica e acelera as leituras, mas duplica informação; a consistência é garantida por imutabilidade e pelo teste CA-27.
2. **PK composta em `ItensPedido`** em vez de `Id` substituto. Mais enxuta, porém menos convencional.
3. **`422` para cliente inexistente** (e não `404`) e **`409` para estoque**.
4. **Idempotência (RN-14)**: é o item de maior custo fora do enunciado. Candidata a corte se o prazo apertar — é um commit isolado (2.7) justamente por isso.
5. **UTC nos filtros de período** (premissa P1 da spec): para um usuário no Brasil, a virada do dia acontece às 21h.
6. **`Produtos.Ativo`**: regra acrescentada por simetria com cliente ativo; não está no enunciado.
7. **Ordem das fases**: banco no Docker já na Fase 1, e testes unitários junto com o código (em vez de uma fase única de testes ao final).
8. **Mensagens de commit em português** com tipos do Conventional Commits em inglês.

---

## Ajustes e Discordâncias do Desenvolvedor (Human-in-the-Loop)

> [!IMPORTANT]
> **Espaço reservado para o desenvolvedor.** Tudo acima é a proposta da IA e permanece como foi gerada. Esta seção registra onde o julgamento humano mudou o rumo — é a evidência de senso crítico pedida no enunciado ("o que você ajustou ou discordou dele e por quê") e a fonte da seção "o que precisei corrigir" do README.
>
> Registre **no momento em que a decisão acontece**, não ao final. Uma entrada por decisão.

### Como registrar

| Campo | Conteúdo |
|---|---|
| **ID** | `HL-01`, `HL-02`, … |
| **Tipo** | `Discordância` (rejeitei a proposta) · `Ajuste` (mantive a ideia, mudei a forma) · `Correção` (a IA errou) · `Acréscimo` (a IA não previu) |
| **Referência** | Decisão (`D5`), fase/tarefa (`3.2`), regra (`RN-08`) ou arquivo |
| **O que a IA propôs** | Em uma frase |
| **O que foi feito** | Em uma frase |
| **Por quê** | O argumento técnico — o que a IA não considerou ou pesou errado |
| **Evidência** | Hash do commit, teste, medição |

### Registro

| ID | Tipo | Referência | O que a IA propôs | O que foi feito | Por quê | Evidência |
|---|---|---|---|---|---|---|
| HL-01 | Discordância | §4 (fases 0–9); todo o processo | Executar as fases 0 a 9 em sequência, numa única sessão que escreve requisitos, código, testes e revisão | Esteira SDD por etapas ([docs/esteira/README.md](esteira/README.md)): PO → arquitetura → backend → frontend → segurança → QA, com um agente por papel, artefato e gate por etapa, backlog épico → histórias → tarefas e achados devolvidos ao dono com re-revisão | Uma sessão única revisa o próprio trabalho, e ninguém confere o produto contra o enunciado linha a linha. Com papéis separados, quem revisa não corrige e quem corrige não se aprova. As fases do §4 continuam como referência técnica e o backlog do PO as substitui como unidade de execução | `.claude/agents/`, `.claude/skills/esteira/`, `docs/esteira/` |
| HL-02 | Acréscimo | Configuração `.claude/` | Criar só a skill própria `/new-endpoint` | Antes de criar skills genéricas, pesquisar e vendorizar skills **oficiais** (dotnet, testcontainers, angular, getsentry), auditadas e fixadas por commit, com um bloco de adaptação local; acrescentar hooks `guard-paths.sh` e `guard-bash.sh`, que limitam o que cada agente escreve e executa | Reutilizar conhecimento mantido por quem publica a ferramenta é melhor que reescrever. Sem auditoria, porém, uma skill de terceiros é código não confiável com acesso ao repositório. Os limites dos agentes precisam de enforcement, não só de prompt | [.claude/skills/VENDORED.md](../.claude/skills/VENDORED.md) |
| HL-03 | Ajuste | Escopo (spec §2.2) | Autenticação fora do escopo | Mantido fora do escopo, por não ser pedida pelo enunciado; o revisor de segurança a registra como **risco aceito**, com a recomendação para produção, e o README a declara | O desenvolvedor questionou se era exigida. O enunciado não pede, e implementá-la gastaria tempo em algo que não pontua | `docs/requisitos/enunciado.md` |
| HL-04 | Discordância | §1 (linha "Diferenciais"); fase 8 (8.1–8.3) | Playwright e GitHub Actions no fim (fase 8); deploy "só se sobrar tempo", depois das fases 0–7 e 9 | Os três diferenciais (ENU-L085 a L087) entram como **compromisso**: o CI vem logo depois do esqueleto da solução e roda em todo push e PR desde o início; o deploy é um esqueleto publicado cedo e atualizado por CD; o Playwright (CA-30) fecha a história do frontend. Deploy numa VM Linux (≥2 GB, x86-64) com o mesmo `docker-compose.yml` mais Caddy para HTTPS | Os diferenciais contam pontos extras, e deixá-los para o fim é a forma mais comum de perdê-los. CI no começo também gera evidência no histórico de commits (peso 10%). O SQL Server 2022 exige ≥2 GB e x86-64, o que exclui os planos gratuitos pequenos. A VM roda o mesmo artefato do ambiente local, o que reforça "sobe com um comando" | `docs/requisitos/enunciado.md` (L085–L087) |
| HL-05 | Acréscimo | Esteira; todos os agentes | Todos os subagentes com `model: inherit`, ou seja, tudo no modelo da sessão (Opus 5.5) | Roteamento de modelo e esforço por agente e por complexidade da tarefa (`C1` Haiku, `C2` Sonnet, `C3` Opus), escalonamento após 2 falhas de gate, novo agente `verificador` (Haiku) para rodar gates, e registro de cada execução no `status.md` | Trabalho mecânico não precisa do modelo mais caro: o Haiku 5.5 custa 1/40 do Opus 5.5. O gate automático (zero warnings + testes) torna barato detectar o erro do modelo mais simples. Fable 5.2 fica só como último degrau | [docs/adr/0001-roteamento-de-modelos.md](adr/0001-roteamento-de-modelos.md) |
| HL-06 | Acréscimo | §4 (fase 0); histórico de commits | Um commit por tarefa a partir da fase 0, sem tratar os arquivos criados antes do `git init` | Bootstrap em ~12 commits lógicos na ordem real de criação, com datas reais e sem retroagir; depois, uma branch por história, um commit por tarefa e PR com merge commit (nunca squash), com o CI rodando em cada PR | A spec, o plano e a configuração `.claude/` nasceram antes do repositório. Um commit único com tudo violaria ENU-L081. Retroagir datas seria reescrita de histórico disfarçada. A branch por história deixa a evolução legível e prova o CI em cada PR | `docs/requisitos/enunciado.md` (L081) |
| HL-07 | Acréscimo | Processo; README "Como usei o Claude" | Evidência do uso da IA apenas pelos commits, pelo `plan.md` e pelo `status.md` | Diário de bordo cronológico em [docs/ia/](ia/diario.md): uma entrada por interação, com pedido, saída, HL/CG e modelo; prompts na íntegra extraídos do log da sessão; roteiro do vídeo de 10 min. O orquestrador atualiza o diário em cada passo da esteira | A conversa com a IA não aparece no git, e parte da avaliação (20% uso do Claude, 5% senso crítico, vídeo) depende dela. Um registro datado deixa auditável o que seria subjetivo | `docs/requisitos/enunciado.md` (L082) |
| HL-08 | Acréscimo | §4 (fase 8); CI; README | Diferenciais limitados aos três do enunciado (Playwright, CI e deploy) | Quatro diferenciais a mais, escolhidos pelo desenvolvedor entre os propostos pela IA. **(1) Segurança da cadeia:** `main` protegida (PR obrigatório, CI verde, sem force-push), Dependabot, CodeQL e *secret scanning* com *push protection*. **(2) Matriz de rastreabilidade** `ENU → requisito → US → tarefa → teste → commit` em [rastreabilidade.md](esteira/rastreabilidade.md), validada no CI: o build falha se uma linha do enunciado ficar sem classificação, se um requisito obrigatório ficar sem teste ou se um teste ou commit citado não existir. **(3) Testes de arquitetura** que transformam as regras do `CLAUDE.md` em testes que falham (ex.: o `Core` não referencia EF nem ASP.NET; DTOs são `sealed`; controllers não acessam o `DbContext`). Biblioteca: **ArchUnitNET** (`TngTech.ArchUnitNET`, Apache-2.0), e não NetArchTest, que a IA tinha sugerido. **(4) Relatório de custo medido** no README, montado a partir da tabela *Execuções de agentes* do `status.md`: tokens por delegação, lidos do resultado de cada subagente, e custo pelos preços do [ADR-0001](adr/0001-roteamento-de-modelos.md) | (1) Num repositório público, a postura de segurança é verificável pelo avaliador, sem custo. (2) Transforma "o SDD cobre o enunciado" de afirmação em verificação automática. É a resposta objetiva ao peso de 10% de especificação. (3) Responde "como você garante que a IA segue as regras?" com um teste que falha, e não com um prompt. É a única dependência nova (regra de ouro 12) e só entra no projeto de testes. Ao conferir no NuGet antes de adotar, o NetArchTest.Rules teve a última versão em 2021-05-23 (1.3.2). O ArchUnitNET está ativo: 0.13.5 saiu em 2026-10-26. (4) O roteamento de modelos (`HL-05`) só convence com números medidos | NuGet (`netarchtest.rules`, `tngtech.archunitnet.xunit`), consultado em 2026-11-13; [ci.yml](../.github/workflows/ci.yml) |
| HL-09 | Ajuste | `product-owner` (frontmatter); [ADR-0001](adr/0001-roteamento-de-modelos.md) §1; delegação da etapa 1 | PO no Opus com esforço `high`, que lê o que quiser e entrega os três artefatos de uma vez | PO no Opus com esforço `medium`. A delegação passa uma lista fechada de entradas e pede a escrita incremental: `01-requisitos.md`, depois `backlog.md`, depois a matriz, cada um gravado antes do próximo | Na primeira delegação, de 13 min, o PO leu 25 arquivos em 70 s, inclusive agentes de outros papéis, a skill `/new-endpoint` e `.git/logs/HEAD`. Depois raciocinou 3,5 min (23.018 tokens de saída) sem gravar nada e entrou em outra geração de cerca de 8 min, perdida quando o desenvolvedor parou o agente. O Opus continua, porque um requisito perdido se propaga. O conteúdo difícil já está na spec e nos `HL`, e a etapa 1 é de classificar e decompor. Com a escrita incremental, uma interrupção não joga fora todo o trabalho | Transcrição do subagente (2026-11-13 21:01–21:14 BRT); [diário E06](ia/diario.md) |
| HL-10 | Acréscimo | PROP-01; spec §8.3 (CA-33), §10 (P7) | A spec ligava a Parte 4 só ao CA-22 e ao CA-23, sem critério para a query corrigida | CA-33: a query corrigida devolve o ranking completo de 2026, uma linha por `ClienteId`, em ordem decrescente com desempate por `Id`, sem clientes sem compra; os 5 primeiros batem com o endpoint e o plano não tem *scan* nem função sobre `DataPedido`. Premissa P7 | O enunciado não diz quantas linhas a query devolve, e a original não tem `TOP`. Sem critério, o QA não tem como aceitar a Parte 4 (peso 10%) | [01-requisitos.md](esteira/01-requisitos.md) §7; decisão por `AskUserQuestion` em 2026-11-13 |
| HL-11 | Acréscimo | PROP-02; spec §8.5 (CA-34, CA-35, CA-36, CA-38) | `GET /pedidos/{id}`, `/clientes`, `/produtos`, `/health/*`, `415` e `500` com contrato, mas sem critério de aceite | Novos CA-34 a CA-36 e CA-38 | Sem critério, o teste dessas rotas não tem nome rastreável na matriz | [01-requisitos.md](esteira/01-requisitos.md) §7; decisão por `AskUserQuestion` em 2026-11-13 |
| HL-12 | Acréscimo | PROP-03; spec RNF-06, §5.1 | RNF-06 sem limite contra abuso | Limite de tamanho do corpo (`413`) e *rate limiter* nativo do ASP.NET Core no `POST /pedidos` (`429 LIMITE_DE_TAXA`), sem dependência nova. O valor do limite de corpo fica para a arquitetura. O código `LIMITE_DE_TAXA` foi nomeado pelo orquestrador ao aplicar a PROP | A API fica pública no deploy (ENU-L087) e sem autenticação (`HL-03`). Os limites de itens não impedem corpos enormes nem rajadas | [01-requisitos.md](esteira/01-requisitos.md) §7; decisão por `AskUserQuestion` em 2026-11-13 |
| HL-13 | Acréscimo | PROP-04; spec RNF-08, §8.4 | Acessibilidade e formatação `pt-BR` só no `CLAUDE.md` | RNF-08 na spec, citada nos CA-28 a CA-32 | Requisito de interface tem de estar na fonte de verdade técnica, onde o QA o verifica, e não só nas convenções de código | [01-requisitos.md](esteira/01-requisitos.md) §7; decisão por `AskUserQuestion` em 2026-11-13 |
| HL-14 | Ajuste | PROP-05; spec RN-14, CA-13, CA-37 | RN-14 sem tratamento da corrida entre duas requisições com a mesma chave; CA-13 com a chave `"K"`, que não é UUID | Ao violar `UX_Pedidos_ChaveIdempotencia`, a transação é desfeita e a API devolve `200` com o pedido original e `Idempotent-Replayed: true`. CA-13 com UUID; CA-37 para chave inválida (`400`) e para a corrida (um `201`, um `200`, uma baixa) | Sem isso, a corrida devolve `500` ou debita o estoque duas vezes, justamente o que a regra quer evitar | [01-requisitos.md](esteira/01-requisitos.md) §7; decisão por `AskUserQuestion` em 2026-11-13 |
| HL-15 | Ajuste | PROP-06; spec RNF-04; `CLAUDE.md` | Spec com `docker compose up` e `CLAUDE.md` com `docker compose up --build` | Num clone limpo, `docker compose up` sem flags sobe tudo; `--build` só reconstrói. O README usa o mesmo texto | ENU-L066 pede um único comando, e o avaliador vai digitar o do exemplo (L068). Dois comandos diferentes parecem passo manual | [01-requisitos.md](esteira/01-requisitos.md) §7; decisão por `AskUserQuestion` em 2026-11-13 |
| HL-16 | Ajuste | PROP-07; spec §9 | Duas tabelas de rastreabilidade: a §9 da spec e a matriz do CI | A §9 continua como resumo e aponta [rastreabilidade.md](esteira/rastreabilidade.md) como a matriz oficial | Evita duas fontes divergentes; a do CI é a que falha o build | [01-requisitos.md](esteira/01-requisitos.md) §7; decisão por `AskUserQuestion` em 2026-11-13 |
| HL-17 | Ajuste | PROP-08; spec RNF-02 | RNF-02 exige *seek* e nenhum *scan* de `Pedidos` em toda listagem | *Seek* só na listagem com filtro e no ranking; sem filtro, *scan* ordenado de `IX_Pedidos_DataPedido` limitado por `TOP`, e a contagem lê o índice mais estreito | O `GET /pedidos` sem filtro é permitido (RN-16), e contar todas as linhas exige ler um índice inteiro. Do jeito antigo, o QA reprovaria um comportamento correto | [02-arquitetura.md](esteira/02-arquitetura.md) §Desvios; decisão por `AskUserQuestion` em 2026-11-13 |
| HL-18 | Acréscimo | PROP-09; spec §5.5 | `ativo=true` nos exemplos, sem dizer o que acontece sem o parâmetro | `ativo` opcional; ausente traz todos; valor não booleano → `400 VALIDACAO` | Sem a regra, o `backend` e o QA decidiriam cada um por si | [02-arquitetura.md](esteira/02-arquitetura.md) §Desvios; decisão por `AskUserQuestion` em 2026-11-13 |
| HL-19 | Acréscimo | PROP-10; spec RNF-06; [ADR-0002](adr/0002-limites-contra-abuso.md) | Limite de taxa sem relação com os testes de concorrência | Testes de integração com o limite elevado por configuração; um teste próprio prova o `429` com limite baixo | Com 10 `POST`s a cada 10 s por IP, o CA-10 (20 `POST`s simultâneos) receberia `429` | [02-arquitetura.md](esteira/02-arquitetura.md) §Desvios; decisão por `AskUserQuestion` em 2026-11-13 |
| HL-20 | Ajuste | PROP-11; spec RN-13 | `DataPedido` com a precisão do relógio | Truncada em milissegundos antes de gravar | A coluna é `DATETIME2(3)`. Sem truncar, o `201` e o `GET /pedidos/{id}` mostrariam instantes diferentes | [02-arquitetura.md](esteira/02-arquitetura.md) §Desvios; decisão por `AskUserQuestion` em 2026-11-13 |
| HL-21 | Acréscimo | PROP-12; spec §5.1; [ADR-0005](adr/0005-contrato-openapi-versionado.md) | Swagger só em Development e Docker, sem contrato versionado | Swagger também no ambiente de teste; `docs/contratos/openapi.json` versionado e conferido por teste no backend e no frontend | O teste de contrato precisa do Swagger no ambiente `Testing`, e o frontend passa a ter os modelos conferidos sem dependência nova | [02-arquitetura.md](esteira/02-arquitetura.md) §Desvios; decisão por `AskUserQuestion` em 2026-11-13 |
| HL-22 | Acréscimo | PROP-13; spec §8.4 | A spec não dizia se a tela envia `Idempotency-Key` | A tela gera a chave ao abrir o formulário e a reutiliza nos reenvios | Sem *retry* automático (`HL-23`), a proteção contra pedido duplicado por duplo clique ou reenvio depois de erro de rede depende da tela | [02-arquitetura.md](esteira/02-arquitetura.md) §Desvios; decisão por `AskUserQuestion` em 2026-11-13 |
| HL-23 | Discordância | [ADR-0004](adr/0004-unidade-de-trabalho-sem-retentativa.md); plano, tarefa 3.2 e tabela de riscos | `UnitOfWork` com `CreateExecutionStrategy()` para conviver com `EnableRetryOnFailure` | Sem *retry* automático do EF; transação explícita atrás de `IUnitOfWork.ExecutarEmTransacaoAsync`; um `Result` de falha faz *rollback*. Os ajustes que o arquiteto sugeriu no backlog foram delegados ao `product-owner` | Repetir a transação inteira acrescenta código e risco de baixa duplicada sem ganho real num banco local; o D5 já previa "sem retry", e a idempotência cobre o reenvio | [02-arquitetura.md](esteira/02-arquitetura.md) §Desvios; decisão por `AskUserQuestion` em 2026-11-13 |
| HL-24 | Exceção | Ambiente (`~/.claude/settings.json`); [ADR-0001](adr/0001-roteamento-de-modelos.md); skill `/esteira`, passos 2 e 5 | Os aliases `haiku`/`sonnet`/`opus` passavam por um *gateway* local (`localhost:20128`) que os levava a um único modelo; uma delegação e um registro em `status.md`, diário e prompts a cada tarefa | O orquestrador removeu, com aval explícito do desenvolvedor, as substituições de modelo e o *gateway* da configuração global (cópia em `settings.json.bak-gateway-20261113`). Nas histórias com tarefas C1/C2 pequenas, uma delegação por história e um registro por história, com um commit por tarefa mantido | O *gateway* fora do ar derrubou a T-02.1 duas vezes (`model_not_found`), e com ele no ar o ADR-0001 virava só rótulo, com o mesmo modelo para C2 e C3. A cerimônia por tarefa custava mais que o esqueleto que ela entrega | Decisão do desenvolvedor no chat em 2026-11-13, tratada como exceção com os riscos aceitos |

### Correções em código gerado

> Para erros concretos da IA encontrados durante a implementação (bug, API inexistente, teste que não testava nada, simplificação indevida). Inclua como foi detectado: hook, teste, revisão, MCP.

| ID | Arquivo | Problema no código gerado | Como foi detectado | Correção | Commit |
|---|---|---|---|---|---|
| CG-01 | Análise da esteira (conversa) | A IA afirmou que subagentes do Claude Code não podem criar subagentes, e usou isso como motivo para o orquestrador ser a sessão principal | Consulta à documentação oficial (`code.claude.com/docs/en/sub-agents`) antes de escrever os agentes | A documentação permite aninhar até 3 níveis. O motivo correto é que subagentes não têm `AskUserQuestion`, que os gates humanos exigem. A decisão foi mantida com o argumento corrigido | — (antes do primeiro commit) |
| CG-02 | Diagnóstico de ambiente (conversa e `docs/esteira/status.md`) | A IA afirmou que o git não estava instalado. A verificação de ambiente tinha devolvido `git version 2.55.0.windows.5`; a IA confundiu "a pasta ainda não é um repositório" com "git ausente" | O desenvolvedor contestou. A releitura do log da sessão mostrou a saída original | Ambiente reverificado com um comando citado por linha (ver [diário, E04](ia/diario.md)). Nova regra: toda afirmação sobre o ambiente cita a saída que a sustenta | bootstrap |
| CG-03 | `backend/tests/Joya.Pedidos.UnitTests/Common/ResultTests.cs` | O `backend` usou `default!` (*null-forgiving*) num teste, contra a regra de ouro 1; o build, os testes e o `dotnet format` passavam | Revisão do orquestrador no gate da US-02 (busca por `!` no item BE-07 do checklist) | Devolvido ao `backend` em CORRECAO: `Should.NotThrow(Func<T>)` devolve o resultado, sem a variável pré-inicializada | 3d2ebe9 |
| CG-04 | | | | | |
