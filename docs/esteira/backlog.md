# Backlog — Mini Gestão de Pedidos

> Modelo: [templates/historia.md](templates/historia.md). Dono: `product-owner`. Cada tarefa `T-<US>.<n>` vira **exatamente um commit** (HL-06), feito pelo orquestrador com autorização.
> Requisitos (`RF`, `RNF`, `ENT`) e propostas (`PROP`) estão em [01-requisitos.md](01-requisitos.md). Regras e critérios (`RN`, `CA`) estão na [spec](../spec.md). As PROPs já foram decididas: cada referência cita a `HL-xx` do [plano](../plan.md) que a aceitou (PROP-01 a PROP-13 → HL-10 a HL-22; ADR-0004 → HL-23).
> Complexidade por [ADR-0001 §2](../adr/0001-roteamento-de-modelos.md): `C1` Haiku, `C2` Sonnet, `C3` Opus. Na dúvida, o nível mais alto.
> `[P]` = pode correr em paralelo com a tarefa vizinha da mesma onda (ver a tabela de ondas no fim).
> Os nomes de teste seguem `Metodo_Cenario_ResultadoEsperado`. O local exato dos arquivos é decisão do `arquiteto`; os caminhos abaixo seguem o `CLAUDE.md`.

## EP-01 — Mini Gestão de Pedidos de ponta a ponta

**Objetivo de negócio.** A operação comercial registra pedidos com baixa de estoque confiável, consulta o histórico com filtros e vê os clientes que mais compraram. O trabalho mostra um fluxo estruturado com o Claude Code (spec → plano → esteira → código testado).
**Critério de sucesso.** Toda linha `Obrigatório` do enunciado tem teste ou verificação aprovada; `docker compose up` num clone limpo sobe a aplicação; CI verde; README com os 5 itens; vídeo de até 10 minutos.
**Linhas do enunciado cobertas.** ENU-L001 a ENU-L100.

---

### US-01 — Fundação SDD e rastreabilidade

| Campo | Valor |
|---|---|
| História | Como **avaliador**, quero **spec, plano e matriz de requisitos versionados antes do código**, para **conferir o planejamento contra o enunciado**. |
| Prioridade | P1 |
| Dono | orquestrador |
| Requisitos | ENT-01, ENT-02, ENT-07, ENT-17, ENT-22, ENT-23 |
| Regras e critérios | — (PROP-01 a PROP-07) |
| Depende de | — |
| Linhas do enunciado | ENU-L007, ENU-L011, ENU-L013, ENU-L014, ENU-L017 |

**Critérios de aceite**

```gherkin
Funcionalidade: Fundação SDD

  Cenário: Matriz cobre o enunciado
    Dado a matriz em docs/esteira/rastreabilidade.md
    Quando o CI roda validar_rastreabilidade.py
    Então o resultado é 0 erro(s)

  Cenário: Decisões humanas aplicadas à spec
    Dado as PROP-01 a PROP-07 decididas pelo desenvolvedor
    Quando a spec é atualizada
    Então cada PROP aceita aparece na spec e cada decisão tem um HL-xx no plano
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-01.1 | | C1 | Versiona os artefatos da etapa 1 | `docs/esteira/01-requisitos.md`, `docs/esteira/backlog.md`, `docs/esteira/rastreabilidade.md` | Manual: validador da matriz sem erros | `python3 .github/scripts/validar_rastreabilidade.py` | `docs(esteira): registra requisitos, backlog e matriz de rastreabilidade (etapa 1)` |
| T-01.2 | | C2 | Aplica as PROPs aceitas (novos CA-33 a CA-38, RNF-08, ajustes em RN-14, RNF-04, RNF-06, §9). C2: texto já redigido nas PROPs | `docs/spec.md` | Manual: cada PROP aceita tem o texto na spec | `python3 .github/scripts/validar_rastreabilidade.py` | `docs(spec): incorpora as propostas aprovadas na etapa 1 (PROP-01 a PROP-07)` |
| T-01.3 | [P] | C1 | Registra as decisões humanas sobre as PROPs | `docs/plan.md` (seção HL) | Manual: um HL-xx por PROP decidida | `grep -c "PROP-0" docs/plan.md` | `docs(plan): registra as decisões sobre as propostas da etapa 1` |

**Definição de pronto**

- [ ] Validador da matriz com 0 erro(s).
- [ ] Cada PROP tem decisão registrada.
- [ ] Commits feitos com autorização.

---

### US-02 — Esqueleto da solução .NET

| Campo | Valor |
|---|---|
| História | Como **desenvolvedor**, quero **a solução .NET 8 em Clean Architecture com build sem warnings**, para **que cada história nasça no padrão do `CLAUDE.md`**. |
| Prioridade | P1 |
| Dono | backend |
| Requisitos | RNF-03, RNF-07, RNF-09, ENT-03 |
| Regras e critérios | — |
| Depende de | US-01 |
| Linhas do enunciado | ENU-L019, ENU-L029 |

**Critérios de aceite**

```gherkin
Funcionalidade: Esqueleto

  Cenário: Build limpo
    Dado a solução backend/Joya.Pedidos.sln com Core, Infrastructure, Api, UnitTests e IntegrationTests
    Quando eu rodo dotnet build
    Então o build passa com 0 warnings e TreatWarningsAsErrors ligado

  Cenário: Tipos comuns
    Dado Result<T>, Error e PaginaResponse<T> no Core
    Quando um resultado de falha é criado
    Então ele carrega o código do erro e não lança exceção
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-02.1 | | C2 | Solução, 3 projetos de `src`, 2 de testes, `Directory.Build.props` (net8.0, nullable, analisadores, `TreatWarningsAsErrors`), `.editorconfig` | `backend/Joya.Pedidos.sln`, `backend/Directory.Build.props`, `backend/src/*/*.csproj`, `backend/tests/*/*.csproj` | Manual: build sem warnings | `dotnet build backend/Joya.Pedidos.sln` | `chore(backend): cria a solução .NET 8 em Clean Architecture` |
| T-02.2 | | C2 | `Result<T>`, `Error`, `PaginaResponse<T>` com testes | `backend/src/Joya.Pedidos.Core/Common/*.cs`, `backend/tests/Joya.Pedidos.UnitTests/Common/ResultTests.cs` | `Failure_ErroInformado_CarregaCodigoSemExcecao`, `PaginaResponse_TotalItems_CalculaTotalPages` (unit) | `dotnet test backend/tests/Joya.Pedidos.UnitTests` | `feat(core): adiciona Result, Error e PaginaResponse` |

---

### US-03 — Qualidade automatizada: CI e testes de arquitetura

| Campo | Valor |
|---|---|
| História | Como **avaliador**, quero **um pipeline que compila e testa a cada push e PR, e testes que provam as regras do `CLAUDE.md`**, para **confiar que a IA seguiu as convenções**. |
| Prioridade | P3 |
| Dono | backend |
| Requisitos | ENT-20, ENT-22, ENT-03, ENT-19 |
| Regras e critérios | RNF-03, RNF-07 |
| Depende de | US-02 (T-03.1, T-03.2); US-04 (T-03.3); US-14 (T-03.4); US-16 (T-03.5) |
| Linhas do enunciado | ENU-L019, ENU-L086, ENU-L085 |

**Critérios de aceite**

