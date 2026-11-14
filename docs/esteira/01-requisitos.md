# Relatório da etapa 1 — Requisitos e backlog (`product-owner`)

> Modelo: [templates/relatorio-etapa.md](templates/relatorio-etapa.md). Rodadas novas são acrescentadas; as antigas não são reescritas.
> Fonte primária: [enunciado.md](../requisitos/enunciado.md), citado como `ENU-Lxxx`. Fonte técnica: [spec.md](../spec.md). Decisões humanas: [plan.md](../plan.md), seção Human-in-the-Loop (`HL-01` a `HL-09`).

## Rodada 1 — 2026-11-13 — DESENHO

### Convenções desta etapa

- **IDs de RNF.** A spec já define `RNF-01` a `RNF-07` (§7). Este relatório **reusa esses IDs com o mesmo sentido**, para não criar dois espaços de nomes. Os novos começam em `RNF-08`. A ordem de apresentação da seção 3 segue o pedido da esteira, e não a numeração.
- **Classe.** `Obrigatório` = o enunciado exige. `Diferencial` = o enunciado diz "não obrigatório" ou "diferencial". `Contexto` = título, cabeçalho ou texto que não pede nada verificável.
- **"Por exemplo".** Quando o enunciado usa "ex." ou "por exemplo", o trecho é sugestão. A escolha feita fica registrada na coluna "Requisito derivado".

---

## 1. Matriz de cobertura do enunciado

Uma linha por frase verificável. Linhas em branco do enunciado foram omitidas. A matriz oficial, validada no CI, está em [rastreabilidade.md](rastreabilidade.md).

