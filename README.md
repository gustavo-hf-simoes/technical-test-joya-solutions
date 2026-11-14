# Mini Gestão de Pedidos — Joya Solutions

[![CI](https://github.com/gustavo-hf-simoes/technical-test-joya-solutions/actions/workflows/ci.yml/badge.svg)](https://github.com/gustavo-hf-simoes/technical-test-joya-solutions/actions/workflows/ci.yml)

Cadastro e consulta de pedidos de clientes: .NET 8, Angular 19 e SQL Server 2022, tudo em Docker. O projeto foi construído com o **Claude Code** numa esteira *Spec-Driven Development* com agentes por papel.

## Sumário

1. [Como rodar com Docker](#como-rodar-com-docker)
2. [Como rodar os testes](#como-rodar-os-testes)
3. [Principais decisões técnicas](#principais-decisões-técnicas)
4. [Como usei o Claude](#como-usei-o-claude)
5. [Configuração AI-first](#configuração-ai-first-claude-e-claudemd)
6. [Parte 4: debug da query](#parte-4-debug-da-query)
7. [Limitações](#limitações)
8. [Custo medido da esteira](#custo-medido-da-esteira)
9. [Links de entrega](#links-de-entrega)
10. [Onde está cada coisa](#onde-está-cada-coisa)
11. [Histórico de commits](#histórico-de-commits)

## Como rodar com Docker

Pré-requisito: Docker com Compose v2. Nada mais: não é preciso .NET, Node nem `.env`.

```bash
docker compose up
```

| Serviço | O que faz | Endereço |
|---|---|---|
| `db` | SQL Server 2022, com os dados num volume nomeado | `127.0.0.1:1433` (só no loopback) |
| `db-init` | Contêiner efêmero: aplica `database/*.sql` (schema, índices, logins e carga) e sai com 0. Rodar de novo não duplica a carga | — |
| `api` | API .NET 8 sem root, conectada como `joya_app` (nunca `sa`) | http://localhost:8080/swagger |
| `frontend` | Angular servido por Nginx sem root, com proxy de `/api` para a API | **http://localhost:4200** |

A ordem é garantida por `depends_on` com healthcheck: o `db` só fica saudável com todos os bancos `ONLINE`, o `db-init` espera o `db`, a `api` espera o `db-init` terminar e o `frontend` espera a `api` responder em `/health/ready`.

```bash
# Depois de mudar o código: reconstrói as imagens
docker compose up --build

# Só o banco (para rodar a API pelo dotnet ou usar o MCP)
docker compose up -d db db-init

# Recriar o banco do zero (APAGA o volume de dados)
docker compose down -v
```

As senhas do compose são valores de **desenvolvimento**, sobrescrevíveis por variável de ambiente (modelo em `.env.example`).

## Como rodar os testes

Execute a partir da raiz do repositório.

| Suíte | Comando | O que exige | Quantidade |
|---|---|---|---|
| Unitários (regras, validações, arquitetura) | `dotnet test backend/tests/Joya.Pedidos.UnitTests` | .NET 8 SDK | 109 |
| Integração (endpoints contra SQL Server real) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "Categoria!=Medicao"` | .NET 8 SDK e Docker em execução | 125 |
| Medição da Parte 4 (carga de 800 mil linhas, 2 a 3 min) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "Categoria=Medicao"` | Os mesmos | 1 |
| Frontend (unitários) | `npm --prefix frontend ci` e depois `npm --prefix frontend test -- --watch=false --browsers=ChromeHeadless` | Node 22 e Chrome | 24 |

- **Integração:** o Testcontainers sobe um SQL Server 2022 descartável e aplica os **mesmos** scripts `database/*.sql` do compose, pelo mesmo `init.sh` ([ADR-0003](docs/adr/0003-banco-real-nos-testes-de-integracao.md)). O Respawn limpa as tabelas entre os testes. Não há banco em memória: o schema, os índices, a baixa atômica de estoque e os logins são testados de verdade.
- **Arquitetura:** os testes unitários incluem regras de dependência (ArchUnitNET). Exemplos: o `Core` não referencia EF, ASP.NET nem SqlClient; DTOs são `sealed record`; métodos `Async` recebem `CancellationToken` por último.
- **Nomes:** os testes seguem `Metodo_Cenario_ResultadoEsperado` e, quando provam um critério da spec, levam o `CA-xx` no traço. A [matriz de rastreabilidade](docs/esteira/rastreabilidade.md) liga cada linha do enunciado ao teste e ao commit, e o CI recusa teste ou hash inexistente.

```bash
# Build sem nenhum aviso (TreatWarningsAsErrors) e formatação
dotnet build backend/Joya.Pedidos.sln
dotnet format backend/Joya.Pedidos.sln --verify-no-changes

# Lint e build do frontend
npm --prefix frontend run lint
npm --prefix frontend run build
```

## Principais decisões técnicas

A lista completa, com alternativas descartadas, está em [docs/plan.md](docs/plan.md) (decisões `D1` a `D16`) e nos ADRs.

| Decisão | Por quê |
|---|---|
| **Clean Architecture enxuta em 3 projetos** (`Core`, `Infrastructure`, `Api`), organizada por feature (D1, D2) | O domínio e os casos de uso não dependem de EF nem de HTTP. A regra é verificada por teste de arquitetura, não só por convenção |
| **Services simples, sem MediatR e sem AutoMapper** (D3) | Para poucos endpoints, um mediador é indireção pura; os dois pacotes passaram a ter licença comercial |
| **`Result<T>` para falha de negócio**, exceção só para o inesperado (D4) | Estoque insuficiente é fluxo normal: tipado, testável e traduzido para Problem Details em um lugar só |
| **Baixa de estoque por `UPDATE` condicional com `OUTPUT`**, numa transação (D5, D7) | Verificação e baixa na mesma instrução: dois pedidos simultâneos não vendem o mesmo item. O preço do item vem do banco nesse momento, nunca do corpo da requisição |
| **Unidade de trabalho sem retentativa automática** ([ADR-0004](docs/adr/0004-unidade-de-trabalho-sem-retentativa.md), `HL-23`) | Repetir uma transação de pedido sem idempotência pode criar pedido em dobro. A IA propôs retentativa; eu discordei |
| **Schema só em `database/*.sql`, EF Core mapeando sem migrations** (D6) | O enunciado pede scripts; duas fontes de schema divergem |
| **Testes de integração com Testcontainers e Respawn** (D10, [ADR-0003](docs/adr/0003-banco-real-nos-testes-de-integracao.md)) | Testa o schema, os índices e a concorrência reais |
| **xUnit, NSubstitute e Shouldly** (D11) | Sem FluentAssertions 8 nem Moq, por licença e telemetria |
| **Angular 19 standalone com Signals, OnPush, Reactive Forms tipados e Tailwind** (D12, D13) | Estado de tela simples e previsível; criação de pedido em rota própria (`/pedidos/novo`) |
| **Nginx do frontend faz proxy de `/api`** (D14) | Mesma origem: sem CORS e sem URL de API embutida no bundle |
| **`db-init` efêmero para a carga** (D15) | A imagem do SQL Server não executa scripts de inicialização |
| **Menor privilégio no banco** (D16) | A API usa `joya_app`, e o MCP usa `claude_mcp`, só leitura. A garantia é a permissão do login, e não um filtro de palavras-chave (`LoginMcp_TentaEscrever_PermissaoNegada`) |
| **Limites contra abuso** ([ADR-0002](docs/adr/0002-limites-contra-abuso.md)) | Tamanho de corpo, quantidade de itens, paginação máxima e *rate limit* na criação de pedido |
| **Contrato OpenAPI versionado** ([ADR-0005](docs/adr/0005-contrato-openapi-versionado.md)) | O frontend consome um contrato exportado, e não a API "como estiver" |
| **Datas em UTC com `TimeProvider`** e filtro de período sempre semiaberto sobre a coluna | Testável, e o índice por data continua utilizável (a mesma lição da Parte 4) |

## Como usei o Claude

### O fluxo

1. **Spec antes de código.** Com o Claude em *plan mode*, escrevi [docs/spec.md](docs/spec.md) (regras `RN-xx`, contratos e critérios `CA-xx`) e [docs/plan.md](docs/plan.md). Cada ponto em que discordei ou ajustei o plano da IA virou uma linha `HL-xx` na seção *Human-in-the-Loop*, com o motivo. São 27 até agora.
2. **Esteira por etapas, não uma conversa só** (`HL-01`). A sessão principal é o orquestrador (skill `/esteira`) e delega cada etapa a um agente com papel e caminhos próprios: `product-owner` → `arquiteto` → `backend` / `frontend` → `ciberseguranca` → `qa`. O orquestrador roda os gates pessoalmente: o "passou" do agente não vale como evidência.
3. **Modelo por complexidade** ([ADR-0001](docs/adr/0001-roteamento-de-modelos.md)). Cada tarefa do backlog tem `C1`/`C2`/`C3`, e isso decide entre Haiku, Sonnet e Opus. O Opus ficou para as tarefas críticas: a baixa atômica de estoque, o relatório e a Parte 4.
4. **Tudo auditável.** O [diário](docs/ia/diario.md) registra cada etapa (pedido, saída, modelo, gate). Os [prompts](docs/ia/prompts.md) do desenvolvedor estão copiados sem edição. A tabela *Execuções de agentes* em [status.md](docs/esteira/status.md) mede modelo, tentativas e tokens.

### O que funcionou bem

- **Spec como fonte de verdade.** Quando um agente encontrou ambiguidade, devolveu uma `PROP-xx` em vez de inventar. Foram 13 PROPs, todas decididas por mim (`HL-10` a `HL-22`).
- **Limites por hook, e não só por prompt.** O `guard-paths.sh` impede que o agente de frontend escreva no backend; o `guard-bash.sh` bloqueia `git` que reescreve histórico e leitura de `.env`.
- **Gate independente.** O build com `TreatWarningsAsErrors`, a busca por `default!`/`#pragma` e o validador de rastreabilidade pegaram defeitos que os testes deixavam passar (ver a tabela abaixo).
- **Backend e frontend em paralelo** em pastas disjuntas (`HL-25`), com uma delegação por história.
- **Testes contra banco real.** Pegaram o erro de collation (`CG-04`) e a corrida do `db-init` com a recuperação do banco, que um banco em memória esconderia.

### O que precisei corrigir no que a IA gerou

| Registro | O que a IA fez | Como foi pego | Correção |
|---|---|---|---|
| `CG-01` | Afirmou que subagentes não podem criar subagentes, e usou isso para justificar o desenho | Consulta à documentação oficial | Premissa corrigida; o desenho ficou, com o motivo certo |
| `CG-02` | Afirmou que o git não estava instalado; ele estava, só a pasta não era repositório | A própria saída do comando | Regra: afirmação sobre o ambiente só com a saída que a comprova |
| `CG-03` | `default!` (*null-forgiving*) num teste, contra a regra de ouro 1. Build, testes e format passavam | Busca por silenciamento no gate | Devolvido ao agente; a busca passou a fazer parte de todo gate. O mesmo erro voltou na T-08.2 e foi pego da mesma forma |
| `CG-04` | Consultas de catálogo misturando collations (erro 468) | Testes de integração contra SQL Server real | Collation explícita nas consultas |
| `CG-05` | Senha padrão do MCP contendo o nome do login, que o SQL Server recusa (erro 33064) | Falha ao subir o banco | Senha trocada. O MCP ainda falhou com `AUTH_FAILED` nesta sessão (ver [Limitações](#limitações)) |
| `CG-06` | `title` do Problem Details genérico, fora dos exemplos da spec 5.2 | Leitura do código pelo orquestrador, com os testes verdes | Títulos alinhados à spec |
| Gate da US-13 | O `db-init` rodava enquanto o banco ainda se recuperava (erro 904) | Subida do compose no gate | Healthcheck do banco exige todos os bancos `ONLINE`; o `init.sh` espera o banco abrir |

E onde **eu discordei da IA** antes de virar código, entre outros:
- retentativa automática de transação (`HL-23`);
- a ordem e o escopo das entregas sob prazo (`HL-25` a `HL-27`).

Também aceitei, como exceção registrada (`HL-24`), remover o *gateway* local de modelos que derrubava as delegações.

## Configuração AI-first (`.claude/` e `CLAUDE.md`)

| Item | Arquivo | Uso real |
|---|---|---|
| **Rules** | [CLAUDE.md](CLAUDE.md) | Arquitetura, 12 regras de ouro, nomenclatura, comandos de build e teste, regras de commit. Todo agente parte dele |
| **Hook de qualidade** | [.claude/hooks/post-edit.sh](.claude/hooks/post-edit.sh) | Depois de cada edição em arquivo do backend, roda `dotnet build` e os testes unitários e devolve a falha ao Claude. Disparou em todas as histórias de backend |
| **Hooks de limite** | [guard-paths.sh](.claude/hooks/guard-paths.sh), [guard-bash.sh](.claude/hooks/guard-bash.sh) | `PreToolUse` declarados no frontmatter de cada agente: escrita fora dos caminhos do agente, `git` que altera histórico, leitura de `.env`, `curl \| sh` |
| **Skill `/new-endpoint`** | [.claude/skills/new-endpoint/](.claude/skills/new-endpoint/) | Passo a passo do endpoint no padrão do projeto (Request → Validator → Service → Controller → testes). Usada na T-06.6 e na T-07.1 |
| **Skill `/esteira`** | [.claude/skills/esteira/](.claude/skills/esteira/) | O orquestrador: delega, roda os gates, roteia achados, pede decisões e propõe commits |
| **Agentes** | [.claude/agents/](.claude/agents/) | `product-owner`, `arquiteto`, `backend`, `frontend`, `ciberseguranca`, `qa` e `verificador`, cada um com modelo, esforço, ferramentas e hooks no frontmatter |
| **Skills vendorizadas** | [VENDORED.md](.claude/skills/VENDORED.md) | `dotnet-webapi`, `optimizing-ef-core-queries`, `testcontainers-dotnet`, `angular-developer`, `security-review` e as de qualidade de testes, com procedência e auditoria |
| **MCP `mssql`** | [.mcp.json](.mcp.json) | Acesso **somente leitura** ao SQL Server pelo login `claude_mcp` (`db_datareader`, `SHOWPLAN`, `VIEW DATABASE STATE`), para conferir o schema real e analisar planos |

**Sobre o MCP.** O login `claude_mcp` não consegue escrever, e isso é provado por teste (`LoginMcp_TentaEscrever_PermissaoNegada`). Na Parte 4, a conexão do MCP falhou com `AUTH_FAILED` (`CG-05`). Então os planos foram colhidos **com o mesmo login e os mesmos comandos** (`SHOWPLAN_XML`, `STATISTICS XML`, `STATISTICS IO`) dentro de um teste de integração. O resultado é reproduzível por qualquer pessoa com Docker, e não depende da sessão do Claude. O passo a passo para usar o MCP contra o banco do compose está na seção 10 de [investigacao.md](database/parte4/investigacao.md).

## Parte 4: debug da query

A query do enunciado deveria trazer o ranking dos clientes que mais compraram em 2026, mas estava lenta e errada. A investigação completa, com os planos antes e depois, está em [database/parte4/investigacao.md](database/parte4/investigacao.md).

| Arquivo | Papel |
|---|---|
| [01-query-original.sql](database/parte4/01-query-original.sql) | A query do enunciado, sem alteração |
| [02-query-corrigida.sql](database/parte4/02-query-corrigida.sql) | **A correção entregue** |
| [03-variante-valor-total.sql](database/parte4/03-variante-valor-total.sql) | Variante para comparação: soma o `ValorTotal` do pedido |
| [00-carga-volume.sql](database/parte4/00-carga-volume.sql) | Carga determinística para medir: 5 mil clientes, 200 mil pedidos, 600 mil itens |

### Os quatro defeitos

| # | Defeito | Efeito | Correção | Teste que prova |
|---|---|---|---|---|
| 1 | `GROUP BY c.Nome` | Clientes homônimos viram uma linha só, com os totais somados | `GROUP BY c.Id, c.Nome`, com o `ClienteId` na saída | `QueryOriginal_ClientesHomonimos_SomaIndevidamente`, `QueryCorrigida_ClientesHomonimos_UmaLinhaPorCliente` |
| 2 | `ORDER BY Total` | Ordem crescente: o "ranking" começa por quem **menos** comprou, e não há desempate | `ORDER BY Total DESC, c.Id` | `QueryOriginal_Ordenacao_RetornaMenorTotalPrimeiro`, `QueryCorrigida_Empate_OrdenaPorMenorClienteId` |
| 3 | `YEAR(p.DataPedido) = 2026` | Função sobre a coluna: o índice por data não é usado e todos os pedidos são lidos | Intervalo semiaberto: `>= '2026-01-01' AND < '2027-01-01'` | `QueryOriginal_Plano_LeTodosOsPedidosComFuncaoSobreDataPedido`, `QueryCorrigida_FronteirasDoAno_ContaSoPedidosDe2026` |
| 4 | `LEFT JOIN` com filtro no `WHERE` sobre a tabela da direita | É um `INNER JOIN` disfarçado. No plano medido, o otimizador manteve a junção externa e juntou os 200 mil pedidos antes de filtrar o ano | `INNER JOIN`, começando por `Pedidos`, o lado filtrado | Plano na seção 4 da investigação |

A correção também é provada contra a API: os 5 primeiros da query corrigida coincidem com `GET /relatorios/top-clientes` de 2026 (`QueryCorrigida_Ranking2026_CoincideComTopClientes`, CA-33).

### Como investiguei e o que medi

Os planos e as leituras foram colhidos com o login de leitura `claude_mcp`. Os tempos vêm de `sys.dm_exec_query_stats`, em médias de 10 execuções com cache quente, sobre 200 mil pedidos.

| Consulta | Leituras lógicas | CPU (ms) | Tempo (ms) | Paralelismo | Acesso a `Pedidos` |
|---|---|---|---|---|---|
| Original | 4 779 | 1 229 | 715 | 3 | Scan dos 200 mil pedidos |
| **Corrigida (`02`)** | **3 589** | **351** | **352** | 1 | Seek em `IX_Pedidos_DataPedido` (−78 % de leituras em `Pedidos`) |
| Variante `ValorTotal` (`03`) | 311 | 85 | 85 | 1 | O mesmo seek, sem ler `ItensPedido` |

### Índices

**Nenhum índice novo foi necessário.** Dois índices existentes cobrem a consulta:
- `IX_Pedidos_DataPedido (DataPedido DESC, Id DESC) INCLUDE (ClienteId, ValorTotal)` cobre `Pedidos`;
- a chave clusterizada de `ItensPedido` (`PedidoId, ProdutoId`) cobre os itens.

O problema nunca foi a falta de índice, e sim a função sobre a coluna, que impedia o uso dele.

**Por que a entrega é a `02`, e não a `03`.** A `03` é cerca de 4 vezes mais rápida e é a mesma forma que a API usa no top-clientes. Mas a query do enunciado soma os itens, e a `02` mantém essa semântica. A `03` só é equivalente porque `ValorTotal` é gravado na mesma transação que os itens, e isso é uma regra da aplicação, não do banco. A comparação está na seção 8 da investigação.

## Limitações

- **Sem autenticação nem autorização**, por decisão de escopo (spec 2.2). Em produção, a API ficaria atrás de um gateway com OIDC.
- **Sem CRUD de clientes e produtos, e sem edição ou cancelamento de pedido** (spec 2.2). Os cadastros vêm da carga inicial.
- **MCP `mssql`:** a conexão falhou com `AUTH_FAILED` na sessão da Parte 4 (`CG-05`). As evidências foram colhidas com o mesmo login, por teste.
- **Medição da Parte 4 fora do gate** (`HL-27`). Ela leva de 2 a 3 minutos e roda sob demanda. Uma regressão de desempenho na query só aparece se alguém rodá-la.
- **Corpo *chunked* acima do limite:** o 413 foi testado com `Content-Length`, mas não com corpo *chunked* contra o Kestrel real.
- **Senhas padrão do compose** são de desenvolvimento. Em produção, viriam de um cofre de segredos. O mesmo vale para o `TrustServerCertificate=True` da API e para o MCP, que conectam sem validar o certificado do SQL Server local (`HL-29`).
- **Angular 19 e .NET 8 fora de suporte** (`SEC-01`, `HL-29`). A linha 19 do Angular não recebe mais correções, e os advisories publicados só são corrigidos na 22; nenhum deles é explorável no código atual. O .NET 8 saiu do suporte em 2026-11-10. As duas atualizações de versão major ficam para depois da entrega.
- **Permissões do `joya_app`:** o login da API tem `UPDATE` e `INSERT` nas tabelas que a criação de pedido altera, conforme a spec 6.5 (`SEC-05`, aceito na `HL-29`).
- **Limite de taxa só no `POST /pedidos`** e relatório sem período máximo ([ADR-0002](docs/adr/0002-limites-contra-abuso.md)). Uma consulta de período longo é cara, mas não altera dados (`HL-29`).
- **Histórias opcionais** (E2E com Playwright, contrato OpenAPI conferido no CI, deploy e idempotência) entram na ordem da `HL-26`, conforme o prazo. O estado de cada uma está em [status.md](docs/esteira/status.md).

## Custo medido da esteira

Tokens por delegação a agente, lidos do resultado de cada chamada e registrados na tabela *Execuções de agentes* de [status.md](docs/esteira/status.md):

| Modelo | Delegações | Tokens | Onde |
|---|---|---|---|
| Opus | 11 | 1.156.396 | Requisitos, arquitetura e as tarefas `C3`: baixa atômica, relatório, Parte 4 |
| Sonnet | 12 | 978.121 | As tarefas `C2` do backend e todo o frontend |
| **Total** | **23** | **2.134.517** | |

- **Escalonamento de modelo:** nenhum. Nenhuma tarefa precisou subir de modelo depois de falhar no gate (0 de 23); as falhas foram corrigidas no mesmo nível.
- **Fora da conta:** a sessão do orquestrador, que é a conversa principal, e as delegações perdidas por erro de ambiente (`HL-24`).
- **Lição de custo** (`HL-27`): a US-10 levou 41 min, e 21,5 deles foram teste esperando a carga de 800 mil linhas. Daí a medição sair do gate e os agentes rodarem só os próprios testes.

## Links de entrega

| O quê | Link |
|---|---|
| Repositório | https://github.com/gustavo-hf-simoes/technical-test-joya-solutions |
| Vídeo (até 10 min) | A publicar. Roteiro em [docs/ia/roteiro-video.md](docs/ia/roteiro-video.md) |
| Aplicação publicada | A publicar (US-17) |

## Onde está cada coisa

| Para entender | Leia |
|---|---|
| O que se pede (texto literal da vaga, com linhas numeradas) | [docs/requisitos/enunciado.md](docs/requisitos/enunciado.md) |
| O que é construído: regras, contratos, modelo de dados, critérios de aceite | [docs/spec.md](docs/spec.md) |
| O plano da IA e o que o desenvolvedor mudou e por quê (`HL-xx`) | [docs/plan.md](docs/plan.md) |
| Qual teste e qual commit atendem cada linha do enunciado (validado no CI) | [docs/esteira/rastreabilidade.md](docs/esteira/rastreabilidade.md) |
| Como é construído: a esteira, os agentes e os gates | [docs/esteira/README.md](docs/esteira/README.md) |
| Como o custo de IA é controlado (modelo por complexidade) | [docs/adr/0001-roteamento-de-modelos.md](docs/adr/0001-roteamento-de-modelos.md) |
| A linha do tempo do trabalho com a IA, com datas, prompts e erros corrigidos | [docs/ia/diario.md](docs/ia/diario.md) |
| Convenções para a IA (rules) | [CLAUDE.md](CLAUDE.md) e [.claude/](.claude/) |

## Histórico de commits

A spec, o plano e a configuração `.claude/` foram escritos antes do `git init`. Para não juntar tudo num commit único, eles entraram como **commits lógicos de bootstrap**, na ordem real em que foram criados. Os commits têm a data real em que foram feitos: nenhuma data foi retroagida. A evolução de cada documento antes do repositório existir está registrada no [diário](docs/ia/diario.md) (`HL-06`).

A partir daí, cada história do backlog tem uma branch, com um commit por tarefa. A branch é integrada por PR com merge commit, sem squash e sem rebase, e o CI roda em cada PR.
