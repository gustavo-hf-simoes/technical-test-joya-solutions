# Status da esteira

> Mantido **somente pelo orquestrador** (skill `/esteira`). Cada linha de gate traz a saída resumida dos comandos que o orquestrador rodou. Um relatório de agente não vale como evidência.

## Próximo passo

**Integrar o PR da etapa 1** (`chore/etapa-1-requisitos`) com merge commit, depois de autorizado. Em seguida, **etapa 2 — Arquitetura** (`arquiteto`, modo DESENHO), a partir da spec já com as `HL-10` a `HL-16`. O arquiteto confirma o limite de corpo da RNF-06 (`HL-12`) e o local dos testes de arquitetura (sugestão do PO: `UnitTests/Arquitetura/`).

## Etapas

| # | Etapa | Agente | Status | Rodada | Gate | Data |
|---|---|---|---|---|---|---|
| 0 | Configuração AI-first (spec, plano, CLAUDE.md, hooks, skills, MCP, agentes) | orquestrador | Concluída | 2 | Configuração validada no CI (`config`). Build e testes .NET ainda sem código; Docker ausente na máquina (ver diário E04) | 2026-11-13 |
| 1 | Requisitos e backlog | product-owner | Concluída | 1 (tentativa 2) | Aprovado: matriz com 0 erro, configuração válida, 7 PROPs decididas (`HL-10` a `HL-16`) | 2026-11-13 |
| 2 | Arquitetura | arquiteto | Pendente | — | — | — |
| 3 | Backend, por história | backend | Pendente | — | — | — |
| 4 | Frontend, por história | frontend | Pendente | — | — | — |
| 4b | Revisão de arquitetura | arquiteto | Pendente | — | — | — |
| 5 | Segurança | ciberseguranca | Pendente | — | — | — |
| 6 | QA final | qa | Pendente | — | — | — |

## Histórias

| US | Dono | Status | Tarefas prontas / total | Commits |
|---|---|---|---|---|
| US-01 Fundação SDD e rastreabilidade | orquestrador | Em PR (etapa 1) | 3 / 3, aguardando commit | — |
| US-02 Esqueleto da solução .NET | backend | Pendente | 0 / 2 | — |
| US-03 Qualidade automatizada: CI e testes de arquitetura | backend | Pendente | 0 / 5 | — |
| US-04 Banco de dados com schema, índices, logins e carga | backend | Pendente | 0 / 5 | — |
| US-05 Base HTTP da API | backend | Pendente | 0 / 3 | — |
| US-06 Criar pedido com baixa atômica de estoque | backend | Pendente | 0 / 7 | — |
| US-07 Catálogo de clientes e produtos para as telas | backend | Pendente | 0 / 1 | — |
| US-08 Listar pedidos com filtros e paginação | backend | Pendente | 0 / 2 | — |
| US-09 Relatório de top 5 clientes | backend | Pendente | 0 / 2 | — |
| US-10 Parte 4: debug e otimização da query | backend | Pendente | 0 / 3 | — |
| US-11 Consultar pedido por id | backend | Pendente | 0 / 1 | — |
| US-12 Idempotência na criação | backend | Pendente | 0 / 2 | — |
| US-13 Aplicação completa com um comando | backend | Pendente | 0 / 2 | — |
| US-14 Tela de listagem de pedidos | frontend | Pendente | 0 / 5 | — |
| US-15 Tela de criação de pedido | frontend | Pendente | 0 / 3 | — |
| US-16 Teste end to end com Playwright | frontend | Pendente | 0 / 1 | — |
| US-17 Deploy online | backend | Pendente | 0 / 2 | — |
| US-18 README e entrega | orquestrador | Pendente | 0 / 7 | — |

## Achados abertos

| ID | Severidade | Dono | Rodada | Status | Resumo |
|---|---|---|---|---|---|
| — | — | — | — | — | — |

## Decisões humanas pendentes

| PROP / pergunta | Origem | Pergunta | Status |
|---|---|---|---|
| PROP-01 a PROP-07 | `product-owner`, etapa 1 | Ver [01-requisitos.md](01-requisitos.md) §7 | Decididas em 2026-11-13: todas aceitas (`HL-10` a `HL-16`) e aplicadas à spec |

## Execuções de agentes

> Uma linha por delegação. Regra de modelo e escalonamento em [ADR-0001](../adr/0001-roteamento-de-modelos.md). Esta tabela alimenta a seção de custo do README.

| Data | Etapa / tarefa | Complexidade | Agente | Modelo | Esforço | Tentativa | Gate | Escalou? | Tokens |
|---|---|---|---|---|---|---|---|---|---|
| 2026-11-13 21:01 | Etapa 1, tentativa 1 | — | product-owner | opus | high | 1 | Não chegou ao gate: parado pelo desenvolvedor após 13 min sem gravar nada | Sim: esforço `high` → `medium` (`HL-09`) | 28.515 de saída (1.256.239 com cache), lidos da transcrição: o resultado veio sem uso |
| 2026-11-13 21:17 | Etapa 1, tentativa 2 (+2 correções do link) | — | product-owner | opus | medium | 2 | Rodada 1 reprovada (link quebrado), rodada 3 aprovada | Não | 127.494 |

## Log de gates

| Data | Etapa | Comandos rodados pelo orquestrador | Resultado |
|---|---|---|---|
| 2026-11-13 | 0 | Validação de YAML e JSON, `bash -n` nos hooks, testes manuais de `guard-paths.sh` e `guard-bash.sh` | Configuração válida; build e testes indisponíveis nesta máquina |
| 2026-11-13 | 0 (rodada 2) | `python .github/scripts/validar_config.py`, `bash .github/scripts/testar_hooks.sh` (local e no CI, run `43525593188`); `dotnet --list-sdks` → 8.0.428 | Aprovado: CI verde em 18 s. Docker ausente: bloqueia a etapa 3, mas não as etapas 1 e 2 |
| 2026-11-13 | 0 (rodada 3, `HL-08`) | `bash .github/scripts/testar_rastreabilidade.sh` (9/9), `python3 .github/scripts/validar_rastreabilidade.py` (0 erros, matriz vazia), `gh api …/branches/main/protection` | Proteção da `main`, secret scanning e Dependabot ativos. O gate é o CI e o CodeQL no primeiro PR |
| 2026-11-13 | 1 (pré-condição) | `docker info` (Server Version 29.9.3, Docker Desktop); tipos de agente do projeto disponíveis; skill `/esteira` carregada | Ambiente pronto. O daemon do Docker agora responde |
| 2026-11-13 | 1 (rodada 1) | `python3 .github/scripts/validar_rastreabilidade.py` → 0 erro; `python .github/scripts/validar_config.py` → `link quebrado: esteira/rastreabilidade.md` em `01-requisitos.md` | Reprovado; devolvido ao `product-owner` |
| 2026-11-13 | 1 (rodada 2) | `validar_config.py` → mesmo erro: a regex de links não ignora crases | Reprovado; devolvido ao `product-owner` |
| 2026-11-13 | 1 (rodada 3) | `validar_rastreabilidade.py` → 0 erro; `validar_config.py` → válida. Leitura de `01-requisitos.md`, `backlog.md` (18 US, 56 tarefas: 7 C1, 34 C2, 15 C3) e `rastreabilidade.md` (75 linhas: 46 obrigatórias, 4 diferenciais, 25 de contexto) | Aprovado. Depois da spec atualizada com as PROPs, os dois validadores passaram de novo |