| ENU | Trecho literal (curto) | Classe | Requisito | Onde a spec cobre | Lacuna? |
|---|---|---|---|---|---|
| ENU-L001 | "TESTE TÉCNICO: MINI GESTÃO DE PEDIDOS" | Contexto | — | Cabeçalho da spec | Não |
| ENU-L003 | "Contexto" | Contexto | — | — | Não |
| ENU-L005 | "aplicação de cadastro e consulta de Pedidos de Clientes" | Contexto | (realizado por RF-01 a RF-06) | §1, §2.1 | Não |
| ENU-L005 | "como você trabalha com IA para entregar uma feature completa, bem planejada, testada" | Contexto | (avaliado via ENT-13, ENT-23) | — | Não |
| ENU-L007 | "O uso do Claude Code é obrigatório e faz parte da avaliação" | Obrigatório | ENT-23 | — (processo; `CLAUDE.md`, `docs/ia/`) | Não |
| ENU-L009 | "Parte 1 — Especificação e planejamento (SDD)" | Contexto | — | — | Não |
| ENU-L011 | "Antes de codar, crie na raiz do repositório" | Obrigatório | ENT-01, ENT-02 | Spec "nenhum código de implementação escrito" | Não. Evidência: ordem dos commits |
| ENU-L013 | "docs/spec.md: … objetivo, regras de negócio, endpoints, modelo de dados e critérios de aceite" | Obrigatório | ENT-01 | §1, §4, §5, §6, §8 | Parcial: Parte 4 e endpoints derivados sem `CA` (PROP-01, PROP-02) |
| ENU-L014 | "docs/plan.md: … plan mode, junto com o que você ajustou ou discordou dele e por quê" | Obrigatório | ENT-02 | `plan.md` + seção HL | Não |
| ENU-L015 | "Parte 2 — Configuração AI First" | Contexto | — | — | Não |
| ENU-L017 | "Inclua a pasta .claude/ e o arquivo CLAUDE.md versionados" | Obrigatório | ENT-07 | — | Não (já versionados) |
| ENU-L019 | "Rules: um CLAUDE.md com … arquitetura, padrões de nomenclatura, como rodar build e testes" | Obrigatório | ENT-03 | — | Não |
| ENU-L020 | "Hook: pelo menos um hook útil (ex.: rodar dotnet build …)" | Obrigatório | ENT-04 | — | Não. Escolha: `post-edit.sh` roda build e testes unitários |
| ENU-L021 | "Skill: pelo menos uma skill customizada (ex.: criar novo endpoint …)" | Obrigatório | ENT-05 | — | Não. Escolha: `/new-endpoint` (e `/esteira`) |
| ENU-L022 | "MCP: conectar o Claude ao SQL Server por meio de um MCP" | Obrigatório | ENT-06 | §6.5 (login `claude_mcp`) | Não |
| ENU-L022 | "explicar no README como ele foi usado durante o desenvolvimento" | Obrigatório | ENT-06 | — | Não (README é tarefa de entrega) |
| ENU-L023 | "Parte 3 — Implementação end to end" | Contexto | — | — | Não |
| ENU-L025 | "Banco de dados (SQL Server)" | Obrigatório | RNF-09 | Cabeçalho (stack) | Não |
| ENU-L027 | "Tabelas Clientes, Produtos, Pedidos e ItensPedido" | Obrigatório | RF-07 | §6.1, §6.2 | Não |
| ENU-L027 | "com chaves primárias, estrangeiras e índices adequados" | Obrigatório | RF-07, RNF-02 | §6.2, §6.3 | Não |
| ENU-L028 | "Scripts de criação e de carga de dados de exemplo na pasta database/" | Obrigatório | RF-08 | §6 ("scripts em `database/`"), RNF-04 | Não |
| ENU-L029 | "Backend (.NET 8 ou superior)" | Obrigatório | RNF-09 | Cabeçalho (.NET 8) | Não |
| ENU-L031 | "POST /pedidos: cria um pedido com itens" | Obrigatório | RF-01 | §5.2, RN-01…04, RN-09…13, CA-01 | Não |
| ENU-L031 | "validando se o cliente existe" | Obrigatório | RF-01 | RN-05, CA-05 | Não |
| ENU-L031 | "e se há estoque suficiente" | Obrigatório | RF-01, RNF-01 | RN-08, §4.3, CA-08, CA-09, CA-10 | Não |
| ENU-L032 | "GET /pedidos: lista os pedidos" | Obrigatório | RF-02 | §5.3, RN-16, CA-15 | Não |
| ENU-L032 | "com filtros por cliente e período" | Obrigatório | RF-02 | RN-15, RN-16, CA-16, CA-17, CA-18 | Não |
| ENU-L032 | "com paginação" | Obrigatório | RF-02 | RN-16, CA-15, CA-18, CA-19 | Não |
| ENU-L033 | "GET /relatorios/top-clientes: retorna os 5 clientes com maior valor total de pedidos" | Obrigatório | RF-03 | §5.4, RN-17, CA-21, CA-24, CA-25, CA-27 | Não |
| ENU-L033 | "no período informado" | Obrigatório | RF-03 | RN-15, RN-17, CA-22, CA-26 | Não |
| ENU-L034 | "Frontend (React ou Angular, com TypeScript)" | Obrigatório | RNF-09 | Cabeçalho (Angular 19) | Não. Escolha: Angular |
| ENU-L036 | "Tela de listagem de pedidos com filtros" | Obrigatório | RF-05 | §2.1 E6, CA-28, CA-29 | Não |
| ENU-L037 | "Tela ou modal de criação de pedido" | Obrigatório | RF-06 | §2.1 E6, CA-30, CA-31, CA-32 | Não. Escolha: tela (rota própria), conforme CA-30 |
| ENU-L038 | "Parte 4 — Debug de query" | Contexto | — | — | Não |
| ENU-L040 | "deveria retornar o ranking dos clientes que mais compraram em 2026" | Obrigatório | RF-04 | §9 (linha Parte 4), RN-15, RN-17 | **Sim**: sem RN/CA próprio para a query corrigida (PROP-01) |
| ENU-L040 | "Corrija, otimize" | Obrigatório | RF-04, RNF-02 | §6.3 | Sim (PROP-01) |
| ENU-L040 | "explique no README o que estava errado e como você investigou (plano de execução, índices etc.)" | Obrigatório | ENT-14 | §9 ("README") | Não |
| ENU-L042–L048 | Query original (`LEFT JOIN`, `YEAR()`, `GROUP BY c.Nome`, `ORDER BY Total`) | Obrigatório | RF-04 | CA-22, CA-23 (defeitos equivalentes) | Sim (PROP-01) |
| ENU-L050 | "Parte 5 — Testes" | Contexto | — | — | Não |
| ENU-L052 | "Obrigatórios" | Contexto | — | — | Não |
| ENU-L054 | "Testes unitários no backend … regras de negócio e validações do pedido" | Obrigatório | RNF-07 | RNF-07, §9 | Não |
| ENU-L054 | "(cliente inexistente, estoque insuficiente, pedido sem itens etc.)" | Obrigatório | RNF-07 | CA-05, CA-08, CA-02 | Não |
| ENU-L055 | "Testes de integração no backend, exercitando os endpoints contra um banco SQL Server real" | Obrigatório | RNF-07 | RNF-07 | Não. Restrição técnica para o arquiteto: banco real, sem *in-memory* |
| ENU-L055 | "(ex.: via Testcontainers ou o SQL Server do próprio Docker Compose)" | Contexto | RNF-07 (escolha: Testcontainers, `CLAUDE.md`) | — | Não |
| ENU-L056 | "Diferencial (não obrigatório)" | Contexto | — | — | Não |
| ENU-L058 | "Testes end to end com Playwright … criar um pedido e vê-lo na listagem" | Diferencial | ENT-19 | CA-30 | Não |
| ENU-L059 | "Parte 6 — Docker" | Contexto | — | — | Não |
| ENU-L061 | "O projeto deve rodar inteiramente no Docker" | Obrigatório | RNF-04 | RNF-04 | Não |
| ENU-L061 | "docker-compose.yml na raiz" | Obrigatório | RNF-04 | — | Não |
| ENU-L063 | "SQL Server (com os scripts de criação e carga executados automaticamente)" | Obrigatório | RNF-04, RF-08 | RNF-04 ("idempotentes") | Não |
| ENU-L064 | "Backend .NET" | Obrigatório | RNF-04 | RNF-04 | Não |
| ENU-L065 | "Frontend" | Obrigatório | RNF-04 | RNF-04 | Não |
| ENU-L066 | "a aplicação completa deve subir com um único comando" | Obrigatório | RNF-04 | RNF-04 | Ambiguidade leve com `CLAUDE.md` (PROP-06) |
| ENU-L068 | "docker compose up" (por exemplo) | Obrigatório | RNF-04 | RNF-04 | PROP-06 |
| ENU-L070 | "Requisitos obrigatórios de entrega" | Contexto | — | — | Não |
| ENU-L072 | "Repositório no GitHub, com o link enviado por e-mail" | Obrigatório | ENT-08 | — | Não |
| ENU-L073 | "README na raiz contendo:" | Obrigatório | ENT-09 | — | Não |
| ENU-L074 | "Como rodar o projeto com Docker" | Obrigatório | ENT-10 | — | Não |
| ENU-L075 | "Como rodar os testes (unitários e de integração)" | Obrigatório | ENT-11 | — | Não |
| ENU-L076 | "Principais decisões técnicas" | Obrigatório | ENT-12 | `plan.md` D-xx, ADRs | Não |
| ENU-L077 | "Como você usou o Claude: o que funcionou bem e o que você precisou corrigir" | Obrigatório | ENT-13 | `plan.md` HL/CG | Não |
| ENU-L078 | "A explicação da Parte 4 (debug da query)" | Obrigatório | ENT-14 | — | Não |
| ENU-L079 | "Projeto rodando no Docker conforme a Parte 6" | Obrigatório | ENT-15 | RNF-04 | Não |
| ENU-L080 | "Testes unitários e de integração conforme a Parte 5" | Obrigatório | ENT-16 | RNF-07 | Não |
| ENU-L081 | "Histórico de commits preservado" | Obrigatório | ENT-17 | — (HL-06) | Não |
| ENU-L081 | "prefira commits pequenos, frequentes e com mensagens claras" | Obrigatório | ENT-17 | — (1 tarefa = 1 commit) | Não |
| ENU-L081 | "Não faça squash nem reescreva o histórico" | Obrigatório | ENT-17 | — (`guard-bash.sh`, `main` protegida) | Não |
| ENU-L082 | "Vídeo curto (até 10 minutos) … fluxo de trabalho com o Claude Code e explicando o código" | Obrigatório | ENT-18 | — (HL-07, roteiro) | Não |
| ENU-L083 | "Diferenciais (não obrigatórios, mas contam pontos)" | Contexto | — | — | Não |
| ENU-L085 | "Testes end to end com Playwright" | Diferencial | ENT-19 | CA-30 | Não |
| ENU-L086 | "Pipeline de GitHub Actions (build e execução dos testes a cada push ou pull request)" | Diferencial | ENT-20 | §9 ("job de CI") | Não |
| ENU-L087 | "Deploy online da aplicação, com o link informado no README" | Diferencial | ENT-21 | — (HL-04) | Não |
| ENU-L088–L100 | Tabela de critérios de avaliação e pesos | Contexto | (ver seção 8) | — | Não |