```gherkin
Funcionalidade: CI e arquitetura

  Cenário: Pipeline em todo push e PR
    Dado um push ou pull request
    Quando o GitHub Actions roda
    Então build, format, testes unitários, testes de integração, lint e build do frontend e a matriz passam

  Cenário: Regra de arquitetura violada falha o teste
    Dado uma classe do Core que referencia EF Core
    Quando os testes de arquitetura rodam
    Então o teste Core_Dependencias_NaoReferenciaEfAspNetNemSqlClient falha
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-03.1 | | C2 | Job de build, `dotnet format` e testes unitários no `ci.yml` existente | `.github/workflows/ci.yml` | Manual: workflow verde no PR | `gh run list --branch <branch> --limit 1` | `ci: compila e roda os testes unitários a cada push e PR` |
| T-03.2 | [P] | C2 | As 9 regras de arquitetura do [02-arquitetura.md §3](02-arquitetura.md) (HL-08), com ArchUnitNET e reflexão: dependências do Core, da Infrastructure e dos controllers; DTOs `sealed record`; entidades sem setter público; `CancellationToken` por último nos `*Async`; classes `sealed`; testes de regra sem I/O. O `UnitTests` passa a referenciar `Api` e `Infrastructure` só para carregar os assemblies. C2: mecanismo de cada regra já fixado pelo arquiteto | `backend/tests/Joya.Pedidos.UnitTests/Arquitetura/RegrasDeArquiteturaTests.cs`, `backend/tests/Joya.Pedidos.UnitTests/Joya.Pedidos.UnitTests.csproj` (pacote `TngTech.ArchUnitNET.xUnit`) | `Core_Dependencias_NaoReferenciaEfAspNetNemSqlClient`, `Core_Dependencias_NaoReferenciaInfrastructureNemApi`, `Infrastructure_Dependencias_NaoReferenciaApi`, `Controllers_Dependencias_NaoAcessamDbContext`, `Dtos_Declaracao_SaoSealedRecord`, `Entidades_Propriedades_NaoTemSetterPublico`, `MetodosAsync_Assinatura_RecebemCancellationTokenPorUltimo`, `Classes_Declaracao_SaoSealed`, `TestesDeRegra_Dependencias_NaoUsamInfrastructureNemApi` (unit) | `dotnet test backend/tests/Joya.Pedidos.UnitTests --filter "FullyQualifiedName~Arquitetura"` | `test(arq): transforma regras do CLAUDE.md em testes de arquitetura (HL-08)` |
| T-03.3 | | C2 | Job de testes de integração (Docker no runner) | `.github/workflows/ci.yml` | Manual: job de integração verde | `gh run list --branch <branch> --limit 1` | `ci: roda os testes de integração contra SQL Server` |
| T-03.4 | | C2 | Job de lint, build e testes do frontend | `.github/workflows/ci.yml` | Manual: job do frontend verde | `gh run list --branch <branch> --limit 1` | `ci: valida lint, build e testes do frontend` |
| T-03.5 | | C2 | Job do Playwright subindo o compose | `.github/workflows/ci.yml` | Manual: job E2E verde | `gh run list --branch <branch> --limit 1` | `ci: roda os testes end to end com Playwright` |

---

### US-04 — Banco de dados com schema, índices, logins e carga

| Campo | Valor |
|---|---|
| História | Como **desenvolvedor**, quero **o schema, os índices, os logins e a carga em scripts SQL idempotentes**, para **que o banco suba igual no Docker, nos testes e no MCP**. |
| Prioridade | P1 |
| Dono | backend |
| Requisitos | RF-07, RF-08, RNF-02, RNF-04, RNF-06, RNF-07, RNF-09, ENT-06 |
| Regras e critérios | RN-03, RN-08 (CHECK), spec §6 |
| Depende de | US-02 |
| Linhas do enunciado | ENU-L022, ENU-L025, ENU-L027, ENU-L028, ENU-L055, ENU-L063 |

**Critérios de aceite**

```gherkin
Funcionalidade: Banco de dados

  Cenário: Schema aplicado
    Dado um SQL Server vazio
    Quando os scripts de database/ são aplicados
    Então existem as 4 tabelas com PKs, FKs, CHECKs e os índices da spec 6.3

  Cenário: Menor privilégio
    Dado o login joya_app
    Quando ele tenta DELETE em Pedidos
    Então o SQL Server nega a permissão
    E o login claude_mcp não consegue INSERT em nenhuma tabela

  Cenário: Carga idempotente
    Dado os scripts já aplicados
    Quando eu aplico os scripts de novo
    Então nada falha e nenhuma linha é duplicada
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-04.1 | | C3 | Schema (spec 6.2, `READ_COMMITTED_SNAPSHOT`), `database/init.sh` (aplica os `*.sql` em ordem) e fixture Testcontainers + Respawn que roda o mesmo `init.sh` (ADR-0003). O `init.sh` nasce aqui porque o fixture depende dele; a T-04.5 só o liga ao compose. C3: fixture é decisão sem padrão anterior | `database/01-schema.sql`, `database/init.sh`, `backend/tests/Joya.Pedidos.IntegrationTests/Infra/*.cs` | `Schema_ScriptsAplicados_CriaTabelasChavesEConstraints` (integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~Schema"` | `feat(db): cria o schema de clientes, produtos e pedidos` |
| T-04.2 | | C3 | Índices da spec 6.3. C3: desempenho (ADR-0001 cita índices) | `database/02-indices.sql` | `Indices_ScriptAplicado_ExistemIndicesDaSpec` (integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~Indices"` | `feat(db): cria os índices das consultas de listagem e ranking` |
| T-04.3 | | C3 | Logins `joya_app` e `claude_mcp` (spec 6.5). C3: segurança | `database/03-logins.sql` | `LoginApp_TentaDelete_PermissaoNegada`, `LoginMcp_TentaEscrever_PermissaoNegada` (integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~Login"` | `feat(db): cria logins de menor privilégio para a API e o MCP` |
| T-04.4 | | C1 | Carga de exemplo idempotente com homônimos, inativos e pedidos de 2025 e 2026 | `database/04-carga.sql` | `Carga_AplicadaDuasVezes_NaoDuplicaLinhas` (integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~Carga"` | `feat(db): adiciona carga de dados de exemplo` |
| T-04.5 | | C2 | `docker-compose.yml` na raiz com `db` e `db-init` (healthcheck; `db-init` roda o `database/init.sh` criado na T-04.1) | `docker-compose.yml` | Manual: `docker compose up -d db db-init` termina com `db-init` saindo com código 0 | `docker compose up -d db db-init && docker compose ps` | `feat(docker): sobe o SQL Server e aplica os scripts automaticamente` |

---

### US-05 — Base HTTP da API

| Campo | Valor |
|---|---|
| História | Como **cliente da API**, quero **erros em Problem Details, health checks e limites contra abuso**, para **diagnosticar falhas sem expor detalhes internos**. |
| Prioridade | P1 |
| Dono | backend |
| Requisitos | RNF-05, RNF-06 |
| Regras e critérios | spec §5.1; CA-36, CA-38 (HL-11); ADR-0002 (HL-12); limite elevado nos testes (HL-19) |
| Depende de | US-04 |
| Linhas do enunciado | ENU-L029, ENU-L064 |

**Critérios de aceite**

```gherkin
Funcionalidade: Base HTTP

  Cenário: CA-38 (HL-11) — falha inesperada
    Dado um erro não previsto em qualquer rota
    Quando a requisição é processada
    Então a resposta é 500 application/problem+json com code "ERRO_INTERNO" e traceId, sem stack trace

  Cenário: CA-36 (HL-11) — readiness
    Dado o SQL Server fora do ar
    Quando eu chamo GET /health/ready
    Então a resposta é 503, e GET /health/live continua 200
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-05.1 | | C2 | `Program.cs`: Problem Details, `IExceptionHandler`, log estruturado, Swagger, `415` | `backend/src/Joya.Pedidos.Api/Program.cs`, `backend/src/Joya.Pedidos.Api/Erros/*.cs` | `Excecao_NaoTratada_Retorna500ProblemDetailsSemStackTrace`, `Post_ContentTypeTexto_Retorna415` (integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~Excecao\|FullyQualifiedName~ContentType"` | `feat(api): padroniza erros em Problem Details com code e traceId` |
| T-05.2 | [P] | C2 | Health checks `live` e `ready` | `backend/src/Joya.Pedidos.Api/Program.cs` | `HealthLive_ProcessoNoAr_Retorna200`, `HealthReady_BancoDisponivel_Retorna200` (integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~Health"` | `feat(api): adiciona health checks de liveness e readiness` |
| T-05.3 | | C3 | Limite de corpo (`413`) e *rate limiter* nativo no `POST /pedidos` (`429`), com os valores do ADR-0002 (HL-12). O limite de taxa vem de configuração: o ambiente de teste o eleva para que CA-10 e CA-37 não esbarrem nele, e o teste do `429` usa um limite baixo próprio (RNF-06, HL-19). C3: segurança | `backend/src/Joya.Pedidos.Api/Program.cs`, `backend/src/Joya.Pedidos.Api/appsettings*.json`, `backend/tests/Joya.Pedidos.IntegrationTests/Infra/*.cs` | `PostPedidos_CorpoAcimaDoLimite_Retorna413`, `PostPedidos_RajadaAcimaDoLimite_Retorna429` (RNF-06, integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~Limite"` | `feat(api): limita tamanho do corpo e taxa de criação de pedidos` |

---

### US-06 — Criar pedido com baixa atômica de estoque

| Campo | Valor |
|---|---|
| História | Como **operador comercial**, quero **criar um pedido com itens validando cliente e estoque**, para **nunca vender o que não existe, nem com vendas simultâneas**. |
| Prioridade | P1 |
| Dono | backend |
| Requisitos | RF-01, RNF-01, RNF-07 |
| Regras e critérios | RN-01…RN-13; CA-01…CA-10, CA-12, CA-14 |
| Depende de | US-04, US-05 |
| Linhas do enunciado | ENU-L031, ENU-L054, ENU-L055 |

**Critérios de aceite** (texto completo na spec §8.1)

```gherkin
Funcionalidade: Criar pedido

  Cenário: CA-01 — Pedido válido é criado, debita estoque e grava o preço praticado
    Quando eu envio POST /pedidos para o cliente 7 com 2 unidades do produto 12 e 2 do produto 31
    Então a resposta é 201 com Location /pedidos/{id}, valorTotal 359,70 e os estoques passam a 8 e 3

  Cenário: CA-02 — Pedido sem itens é rejeitado
    Quando eu envio POST /pedidos com a lista de itens vazia
    Então a resposta é 400 com code "VALIDACAO" e nenhum pedido é gravado

  Cenário: CA-05 — Cliente inexistente
    Quando eu envio POST /pedidos para o cliente 9999
    Então a resposta é 422 com code "CLIENTE_NAO_ENCONTRADO" e o estoque não muda

  Cenário: CA-08 — Estoque insuficiente não grava nada
    Quando eu envio 3 unidades do produto 12 e 6 do produto 31
    Então a resposta é 409 "ESTOQUE_INSUFICIENTE" listando o produto 31 com estoqueDisponivel 5
    E nenhum pedido é gravado e nenhum estoque muda

  Cenário: CA-10 — Concorrência
    Dado o produto 50 com estoque 5
    Quando 20 requisições simultâneas pedem 1 unidade cada
    Então exatamente 5 são 201, 15 são 409 e o estoque final é 0
```

Os CA-03, CA-04, CA-06, CA-07, CA-09, CA-12 e CA-14 valem como estão na spec.

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-06.1 | | C2 | Entidades `Pedido` e `ItemPedido` com invariantes e cálculo do total (RN-11) | `backend/src/Joya.Pedidos.Core/Pedidos/Pedido.cs`, `ItemPedido.cs`, testes | `Criar_ItensValidos_CalculaValorTotalEmDecimal` (unit) | `dotnet test backend/tests/Joya.Pedidos.UnitTests --filter "FullyQualifiedName~Pedido"` | `feat(core): modela pedido e itens com valor total em decimal (RN-11)` |
| T-06.2 | [P] | C2 | `CriarPedidoRequest` e `CriarPedidoValidator` (RN-01 a RN-04) | `backend/src/Joya.Pedidos.Core/Pedidos/CriarPedidoRequest.cs`, `CriarPedidoValidator.cs`, testes | `Validate_ItensVazios_RetornaErroNoCampoItens` (CA-02), `Validate_QuantidadeForaDoLimite_RetornaErroNaQuantidade` (CA-03), `Validate_ProdutoRepetido_RetornaErroDeDuplicidade` (CA-04), `Validate_MaisDeCinquentaItens_RetornaErroNoCampoItens` (unit) | `dotnet test backend/tests/Joya.Pedidos.UnitTests --filter "FullyQualifiedName~CriarPedidoValidator"` | `feat(core): valida estrutura do pedido (RN-01 a RN-04)` |
| T-06.3 | | C2 | `PedidoService.CriarAsync`: cliente, produtos, ordem de avaliação, baixa via porta em ordem de `ProdutoId`, `TimeProvider` | `backend/src/Joya.Pedidos.Core/Pedidos/PedidoService.cs`, `PedidoErrors.cs`, portas, testes | `CriarAsync_ClienteInexistente_RetornaClienteNaoEncontrado` (CA-05), `CriarAsync_ClienteInativo_RetornaClienteInativo` (CA-06), `CriarAsync_ProdutosInvalidos_RetornaTodosOsIdsInvalidos` (CA-07), `CriarAsync_EstoqueInsuficiente_RetornaConflitoSemGravar` (CA-08), `CriarAsync_ItensForaDeOrdem_BaixaEmOrdemCrescenteDeProdutoId`, `CriarAsync_PedidoValido_UsaDataDoTimeProviderEmUtc`, `CriarAsync_RelogioComTicks_TruncaDataPedidoEmMilissegundos` (RN-13, HL-20) (unit) | `dotnet test backend/tests/Joya.Pedidos.UnitTests --filter "FullyQualifiedName~PedidoService"` | `feat(core): orquestra a criação do pedido (RN-05 a RN-08, RN-13)` |
| T-06.4 | | C3 | `DbContext` e mapeamento EF sobre o schema existente, `UnitOfWork`, repositórios de cliente e produto. **Sem *retry* automático** (ADR-0004, HL-23): sem `EnableRetryOnFailure` e sem `CreateExecutionStrategy`. `IUnitOfWork.ExecutarEmTransacaoAsync` faz commit em `Success` e rollback com `ChangeTracker.Clear()` em `Failure` (RN-10). C3: ADR-0001 cita mapeamento EF | `backend/src/Joya.Pedidos.Infrastructure/**` | `Mapeamento_PedidoGravado_LeIgualAoSchema`, `ExecutarEmTransacaoAsync_OperacaoFalha_DesfazAlteracoes` (RN-10) (integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~Mapeamento\|FullyQualifiedName~ExecutarEmTransacaoAsync"` | `feat(infra): mapeia o schema existente com EF Core` |
| T-06.5 | | C3 | `IEstoqueRepository.TentarBaixarAsync` com `UPDATE … OUTPUT inserted.Preco` (spec 4.3). C3: concorrência | `backend/src/Joya.Pedidos.Infrastructure/Estoque/EstoqueRepository.cs` | `TentarBaixarAsync_EstoqueSuficiente_DebitaERetornaPreco`, `TentarBaixarAsync_EstoqueInsuficiente_NaoDebita` (integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~TentarBaixarAsync"` | `feat(infra): debita estoque com instrução atômica (RN-08, RN-09)` |
| T-06.6 | | C2 | `PedidosController.Criar` (`POST /pedidos`), `201` + `Location`, tradução de `Result` (use `/new-endpoint`) | `backend/src/Joya.Pedidos.Api/Controllers/PedidosController.cs`, testes | `PostPedidos_PedidoValido_Retorna201ComLocationEDebitaEstoque` (CA-01), `PostPedidos_SemItens_Retorna400SemGravar` (CA-02), `PostPedidos_QuantidadeInvalida_Retorna400` (CA-03), `PostPedidos_ProdutoRepetido_Retorna400` (CA-04), `PostPedidos_ClienteInexistente_Retorna422` (CA-05), `PostPedidos_ClienteInativo_Retorna422` (CA-06), `PostPedidos_ProdutosInvalidos_Retorna422ComIds` (CA-07), `PostPedidos_EstoqueInsuficiente_Retorna409SemGravarNada` (CA-08), `PostPedidos_ConsomeTodoEstoque_Retorna201EZeraEstoque` (CA-09), `PostPedidos_PrecoNoCorpo_IgnoraEUsaPrecoDeTabela` (CA-12), `PostPedidos_RegrasEmConflito_Retorna400Primeiro` (CA-14) (integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~PostPedidos"` | `feat(api): expõe POST /pedidos` |
| T-06.7 | | C3 | Teste de concorrência com 20 requisições paralelas. C3: concorrência | `backend/tests/Joya.Pedidos.IntegrationTests/Pedidos/ConcorrenciaTests.cs` | `PostPedidos_VinteRequisicoesSimultaneas_EstoqueNuncaFicaNegativo` (CA-10, integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~VinteRequisicoesSimultaneas"` | `test(api): prova que o estoque não fica negativo sob concorrência (CA-10)` |

---

### US-07 — Catálogo de clientes e produtos para as telas

| Campo | Valor |
|---|---|
| História | Como **operador comercial**, quero **escolher cliente e produtos ativos numa lista**, para **criar o pedido sem digitar IDs**. |
| Prioridade | P1 (derivado, habilita ENU-L037) |
| Dono | backend |
| Requisitos | RF-10 |
| Regras e critérios | RN-06, RN-07; CA-35 (HL-11); spec §5.5 com o parâmetro `ativo` opcional (HL-18) |
| Depende de | US-06 (T-06.4) |
| Linhas do enunciado | ENU-L037 |

**Critérios de aceite**

```gherkin
Funcionalidade: Catálogo

  Cenário: CA-35 (HL-11) — Só ativos, por nome
    Quando eu envio GET /clientes?ativo=true e GET /produtos?ativo=true
    Então recebo apenas registros ativos, ordenados por nome

  Cenário: Parâmetro ativo opcional (spec §5.5, HL-18)
    Quando eu envio GET /clientes sem o parâmetro ativo
    Então recebo todos os clientes, ativos e inativos, ordenados por nome
    E GET /clientes?ativo=talvez responde 400 com code "VALIDACAO"
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-07.1 | | C2 | `GET /clientes` e `GET /produtos` com projeção `AsNoTracking` e `ativo` opcional: ausente traz todos, valor não booleano → `400` (HL-18). Use `/new-endpoint` | `backend/src/Joya.Pedidos.Core/Clientes/**`, `Produtos/**`, `Infrastructure`, `Api/Controllers/*` | `GetClientes_FiltroAtivo_RetornaSoAtivosOrdenadosPorNome`, `GetProdutos_FiltroAtivo_RetornaSoAtivosOrdenadosPorNome` (CA-35), `GetClientes_SemFiltroAtivo_RetornaTodos`, `GetClientes_AtivoInvalido_Retorna400`, `GetProdutos_AtivoInvalido_Retorna400` (spec §5.5, HL-18) (integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~GetClientes\|FullyQualifiedName~GetProdutos"` | `feat(api): expõe clientes e produtos ativos para os seletores` |

---

### US-08 — Listar pedidos com filtros e paginação

| Campo | Valor |
|---|---|
| História | Como **operador comercial**, quero **listar pedidos por cliente e período, paginados**, para **consultar o histórico de vendas**. |
| Prioridade | P1 |
| Dono | backend |
| Requisitos | RF-02, RNF-02, RNF-07 |
| Regras e critérios | RN-15, RN-16; CA-15…CA-20; RNF-02 na redação aceita na HL-17: *seek* só na listagem com filtro; sem filtro, *scan* ordenado com `TOP` |
| Depende de | US-06 (T-06.4) |
| Linhas do enunciado | ENU-L032, ENU-L055 |

**Critérios de aceite** (texto completo na spec §8.2)

```gherkin
Funcionalidade: Listar pedidos

  Cenário: CA-15 — Listagem padrão paginada
    Quando eu envio GET /pedidos
    Então recebo 20 itens, page 1, totalItems 75, totalPages 4, ordenados por dataPedido e id decrescentes

  Cenário: CA-17 — Período inclusivo nas duas pontas
    Quando eu envio GET /pedidos?dataInicio=2026-03-01&dataFim=2026-03-31
    Então os pedidos de 01/03 00:00:00.000 e 31/03 23:59:59.999 entram, e os de 28/02 e 01/04 não

  Cenário: CA-19 — Parâmetros inválidos
    Quando eu envio GET /pedidos?page=0
    Então a resposta é 400 com code "VALIDACAO"
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-08.1 | [P] | C2 | `Periodo` (fim exclusivo = fim + 1 dia) e `ListarPedidosQueryValidator` | `backend/src/Joya.Pedidos.Core/Common/Periodo.cs`, `Pedidos/ListarPedidosQuery*.cs`, testes | `Criar_DataFimInclusiva_GeraFimExclusivoNoDiaSeguinte`, `Validate_ParametrosInvalidos_RetornaErroDeValidacao` (CA-19) (unit) | `dotnet test backend/tests/Joya.Pedidos.UnitTests --filter "FullyQualifiedName~Periodo\|FullyQualifiedName~ListarPedidos"` | `feat(core): valida período e paginação da listagem (RN-15, RN-16)` |
| T-08.2 | | C2 | `GET /pedidos` com filtro semiaberto e projeção direta | `Infrastructure/Pedidos/PedidoConsultas.cs`, `Api/Controllers/PedidosController.cs`, testes | `GetPedidos_SemFiltro_RetornaPrimeiraPaginaOrdenada` (CA-15), `GetPedidos_FiltroCliente_RetornaSoDoCliente` (CA-16), `GetPedidos_FiltroPeriodo_IncluiAsDuasPontas` (CA-17), `GetPedidos_PercorreTodasAsPaginas_NaoRepeteNemOmite` (CA-18), `GetPedidos_ParametrosInvalidos_Retorna400` (CA-19), `GetPedidos_ClienteSemPedidos_Retorna200Vazio` (CA-20) (integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~GetPedidos"` | `feat(api): expõe GET /pedidos com filtros e paginação` |

---

### US-09 — Relatório de top 5 clientes

| Campo | Valor |
|---|---|
| História | Como **gestor comercial**, quero **os 5 clientes com maior valor no período**, para **identificar os melhores clientes**. |
| Prioridade | P1 |
| Dono | backend |
| Requisitos | RF-03, RNF-02, RNF-07 |
| Regras e critérios | RN-15, RN-17; CA-21…CA-27 |
| Depende de | US-08 (T-08.1) |
| Linhas do enunciado | ENU-L033, ENU-L055 |

**Critérios de aceite** (texto completo na spec §8.3)

```gherkin
Funcionalidade: Top 5 clientes

  Cenário: CA-21 — Ranking do período
    Dado 8 clientes com pedidos em 2026
    Quando eu envio GET /relatorios/top-clientes?dataInicio=2026-01-01&dataFim=2026-12-31
    Então recebo exatamente 5 clientes em ordem decrescente de valorTotal, posicao 1 a 5

  Cenário: CA-23 — Homônimos não são somados
    Dado os clientes 15 e 22 chamados "João da Silva" com 500,00 e 300,00
    Então o ranking traz duas linhas, e nenhuma com 800,00

  Cenário: CA-26 — Período obrigatório
    Quando eu envio GET /relatorios/top-clientes sem dataInicio
    Então a resposta é 400 com code "VALIDACAO"
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-09.1 | | C2 | `TopClientesQueryValidator` (período obrigatório) | `backend/src/Joya.Pedidos.Core/Relatorios/*.cs`, testes | `Validate_SemDataInicio_RetornaErroDeValidacao` (CA-26, unit) | `dotnet test backend/tests/Joya.Pedidos.UnitTests --filter "FullyQualifiedName~TopClientes"` | `feat(core): exige período no relatório de top clientes (RN-17)` |
| T-09.2 | | C3 | Consulta agrupada por `ClienteId`, desempate por id, `GET /relatorios/top-clientes`. C3: desempenho e o mesmo defeito da Parte 4 | `Infrastructure/Relatorios/*.cs`, `Api/Controllers/RelatoriosController.cs`, testes | `GetTopClientes_OitoClientes_RetornaCincoEmOrdemDecrescente` (CA-21), `GetTopClientes_PedidosForaDoPeriodo_NaoContam` (CA-22), `GetTopClientes_Homonimos_NaoSomaNomesIguais` (CA-23), `GetTopClientes_Empate_OrdenaPorMenorClienteId` (CA-24), `GetTopClientes_MenosDeCinco_RetornaSoQuemComprou` (CA-25), `GetTopClientes_SemPeriodo_Retorna400` (CA-26), `GetTopClientes_ValorTotal_IgualSomaDosItens` (CA-27) (integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~GetTopClientes"` | `feat(api): expõe GET /relatorios/top-clientes` |

---

### US-10 — Parte 4: debug e otimização da query

| Campo | Valor |
|---|---|
| História | Como **avaliador**, quero **a query do ranking corrigida, otimizada e com a investigação documentada**, para **avaliar o raciocínio de diagnóstico**. |
| Prioridade | P1 |
| Dono | backend |
| Requisitos | RF-04, RNF-02, ENT-06, ENT-14 |
| Regras e critérios | RN-15, RN-17; CA-22, CA-23; CA-33 (HL-10) |
| Depende de | US-04, US-09 |
| Linhas do enunciado | ENU-L022, ENU-L040, ENU-L042 a ENU-L048 |

**Critérios de aceite**

```gherkin
Funcionalidade: Query da Parte 4

  Cenário: A query original tem os defeitos descritos
    Dado a carga com homônimos e pedidos de 2025 e 2026
    Quando executo a query original
    Então os homônimos aparecem somados e a ordem é crescente

  Cenário: CA-33 (HL-10) — Query corrigida
    Quando executo a query corrigida
    Então há uma linha por ClienteId, em ordem decrescente de total, só com compras de 2026
    E os 5 primeiros coincidem com GET /relatorios/top-clientes de 2026
    E o plano de execução não tem scan de Pedidos nem YEAR() sobre DataPedido
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-10.1 | | C3 | Reproduz os defeitos da query original em teste. C3: Parte 4 | `database/parte4/01-query-original.sql`, `backend/tests/Joya.Pedidos.IntegrationTests/Parte4/QueryRankingTests.cs` | `QueryOriginal_ClientesHomonimos_SomaIndevidamente`, `QueryOriginal_Ordenacao_RetornaMenorTotalPrimeiro` (integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~QueryOriginal"` | `test(db): reproduz os defeitos da query de ranking da Parte 4` |
| T-10.2 | | C3 | Query corrigida: `INNER JOIN`, `GROUP BY c.Id, c.Nome`, intervalo semiaberto, `ORDER BY Total DESC, c.Id`. C3: desempenho | `database/parte4/02-query-corrigida.sql`, teste | `QueryCorrigida_Ranking2026_CoincideComTopClientes` (CA-33, integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~QueryCorrigida"` | `perf(db): corrige e otimiza a query de ranking de 2026 (Parte 4)` |
| T-10.3 | | C3 | Registra a investigação: planos antes e depois, leituras lógicas, uso do MCP `mssql`. C3: análise de desempenho | `database/parte4/investigacao.md` | Manual: plano antes e depois anexado, consultas do MCP transcritas | `docker compose up -d db db-init` e consulta via MCP | `docs(db): documenta a investigação da query com plano de execução` |

---

### US-11 — Consultar pedido por id

| Campo | Valor |
|---|---|
| História | Como **operador comercial**, quero **ver um pedido com seus itens**, para **conferir o que foi vendido e a que preço**. |
| Prioridade | P2 (derivado) |
| Dono | backend |
| Requisitos | RF-09 |
| Regras e critérios | RN-09, RN-13 (HL-20); CA-11; CA-34 (HL-11); contrato OpenAPI (ADR-0005, HL-21) na T-11.2 |
| Depende de | US-06; T-11.2 depende também de T-07.1, T-08.2, T-09.2 e T-12.1 (todos os endpoints) |
| Linhas do enunciado | — (derivado do `Location` de ENU-L031); T-11.2 apoia ENU-L080 |

**Critérios de aceite**

```gherkin
Funcionalidade: Detalhe do pedido

  Cenário: CA-11 — Preço praticado não muda
    Dado um pedido com 2 unidades do produto 12 a 149,90
    Quando o preço de tabela muda para 199,90
    Então GET /pedidos/{id} continua com precoUnitario 149,90 e valorTotal 299,80

  Cenário: CA-34 (HL-11) — Pedido inexistente
    Quando eu envio GET /pedidos/999999
    Então a resposta é 404 com code "PEDIDO_NAO_ENCONTRADO"

  Cenário: DataPedido estável entre criação e leitura (RN-13, HL-20)
    Dado um pedido criado por POST /pedidos
    Quando eu leio GET /pedidos/{id}
    Então dataPedido é o mesmo instante do 201, com precisão de milissegundos

  Cenário: Contrato publicado igual ao versionado (ADR-0005, HL-21)
    Dado docs/contratos/openapi.json no repositório
    Quando a API no ambiente de teste publica /swagger/v1/swagger.json
    Então os dois documentos são iguais, ou o teste falha pedindo para regerar e revisar o diff
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-11.1 | | C2 | `GET /pedidos/{id}` com projeção única (sem `Include`) | `Infrastructure/Pedidos/PedidoConsultas.cs`, `Api/Controllers/PedidosController.cs`, testes | `GetPedidoPorId_PrecoDeTabelaAlterado_MantemPrecoPraticado` (CA-11), `GetPedidoPorId_Inexistente_Retorna404` (CA-34), `GetPedidoPorId_AposCriacao_DataPedidoIgualAoRetornoDoPost` (RN-13, HL-20) (integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~GetPedidoPorId"` | `feat(api): expõe GET /pedidos/{id} com os itens` |
| T-11.2 | | C2 | Contrato OpenAPI (ADR-0005): Swagger também no ambiente `Testing` (HL-21), `ObrigatoriosSchemaFilter`, `MapType<decimal>`, `CustomSchemaIds`, `docs/contratos/openapi.json` gerado pelo teste com `ATUALIZAR_CONTRATO=1`. Roda depois de todos os endpoints (ver dependências) | `backend/src/Joya.Pedidos.Api/Program.cs`, `backend/src/Joya.Pedidos.Api/Swagger/ObrigatoriosSchemaFilter.cs`, `docs/contratos/openapi.json`, `backend/tests/Joya.Pedidos.IntegrationTests/Contrato/ContratoTests.cs` | `Contrato_OpenApiPublicado_IgualAoVersionado` (ADR-0005, HL-21, integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~Contrato_OpenApiPublicado"` | `test(api): versiona e verifica o contrato OpenAPI` |

---

### US-12 — Idempotência na criação

| Campo | Valor |
|---|---|
| História | Como **cliente da API**, quero **repetir um POST com a mesma chave sem duplicar o pedido**, para **me proteger de duplo clique e retry**. |
| Prioridade | P2 (derivado) |
| Dono | backend |
| Requisitos | RF-11, RNF-01 |
| Regras e critérios | RN-14; CA-13; CA-37 (HL-14) |
| Depende de | US-06 |
| Linhas do enunciado | — (acrescentado) |

**Critérios de aceite**

```gherkin
Funcionalidade: Idempotência

  Cenário: CA-13 — Repetição com a mesma chave
    Dado um POST /pedidos com Idempotency-Key UUID que devolveu 201 com o pedido 1042
    Quando reenvio com a mesma chave
    Então recebo 200 com o pedido 1042, Idempotent-Replayed: true, e o estoque foi debitado uma vez

  Cenário: CA-37 (HL-14) — Chave inválida e corrida
    Quando envio Idempotency-Key "abc"
    Então recebo 400 com code "VALIDACAO"
    E duas requisições simultâneas com a mesma chave geram um 201, um 200 e uma única baixa
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-12.1 | | C3 | Cabeçalho `Idempotency-Key`, busca por chave e reapresentação. C3: idempotência (ADR-0001) | `Core/Pedidos/*.cs`, `Infrastructure/Pedidos/*.cs`, `Api/Controllers/PedidosController.cs`, testes | `CriarAsync_ChaveJaUsada_RetornaPedidoOriginalSemBaixa` (unit), `PostPedidos_MesmaChave_Retorna200SemNovaBaixa` (CA-13), `PostPedidos_ChaveNaoUuid_Retorna400` (CA-37) (integração) | `dotnet test backend/Joya.Pedidos.sln --filter "FullyQualifiedName~Chave"` | `feat(api): torna a criação de pedido idempotente por chave (RN-14)` |
| T-12.2 | | C3 | Corrida na mesma chave: violação do índice único vira reapresentação. C3: concorrência. Reapresentação depois do rollback (HL-14, ADR-0004) | `Infrastructure/Pedidos/*.cs`, teste | `PostPedidos_MesmaChaveSimultanea_UmaUnicaBaixa` (CA-37, integração) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~MesmaChaveSimultanea"` | `fix(infra): trata corrida de requisições com a mesma chave de idempotência` |

---

### US-13 — Aplicação completa com um comando

| Campo | Valor |
|---|---|
| História | Como **avaliador**, quero **subir banco, API e frontend com `docker compose up`**, para **avaliar sem configurar nada**. |
| Prioridade | P1 |
| Dono | backend |
| Requisitos | RNF-04, RNF-06, ENT-15 |
| Regras e critérios | spec RNF-04 (HL-15) |
| Depende de | US-04 (T-04.5), US-05; US-14 (T-14.5) para T-13.2 |
| Linhas do enunciado | ENU-L061 a ENU-L068, ENU-L079 |

**Critérios de aceite**

```gherkin
Funcionalidade: Docker

  Cenário: Clone limpo sobe com um comando
    Dado um clone limpo, só com Docker instalado e sem .env
    Quando eu rodo docker compose up
    Então o banco recebe schema e carga, a API responde /health/ready 200 e o frontend abre em http://localhost:4200

  Cenário: Subir de novo
    Dado a aplicação já criada uma vez
    Quando eu rodo docker compose up de novo
    Então nada falha e a carga não duplica
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-13.1 | | C2 | `Dockerfile` da API (multi-stage, sem root) e serviço `api` no compose com `depends_on` saudável e login `joya_app` | `backend/Dockerfile`, `docker-compose.yml` | Manual: `docker compose up -d api` e `/health/ready` devolve 200 | `docker compose up -d --build api && docker compose ps` | `feat(docker): empacota a API sem root e a liga ao banco` |
| T-13.2 | | C2 | Serviço `frontend` no compose, ligado à imagem do frontend | `docker-compose.yml` | Manual: clone limpo, `docker compose up`, telas abrem em http://localhost:4200 | `docker compose down -v && docker compose up -d && docker compose ps` | `feat(docker): sobe a aplicação completa com um comando` |

---

### US-14 — Tela de listagem de pedidos

| Campo | Valor |
|---|---|
| História | Como **operador comercial**, quero **uma tela que lista pedidos com filtro de cliente e período**, para **consultar vendas sem usar a API**. |
| Prioridade | P1 |
| Dono | frontend |
| Requisitos | RF-05, RNF-08, RNF-09 |
| Regras e critérios | RN-15, RN-16; CA-28, CA-29 |
| Depende de | US-07, US-08 |
| Linhas do enunciado | ENU-L034, ENU-L036, ENU-L065 |

**Critérios de aceite** (texto completo na spec §8.4)

```gherkin
Funcionalidade: Listagem

  Cenário: CA-28 — Listagem com filtros
    Quando eu seleciono "Mercado Bom Preço Ltda" e o período de 01/01/2026 a 31/12/2026 e aplico
    Então a tabela mostra só pedidos desse cliente nesse período, com data, cliente, itens e valor em pt-BR
    E os filtros ficam na URL e sobrevivem a um recarregamento

  Cenário: CA-29 — Estados
    Quando a consulta está em andamento, volta vazia ou falha
    Então vejo um indicador, "Nenhum pedido encontrado" ou um erro com "Tentar novamente"
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-14.1 | | C2 | Projeto Angular 19 standalone, Tailwind, ESLint, `LOCALE_ID` `pt-BR` | `frontend/**` (configuração) | `AppComponent_Inicializacao_UsaLocalePtBr` (componente) | `npm --prefix frontend run lint && npm --prefix frontend run build` | `chore(frontend): cria o projeto Angular 19 com Tailwind e pt-BR` |
| T-14.2 | | C2 | Serviços `PedidosApi`, `ClientesApi` em `core/api/`, modelos tipados com os nomes dos schemas e `ProblemaApi`, e `contrato.spec.ts`, que confere os modelos com `docs/contratos/openapi.json` (ADR-0005). Depende da T-11.2 | `frontend/src/app/core/api/*.ts`, `frontend/src/app/core/api/contrato.spec.ts` | `PedidosApi_Listar_EnviaFiltrosNaQueryString`, `Contrato_ModelosDoFrontend_IgualAoOpenApiVersionado` (ADR-0005) (unit) | `npm --prefix frontend test -- --watch=false --browsers=ChromeHeadless` | `feat(frontend): adiciona os clientes HTTP de pedidos e clientes` |
| T-14.3 | | C2 | Componente de listagem com filtros, paginação e filtros na URL | `frontend/src/app/pedidos/pedido-lista.component.ts` (+ template) | `PedidoListaComponent_FiltrosAplicados_AtualizaQueryString` (CA-28), `PedidoListaComponent_Valores_FormataMoedaEDataEmPtBr` (componente) | `npm --prefix frontend test -- --watch=false --browsers=ChromeHeadless` | `feat(frontend): cria a tela de listagem de pedidos com filtros (CA-28)` |
| T-14.4 | | C1 | Estados de carregamento, vazio e erro com "Tentar novamente" | `frontend/src/app/pedidos/pedido-lista.component.*` | `PedidoListaComponent_RespostaVazia_MostraNenhumPedidoEncontrado`, `PedidoListaComponent_FalhaNaApi_MostraErroComTentarNovamente` (CA-29, componente) | `npm --prefix frontend test -- --watch=false --browsers=ChromeHeadless` | `feat(frontend): mostra estados de carregamento, vazio e erro (CA-29)` |
| T-14.5 | | C2 | `Dockerfile` do frontend com Nginx sem root e proxy de `/api` para a API | `frontend/Dockerfile`, `frontend/nginx.conf` | Manual: imagem sobe e `/api/health/live` responde pelo Nginx | `docker build -t joya-frontend frontend` | `feat(frontend): empacota o frontend em Nginx com proxy para a API` |

---

### US-15 — Tela de criação de pedido

| Campo | Valor |
|---|---|
| História | Como **operador comercial**, quero **criar um pedido numa tela com seletores de cliente e produto**, para **registrar a venda e vê-la na listagem**. |
| Prioridade | P1 |
| Dono | frontend |
| Requisitos | RF-06, RNF-08 |
| Regras e critérios | RN-01…RN-08, RN-14; CA-30, CA-31, CA-32; envio de `Idempotency-Key` pela tela (HL-22; ADR-0004) |
| Depende de | US-14, US-06, US-07 |
| Linhas do enunciado | ENU-L037 |

**Critérios de aceite**

```gherkin
Funcionalidade: Criação

  Cenário: CA-30 — Criar e ver na listagem
    Quando escolho um cliente, adiciono 2 unidades de um produto e confirmo
    Então volto à listagem com confirmação de sucesso e o pedido aparece no topo com o total correto

  Cenário: CA-30 com HL-22 — Reenvio não duplica o pedido
    Dado o formulário aberto, que gerou uma Idempotency-Key
    Quando eu confirmo, a requisição falha por rede e eu confirmo de novo
    Então as duas requisições levam a mesma Idempotency-Key

  Cenário: CA-31 — Formulário impede envio inválido
    Quando não há cliente, não há itens ou uma quantidade é inválida
    Então o botão confirmar fica desabilitado e cada campo inválido mostra o motivo

  Cenário: CA-32 — Estoque insuficiente
    Quando a API responde 409
    Então os itens sem estoque ficam destacados com a quantidade disponível, os dados são preservados e os produtos são recarregados
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-15.1 | | C2 | Formulário reativo tipado com seletores e validação (RN-01 a RN-04 no cliente) | `frontend/src/app/pedidos/pedido-form.component.ts`, `frontend/src/app/core/api/produtos-api.ts` | `PedidoFormComponent_SemItens_DesabilitaConfirmar`, `PedidoFormComponent_QuantidadeInvalida_MostraMotivo` (CA-31, componente) | `npm --prefix frontend test -- --watch=false --browsers=ChromeHeadless` | `feat(frontend): cria o formulário tipado de novo pedido (CA-31)` |
| T-15.2 | | C2 | Envio, redirecionamento e mensagem de sucesso. `Idempotency-Key` gerada com `crypto.randomUUID()` ao abrir o formulário e reutilizada em todo reenvio (`PedidosApi.criar`, HL-22, ADR-0004) | `frontend/src/app/pedidos/pedido-form.component.ts`, `frontend/src/app/core/api/pedidos-api.ts`, rotas | `PedidoFormComponent_Confirmar_RedirecionaParaListagemComSucesso` (CA-30), `PedidoFormComponent_Reenvio_ReutilizaIdempotencyKey` (CA-30, RN-14, HL-22) (componente) | `npm --prefix frontend test -- --watch=false --browsers=ChromeHeadless` | `feat(frontend): envia o pedido e volta à listagem (CA-30)` |
| T-15.3 | | C2 | Tratamento de `409` e `422` com Problem Details | `frontend/src/app/pedidos/pedido-form.component.*` | `PedidoFormComponent_Resposta409_DestacaItensEPreservaDados` (CA-32, componente) | `npm --prefix frontend test -- --watch=false --browsers=ChromeHeadless` | `feat(frontend): trata estoque insuficiente sem perder os dados (CA-32)` |

---

### US-16 — Teste end to end com Playwright

| Campo | Valor |
|---|---|
| História | Como **avaliador**, quero **um teste E2E que cria um pedido e o vê na listagem**, para **provar o fluxo de ponta a ponta**. |
| Prioridade | P3 |
| Dono | frontend |
| Requisitos | ENT-19 |
| Regras e critérios | CA-30 |
| Depende de | US-15, US-13 |
| Linhas do enunciado | ENU-L058, ENU-L085 |

**Critérios de aceite**

```gherkin
Funcionalidade: E2E

  Cenário: CA-30 — Fluxo real contra a aplicação no Docker
    Dado a aplicação de pé com docker compose up
    Quando o Playwright cria um pedido pela tela
    Então o pedido aparece no topo da listagem com o valor total correto
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-16.1 | | C2 | Projeto Playwright em `e2e/` e o teste do fluxo de criação | `e2e/package.json`, `e2e/playwright.config.ts`, `e2e/criar-pedido.spec.ts` | `CriarPedido_FluxoCompleto_ApareceNoTopoDaListagem` (E2E) | `npm --prefix e2e test` | `test(e2e): cria um pedido e o vê na listagem com Playwright (CA-30)` |

---

### US-17 — Deploy online

| Campo | Valor |
|---|---|
| História | Como **avaliador**, quero **a aplicação acessível numa URL pública com HTTPS**, para **testar sem rodar nada**. |
| Prioridade | P3 |
| Dono | backend |
| Requisitos | ENT-21, RNF-06 |
| Regras e critérios | HL-04 (VM ≥ 2 GB x86-64, mesmo compose + Caddy) |
| Depende de | US-13 (T-13.1) |
| Linhas do enunciado | ENU-L087 |

**Critérios de aceite**

```gherkin
Funcionalidade: Deploy

  Cenário: URL pública
    Dado o deploy na VM
    Quando abro a URL do README
    Então a tela de pedidos abre via HTTPS e /api/health/ready devolve 200
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-17.1 | | C3 | Override de compose para a VM com Caddy (HTTPS), sem segredos no repositório. C3: deploy (ADR-0001) | `deploy/docker-compose.prod.yml`, `deploy/Caddyfile`, `deploy/README.md` | Manual: URL pública responde `/api/health/ready` 200 | `docker compose -f docker-compose.yml -f deploy/docker-compose.prod.yml config` | `feat(deploy): publica a aplicação numa VM com Caddy e HTTPS` |
| T-17.2 | | C3 | Workflow de CD após merge na `main`, com segredos do GitHub. C3: segurança | `.github/workflows/deploy.yml` | Manual: merge na `main` atualiza a URL | `gh run list --workflow deploy.yml --limit 1` | `ci: atualiza o deploy a cada merge na main` |

---

### US-18 — README e entrega

| Campo | Valor |
|---|---|
| História | Como **avaliador**, quero **um README com os 5 itens exigidos, a configuração do Claude e os links de entrega**, para **avaliar sem procurar informação**. |
| Prioridade | P1 |
| Dono | orquestrador |
| Requisitos | ENT-03…ENT-18, ENT-21, ENT-22, ENT-23 |
| Regras e critérios | HL-03, HL-07, HL-08 |
| Depende de | todas as anteriores (cada seção pode entrar quando a história correspondente fechar) |
| Linhas do enunciado | ENU-L007, ENU-L017 a ENU-L022, ENU-L040, ENU-L072 a ENU-L082, ENU-L087 |

**Critérios de aceite**

```gherkin
Funcionalidade: README

  Cenário: Itens exigidos
    Dado o README.md na raiz
    Quando o avaliador lê o índice
    Então encontra: rodar com Docker, rodar testes, decisões técnicas, uso do Claude, Parte 4, MCP, limitações, links

  Cenário: Comandos do README funcionam
    Dado um clone limpo
    Quando executo os comandos das seções Docker e Testes
    Então todos terminam com sucesso
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-18.1 | | C1 | Seções "Como rodar com Docker" e "Como rodar os testes" | `README.md` | Manual: comandos do README executados num clone limpo | `docker compose up -d && dotnet test backend/Joya.Pedidos.sln` | `docs(readme): explica como rodar o projeto e os testes` |
| T-18.2 | [P] | C2 | Seção da configuração AI-first: rules, hooks, skills, agentes e uso do MCP com exemplos reais | `README.md` | Manual: cada item cita o arquivo em `.claude/` e um uso real registrado no diário | `git ls-files .claude CLAUDE.md .mcp.json` | `docs(readme): documenta rules, hooks, skills e o uso do MCP` |
| T-18.3 | [P] | C2 | Seções "Decisões técnicas" e "Como usei o Claude" (o que funcionou, `HL` e `CG`), com a leitura do histórico de commits | `README.md` | Manual: cada correção citada aponta para um `HL-xx` ou `CG-xx` | `grep -c "CG-\|HL-" README.md` | `docs(readme): registra decisões técnicas e o uso do Claude` |
| T-18.4 | | C2 | Seção da Parte 4: defeitos, investigação, planos e índices | `README.md` | Manual: os defeitos citados batem com os testes `QueryOriginal_*` | `grep -n "Parte 4" README.md` | `docs(readme): explica o debug e a otimização da query (Parte 4)` |
| T-18.5 | [P] | C2 | Limitações (autenticação, risco aceito) e relatório de custo medido (HL-08) | `README.md` | Manual: custo bate com a tabela de execuções do `status.md` | `grep -n "Limitações\|Custo" README.md` | `docs(readme): declara limitações e o custo medido da esteira` |
| T-18.6 | | C1 | Links de entrega: repositório, vídeo e deploy | `README.md` | Manual: os três links abrem sem login | `grep -n "https://" README.md` | `docs(readme): adiciona os links do repositório, do vídeo e do deploy` |
| T-18.7 | [P] | C1 | Roteiro final do vídeo de até 10 minutos | `docs/ia/roteiro-video.md` | Manual: roteiro cronometrado em até 10 minutos | `grep -c "min" docs/ia/roteiro-video.md` | `docs(ia): fecha o roteiro do vídeo de demonstração` |

---

## Ordem de execução

Configuração AI-first e documentos primeiro, depois banco, API e frontend. Os diferenciais (P3) entram cedo quando a HL-04 pede.

| Onda | Tarefas | Pré-requisito | Observação |
|---|---|---|---|
| 0 | T-01.1 → (gate 1, decisões das PROPs) → T-01.2, T-01.3 [P] | — | A configuração `.claude/` já está versionada |
| 1 | T-02.1 → T-02.2 | Onda 0 | Esqueleto |
| 2 | T-03.1, T-03.2 [P] · T-04.1 | Onda 1 | CI logo depois do esqueleto (HL-04) |
| 3 | T-04.2 → T-04.3 → T-04.4 → T-04.5 | T-04.1 | Banco antes da API; libera o MCP |
| 4 | T-03.3 · T-05.1, T-05.2 [P] → T-05.3 | Onda 3 | T-05.3 com os valores do ADR-0002 (HL-12) |
| 5 | T-06.1, T-06.2 [P], T-08.1 [P], T-09.1 [P] → T-06.3 | Onda 4 | Core sem I/O |
| 6 | T-06.4 → T-06.5 → T-06.6 → T-06.7 | Onda 5 | Criação de pedido |
| 7 | T-07.1, T-08.2, T-11.1 (sequenciais no mesmo controller quando necessário) → T-09.2 | T-06.4 | Consultas |
| 8 | T-10.1 → T-10.2 → T-10.3 · T-12.1 → T-12.2 → T-11.2 | Onda 7 | Parte 4 e idempotência; o contrato (T-11.2) fecha a API depois do último endpoint e antes do frontend |
| 9 | T-13.1 · T-17.1 | Onda 8 | Deploy publicado cedo (HL-04) |
| 10 | T-14.1 → T-14.2 → T-14.3 → T-14.4 → T-14.5 | T-07.1, T-08.2, T-11.2 | API antes do frontend; a T-14.2 lê o contrato versionado |
| 11 | T-15.1 → T-15.2 → T-15.3 · T-13.2 · T-03.4 | Onda 10 | Aplicação completa no compose |
| 12 | T-16.1 → T-03.5 · T-17.2 | Onda 11 | Playwright fecha o frontend (HL-04) |
| 13 | T-18.1 … T-18.7 | Todas | README e entrega; seções podem antecipar quando a história fechar |

**Contagem (rodada 2, ajustes da etapa 2).** 18 histórias e 57 tarefas: `C1` 7, `C2` 35, `C3` 15. A rodada 2 criou a T-11.2 (`C2`) e alterou T-03.2, T-04.1, T-04.5, T-05.3, T-06.3, T-06.4, T-07.1, T-11.1, T-14.2 e T-15.2, sem mudar a complexidade de nenhuma.