---

## 2. Requisitos funcionais

| ID | Ator | Comportamento | Regras | Critérios de aceite | ENU |
|---|---|---|---|---|---|
| **RF-01** | Operador comercial (via API ou tela) | Criar um pedido com itens. Valida a estrutura, a existência do cliente e o estoque. Debita o estoque de forma atômica e grava o preço praticado. | RN-01…RN-13 | CA-01…CA-10, CA-12, CA-14 | L031, L054 |
| **RF-02** | Operador comercial | Listar pedidos com filtros opcionais por cliente e período, paginados, do mais recente para o mais antigo. | RN-15, RN-16 | CA-15…CA-20 | L032 |
| **RF-03** | Gestor comercial | Obter os 5 clientes com maior valor total no período informado (obrigatório). | RN-15, RN-17 | CA-21…CA-27 | L033 |
| **RF-04** | Desenvolvedor / DBA | Entregar a query da Parte 4 corrigida e otimizada em `database/`, com a investigação documentada (plano de execução antes e depois). | RN-15, RN-17 | CA-22, CA-23; **CA-33 proposto** (PROP-01) | L040–L048 |
| **RF-05** | Operador comercial | Tela de listagem de pedidos com filtro de cliente e período, paginação e estados de carregamento, vazio e erro. | RN-15, RN-16 | CA-28, CA-29 | L036 |
| **RF-06** | Operador comercial | Tela de criação de pedido: escolher cliente, adicionar itens, confirmar e voltar à listagem; tratar `409` sem perder o que foi digitado. | RN-01…RN-08 | CA-30, CA-31, CA-32 | L037 |
| **RF-07** | Sistema (banco) | Tabelas `Clientes`, `Produtos`, `Pedidos` e `ItensPedido` com PKs, FKs, constraints e índices da spec. | RN-03, RN-08 (CHECK) | RNF-02; teste de schema (novo, sem CA) | L027 |
| **RF-08** | Sistema (banco) | Scripts de criação e de carga em `database/`, idempotentes. A carga inclui casos de prova: homônimos, cliente e produto inativos, pedidos em 2025 e 2026. | — | RNF-04 | L028, L063 |
| **RF-09** *(derivado)* | Operador / frontend | Consultar um pedido com seus itens (`GET /pedidos/{id}`), alvo do `Location`. | RN-09 | CA-11; **CA-34 proposto** (PROP-02) | — |
| **RF-10** *(derivado)* | Frontend | Listar clientes e produtos ativos para os seletores (`GET /clientes`, `GET /produtos`). | RN-06, RN-07 | **CA-35 proposto** (PROP-02) | habilita L037 |
| **RF-11** *(derivado)* | Cliente da API | Idempotência opcional do `POST /pedidos` por `Idempotency-Key`. | RN-14 | CA-13; **CA-37 proposto** (PROP-05) | — |

---

## 3. Requisitos não funcionais

Apresentados na ordem pedida pela esteira. IDs alinhados à spec §7.

| ID | Tema | Requisito | Fonte | Como se verifica |
|---|---|---|---|---|
| **RNF-01** | Integridade sob concorrência | Nenhuma concorrência gera estoque negativo ou venda acima do saldo. Baixa por instrução atômica, transação única, ordem de lock por `ProdutoId`, `CHECK (Estoque >= 0)`. | Spec RNF-01, §4.3; ENU-L031 | Integração CA-10 (20 requisições paralelas) |
| **RNF-02** | Desempenho de consulta | Listagem e ranking com *index seek*, sem *key lookup*, sem *scan* de `Pedidos`, sem N+1. Filtro de período sargável. | Spec RNF-02, §6.3; ENU-L027, L040 | Plano de execução via MCP (evidência na Parte 4) |
| **RNF-06** | Segurança | (a) Validação de entrada: FluentValidation, limites RN-01/02, `pageSize ≤ 100`, tipos estritos. (b) Menor privilégio: API usa `joya_app` sem `DELETE`/DDL; MCP usa `claude_mcp` só leitura; contêineres sem root. (c) Segredos: nenhum segredo real no repositório; senha de dev só no compose local, documentada como tal; *secret scanning* ativo (HL-08). (d) Sem exceção ou stack trace exposto; só consultas parametrizadas. (e) **Limites contra abuso**: tamanho máximo do corpo e limite de taxa no `POST` — **não estão na spec** (PROP-03). | Spec RNF-06, §6.5; HL-03, HL-08 | Testes de integração de permissão e de 500; revisão `ciberseguranca` |
| **RNF-05** | Observabilidade | Log estruturado no console; todo erro em Problem Details (RFC 9457) com `code` e `traceId`; `/health/live` e `/health/ready` (este verifica o SQL Server). | Spec RNF-05, §5.1, §5.5 | Integração; `depends_on: service_healthy` no compose |
| **RNF-04** | Portabilidade | `docker-compose.yml` na raiz; `docker compose up` sobe banco (schema + carga automáticos), API e frontend, sem passo manual nem `.env` obrigatório. Subir de novo não falha nem duplica a carga. | Spec RNF-04; ENU-L061–L068 | Verificação manual em clone limpo + job de CI |
| **RNF-03** | Manutenibilidade: build limpo | Zero warnings (`TreatWarningsAsErrors`), nullable ligado, analisadores ativos, `dotnet format` e lint do Angular limpos. | Spec RNF-03; `CLAUDE.md` | `dotnet build`, `dotnet format --verify-no-changes`, `npm run lint` |
| **RNF-07** | Manutenibilidade: testes | Testes unitários das regras e validações do pedido, sem I/O. Testes de integração dos endpoints contra **SQL Server real**, aplicando os mesmos scripts de `database/`. Relógio injetável. | Spec RNF-07; ENU-L054, L055 | `dotnet test` nos dois projetos |
| **RNF-08** *(novo)* | Acessibilidade e `pt-BR` | Moeda `R$ 1.234,56` e datas `dd/MM/yyyy` em `pt-BR`; campos com `label` associado; mensagens de erro ligadas ao campo; navegação por teclado nos formulários; contraste legível. | `CLAUDE.md` (Frontend); **não está na spec** (PROP-04) | Testes de componente + revisão manual |
| **RNF-09** | Stack exigida | SQL Server, .NET 8 ou superior, Angular com TypeScript. | ENU-L025, L029, L034; cabeçalho da spec | Build dos projetos |

**Restrições que passam para o arquiteto** (registro, sem decisão):

- Testes de integração precisam de SQL Server real (ENU-L055). A escolha entre Testcontainers e o banco do compose é do arquiteto (o `CLAUDE.md` já aponta Testcontainers + Respawn).
- Os scripts de `database/` são a única fonte do schema; o EF mapeia, não migra (spec §6).
- O MCP precisa de um login próprio e somente leitura (spec §6.5).

---

## 4. Requisitos de entrega

| ID | Requisito | ENU | Evidência que o avaliador procura |
|---|---|---|---|
| **ENT-01** | `docs/spec.md` na raiz, antes do código, com objetivo, regras, endpoints, modelo de dados e critérios de aceite | L011, L013 | Arquivo e ordem dos commits |
| **ENT-02** | `docs/plan.md` gerado em plan mode, com ajustes e discordâncias e o porquê | L011, L014 | Seção HL preenchida (`HL-01`…) |
| **ENT-03** | `CLAUDE.md` com arquitetura, nomenclatura e comandos de build e testes | L019 | Arquivo; testes de arquitetura (HL-08) que provam as regras |
| **ENT-04** | Pelo menos um hook útil | L020 | `.claude/hooks/post-edit.sh` e `settings.json` |
| **ENT-05** | Pelo menos uma skill customizada | L021 | `.claude/skills/new-endpoint/` |
| **ENT-06** | MCP conectado ao SQL Server e explicado no README | L022 | `.mcp.json`, login `claude_mcp`, seção do README com exemplos reais de uso |
| **ENT-07** | `.claude/` e `CLAUDE.md` versionados | L017 | `git ls-files` |
| **ENT-08** | Repositório no GitHub e link enviado por e-mail | L072 | Repositório público; e-mail (ato do desenvolvedor) |
| **ENT-09** | README na raiz | L073 | Arquivo `README.md` |
| **ENT-10** | README: como rodar com Docker | L074 | Seção com o comando único e as URLs |
| **ENT-11** | README: como rodar os testes unitários e de integração | L075 | Seção com comandos e pré-requisito (Docker para integração) |
| **ENT-12** | README: principais decisões técnicas | L076 | Seção que resume D-xx, ADRs e HL |
| **ENT-13** | README: como usou o Claude, o que funcionou e o que precisou corrigir | L077 | Seção baseada nos `HL`/`CG` e no diário |
| **ENT-14** | README: explicação da Parte 4 (o que estava errado e como investigou) | L040, L078 | Seção com defeitos, planos antes/depois e índices |
| **ENT-15** | Projeto rodando no Docker conforme a Parte 6 | L079 | Mesmo que RNF-04 |
| **ENT-16** | Testes unitários e de integração conforme a Parte 5 | L080 | Mesmo que RNF-07; CI verde |
| **ENT-17** | Histórico de commits preservado, pequeno e claro, sem squash nem reescrita | L081 | 1 commit por tarefa, PR com merge commit (HL-06), `main` protegida |
| **ENT-18** | Vídeo de até 10 minutos mostrando o fluxo com o Claude Code e explicando o código | L082 | Link no README; roteiro em `docs/ia/` (HL-07) |
| **ENT-19** *(diferencial)* | Testes E2E com Playwright: criar pedido e vê-lo na listagem | L058, L085 | `e2e/` + job no CI (CA-30) |
| **ENT-20** *(diferencial)* | GitHub Actions: build e testes a cada push ou PR | L086 | `.github/workflows/ci.yml` verde nos PRs |
| **ENT-21** *(diferencial)* | Deploy online com link no README | L087 | URL HTTPS funcionando (VM + Caddy, HL-04) |
| **ENT-22** *(diferencial, HL-08)* | Diferenciais a mais: `main` protegida + CodeQL + Dependabot (feito); matriz de rastreabilidade validada no CI; testes de arquitetura com ArchUnitNET; relatório de custo medido no README | — | Configuração do repositório, `rastreabilidade.md`, testes, seção de custo |
| **ENT-23** | Uso estruturado e obrigatório do Claude Code | L007 | `.claude/`, esteira, diário `docs/ia/`, vídeo |

---

## 5. Fora do escopo e porquê

| Item | Por que fica fora | Risco da ausência | Como o README declara |
|---|---|---|---|
| Autenticação e autorização (HL-03) | O enunciado não pede | Qualquer pessoa com acesso à URL cria pedidos e lê dados. Grave no deploy público | Seção "Limitações": API aberta por decisão; em produção, gateway com OIDC. Registrada como risco aceito pelo `ciberseguranca` |
| CRUD de clientes e produtos | Não pedido; os dados vêm da carga | Sem como cadastrar pelo sistema | "Dados de exemplo vêm de `database/`" |
| Edição, cancelamento e exclusão de pedido | Não pedido; pedido é fato imutável (RN-12) | Erro de digitação não tem correção pela aplicação | "Pedido é imutável; cancelamento exigiria estorno de estoque" |
| Tela do relatório de top clientes | O enunciado pede só o endpoint (L033) | Avaliador pode esperar ver o ranking na tela | "Relatório disponível pela API e Swagger" |
| Paginação ou busca em `/clientes` e `/produtos` | A carga tem dezenas de linhas | Não escala para catálogo real | "Evolução: typeahead paginado" |
| Fuso de negócio (períodos em horário de Brasília) | Premissa P1: UTC | Pedido de 22h de 31/12 em Brasília entra em 01/01 | "Datas e períodos em UTC" |
| Reserva de estoque, pagamento, frete, impostos, descontos | Não pedido | — | Lista curta em "Fora do escopo" |
| Métricas e tracing distribuído | Não pedido; RNF-05 cobre o mínimo (logs, `traceId`, health) | Diagnóstico em produção mais limitado | "Observabilidade mínima" |
| Limite de taxa (se o desenvolvedor rejeitar a PROP-03) | Não pedido | Abuso do `POST` no deploy público | Junto com autenticação, como risco aceito |

---

## 6. Itens acrescentados além do enunciado

| Item | Justificativa | Custo de remover |
|---|---|---|
| `Clientes.Ativo` e RN-06 (cliente inativo) | Premissa P2; "cliente existe" é o pedido (L031), mas vender para cliente desativado é um erro comum | Baixo: tirar a coluna, a regra e o CA-06 |
| `Produtos.Ativo` e parte da RN-07 | Simetria com cliente; a baixa atômica já filtra `Ativo = 1` | Baixo: tirar o predicado da baixa e o produto inativo do CA-07 |
| Idempotência (RN-14, RF-11) | Protege contra duplo clique e *retry* que debitariam estoque duas vezes | Médio: coluna, índice filtrado, cabeçalhos e CA-13. Sem ela, um *retry* duplica pedidos |
| `GET /pedidos/{id}` (RF-09) | Alvo do `Location` do `201`; usado no CA-11 | Baixo, mas o `201` ficaria sem `Location` válido e o CA-11 perderia o meio de verificação |
| `GET /clientes` e `GET /produtos` (RF-10) | A tela de criação (L037) não funciona sem seletores | Alto: a tela teria de pedir IDs digitados, o que torna o frontend pouco funcional (peso 5%) |
| Health checks (RNF-05) | O compose precisa saber quando a API está pronta para subir o frontend sem falha (L066) | Médio: subida sem ordem confiável |
| Limites de 50 itens e 10 000 unidades (P5) | Validação de entrada e proteção contra abuso | Baixo: constantes |
| Produto repetido é erro (RN-03, P3) | Contrato explícito; PK composta traduz a regra | Baixo: consolidar antes da baixa |
| `ValorTotal` desnormalizado | Listagem e ranking leem só `Pedidos`, com índice coberto | Médio: consultas passam a agregar itens; muda o plano da Parte 4 |
| Logins de menor privilégio (`joya_app`, `claude_mcp`) | Segurança; o MCP não pode escrever | Baixo, mas o MCP com `sa` seria um achado grave de segurança |
| Problem Details com `code` e `traceId` | Contrato de erro legível por máquina; o frontend usa o `code` | Médio: o frontend teria de interpretar textos |
| Swagger em `/swagger` | Facilita a avaliação da API | Baixo |
| Diferenciais do HL-08 (ENT-22) | Decisão humana: evidência verificável de qualidade | Baixo por item; cada um é independente |

---

## 7. Lacunas e propostas de alteração da spec

### Desvios em relação à spec

| PROP | Texto atual da spec | Proposta | Motivo |
|---|---|---|---|
| **PROP-01** | §9: "Parte 4 — debug da query · RN-15, RN-17 · CA-22, CA-23 · Script comparativo em `database/` + README". Não há regra nem critério para a query corrigida. | Acrescentar em §8.3 o **CA-33**: "Dado a carga de exemplo, quando executo a query corrigida da Parte 4 (arquivo em `database/`), então ela retorna os clientes com compra em 2026 em ordem decrescente de total, uma linha por `ClienteId` (homônimos separados), sem clientes sem compra no período, com desempate por `Id`; os 5 primeiros coincidem com `GET /relatorios/top-clientes` de 2026; e o plano de execução não tem *scan* de `Pedidos` nem função sobre `DataPedido`." Registrar a premissa **P7**: a query corrigida devolve o **ranking completo** (o original não tem `TOP`), e não só 5. | ENU-L040 pede "o ranking dos clientes que mais compraram em 2026". Sem critério, o QA não tem como aceitar a Parte 4 (peso 10%). A quantidade de linhas é ambígua no enunciado. |
| **PROP-02** | §5.5 lista `GET /pedidos/{id}`, `/clientes`, `/produtos`, `/health/*`; §5.1 cita `415` e `500`. Nenhum tem `CA`. | Acrescentar em §8: **CA-34** `GET /pedidos/{id}` devolve `200` com itens e `404 PEDIDO_NAO_ENCONTRADO` para id inexistente. **CA-35** `GET /clientes?ativo=true` e `GET /produtos?ativo=true` devolvem só ativos, ordenados por nome. **CA-36** `/health/live` devolve `200`; `/health/ready` devolve `503` sem banco e `200` com banco. **CA-38** `POST` com `Content-Type` diferente de JSON devolve `415`; falha inesperada devolve `500 ERRO_INTERNO` em Problem Details, sem stack trace. | Todo comportamento com contrato precisa de critério verificável. Sem isso, os testes dessas rotas não têm nome rastreável. |
| **PROP-03** | RNF-06: "Somente consultas parametrizadas; nenhuma mensagem de exceção…; contêineres … sem root; a aplicação não usa `sa`." Não fala de limites contra abuso. | Acrescentar à RNF-06: "Tamanho máximo do corpo da requisição (valor definido pelo arquiteto) → `413`. Limite de taxa por IP no `POST /pedidos` com o *rate limiter* nativo do ASP.NET Core, sem dependência nova → `429` em Problem Details." Incluir `413` e `429` no catálogo de erros. | A API ficará pública no deploy (ENU-L087) e sem autenticação (HL-03). Os limites de itens não impedem corpos enormes nem rajadas. |
| **PROP-04** | A spec não tem requisito de acessibilidade nem de formatação `pt-BR` (só o `CLAUDE.md` cita `pt-BR`). | Acrescentar **RNF-08** (texto da seção 3 deste relatório) e citá-lo nos CA-28 a CA-32. | Formatação errada de moeda e data é visível na primeira tela. Requisito de UI deve estar na fonte de verdade técnica, e não só nas convenções. |
| **PROP-05** | RN-14 não define o que acontece quando duas requisições com a **mesma** `Idempotency-Key` chegam ao mesmo tempo. CA-13 usa a chave `"K"`, mas §5.1 exige UUID. Não há CA para chave inválida. | Acrescentar à RN-14: "Se a gravação violar `UX_Pedidos_ChaveIdempotencia`, a transação é desfeita (o estoque volta) e a API devolve `200` com o pedido original e `Idempotent-Replayed: true`." Trocar `"K"` por um UUID no CA-13. Novo **CA-37**: chave que não é UUID → `400 VALIDACAO`; duas requisições simultâneas com a mesma chave → um `201`, um `200` e uma única baixa. | Sem isso, a corrida devolve `500` ou debita estoque duas vezes, o que é exatamente o que a regra quer evitar. |
| **PROP-06** | RNF-04: "`docker compose up` sobe…". O `CLAUDE.md` documenta `docker compose up --build`. | Acrescentar à RNF-04: "Num clone limpo, `docker compose up` (sem flags) constrói as imagens e sobe tudo. `--build` só é necessário para reconstruir depois de mudar o código." O README usa o mesmo texto. | ENU-L066 pede um único comando e o avaliador vai digitar o do exemplo (L068). Dois comandos diferentes nos documentos parecem passo manual. |
| **PROP-07** | §9 "Rastreabilidade" mantém uma tabela própria, mais grossa, do enunciado até o teste. | Manter a §9 como resumo e acrescentar: `A matriz oficial, validada no CI, é docs/esteira/rastreabilidade.md (HL-08).` | Evita duas tabelas divergentes; a do CI é a que falha o build. |

### Bloqueios e perguntas

- **P-A (PROP-01):** a query da Parte 4 deve devolver o ranking completo ou só os 5 primeiros? Recomendação: completo, porque o enunciado não limita e a correção não deve mudar a intenção da query.
- **P-B (PROP-01):** clientes sem compra em 2026 devem aparecer com total 0? O `LEFT JOIN` do original sugere que sim, mas "os que mais compraram" sugere que não. Recomendação: não aparecem (igual à RN-17), e o README explica que o `WHERE` sobre a tabela da direita já anulava o `LEFT JOIN`.
- **P-C (PROP-03):** aceitar o limite de taxa ou declará-lo como risco aceito junto com a autenticação?
- **P-D:** o link do repositório será enviado por e-mail pelo próprio desenvolvedor (ENT-08). A tarefa do backlog cobre só o README.

---

## 8. Pesos da avaliação → prioridade

| Critério (ENU) | Peso | Requisitos | Evidência que o avaliador vai procurar |
|---|---|---|---|
| Qualidade da spec e do plano (L091) | 15% | ENT-01, ENT-02, ENT-22 (matriz) | Spec com RN/CA numerados; plano com HL reais; matriz validada no CI; PROPs aplicadas |
| Uso estruturado do Claude (L092) | 20% | ENT-03…ENT-07, ENT-13, ENT-23, ENT-22 (arquitetura, custo) | `CLAUDE.md`, hooks que funcionam, skills usadas de fato, MCP com login só leitura e exemplos no README, esteira e diário |
| Backend .NET e modelagem SQL (L093) | 15% | RF-01, RF-02, RF-03, RF-07, RF-08, RNF-01, RNF-02, RNF-06, RNF-09 | Clean Architecture, baixa atômica, índices justificados, Problem Details, zero warnings |
| Debug e otimização da query (L094) | 10% | RF-04, RNF-02, ENT-14 | Script corrigido, lista dos 4 defeitos, plano antes/depois, uso do MCP |
| Testes unitários e de integração (L095) | 15% | RNF-07, RNF-01 (CA-10), ENT-16 | Testes nomeados por CA; SQL Server real; teste de concorrência |
| Docker (L096) | 5% | RNF-04, ENT-15 | `docker compose up` num clone limpo, sem `.env` e sem passo manual |
| Histórico de commits (L097) | 10% | ENT-17 | Commits pequenos por tarefa, mensagens claras, PRs com merge commit, sem squash |
| Frontend funcional (L098) | 5% | RF-05, RF-06, RF-10, RNF-08 | Filtros que funcionam, criação com tratamento de `409`, `pt-BR` |
| README e senso crítico (L099) | 5% | ENT-09…ENT-14 | Seções exigidas, correções da IA (`CG`), limitações declaradas |
| Pontos extras (L100) | — | ENT-19, ENT-20, ENT-21, ENT-22 | Playwright no CI, Actions verde, URL online |

**Consequência para a prioridade:** P1 cobre tudo que tem peso. Os diferenciais são P3, mas o CI vem cedo na ordem de execução (HL-04), porque também gera evidência para o histórico de commits (10%).

### Tarefas

| Tarefa | Status | Mensagem de commit prevista | Arquivos principais |
|---|---|---|---|
| T-01.1 | Pronta (aguarda gate 1) | `docs(esteira): registra requisitos, backlog e matriz de rastreabilidade (etapa 1)` | `docs/esteira/01-requisitos.md`, `docs/esteira/backlog.md`, `docs/esteira/rastreabilidade.md` |

### Testes criados ou alterados

| Teste | Tipo | Cobre |
|---|---|---|
| — (etapa de requisitos; os nomes previstos estão no backlog e na matriz) | — | — |

### Verificações executadas

| Comando | Resultado | Resumo da saída |
|---|---|---|
| `python3 .github/scripts/validar_rastreabilidade.py` | Não executado (o PO não roda comandos; o orquestrador roda no gate) | Regras conferidas uma a uma na mão |

## Resposta a achados

| Achado | Rodada | O que foi feito | Onde (arquivo:linha) | Teste que prova | Status declarado |
|---|---|---|---|---|---|
| — | — | — | — | — | — |
