# Status da esteira

> Mantido **somente pelo orquestrador** (skill `/esteira`). Cada linha de gate traz a saída resumida dos comandos que o orquestrador rodou. Um relatório de agente não vale como evidência.

## Próximo passo

**Commits e PR da US-02** (`us-02-esqueleto` → `main`), depois de autorizados. Em seguida, **etapa 3, onda 1**: US-03 (CI e testes de arquitetura) e T-04.1, pela ordem do [backlog](backlog.md), com uma delegação por história (`HL-24`). A US-04 em diante exige Docker.

## Etapas

| # | Etapa | Agente | Status | Rodada | Gate | Data |
|---|---|---|---|---|---|---|
| 0 | Configuração AI-first (spec, plano, CLAUDE.md, hooks, skills, MCP, agentes) | orquestrador | Concluída | 2 | Configuração validada no CI (`config`). Build e testes .NET ainda sem código; Docker ausente na máquina (ver diário E04) | 2026-11-13 |
| 1 | Requisitos e backlog | product-owner | Concluída | 1 (tentativa 2) | Aprovado: matriz com 0 erro, configuração válida, 7 PROPs decididas (`HL-10` a `HL-16`) | 2026-11-13 |
| 2 | Arquitetura | arquiteto | Concluída | 1 | Aprovado: checklist com 55 itens verificáveis, 4 ADRs (0002 a 0005), matriz com 0 erro, configuração válida, 6 PROPs e o ADR-0004 decididos (`HL-17` a `HL-23`) | 2026-11-13 |
| 3 | Backend, por história | backend | Em andamento (US-02 pronta) | 2 (US-02) | US-02 aprovada na rodada 2: build com 0 aviso, 10/10 testes unitários, `dotnet format` sem mudanças, matriz com 0 erro, configuração válida. Rodada 1 reprovada por `default!` (BE-07, `CG-03`). Integração: não há testes ainda | 2026-11-13 |
| 4 | Frontend, por história | frontend | Pendente | — | — | — |
| 4b | Revisão de arquitetura | arquiteto | Pendente | — | — | — |
| 5 | Segurança | ciberseguranca | Pendente | — | — | — |
| 6 | QA final | qa | Pendente | — | — | — |

## Histórias

| US | Dono | Status | Tarefas prontas / total | Commits |
|---|---|---|---|---|
| US-01 Fundação SDD e rastreabilidade | orquestrador | Concluída (PR #2) | 3 / 3 | 501ae41, 125952a, abf8b91 |
| US-02 Esqueleto da solução .NET | backend | Pronta, PR aberto | 2 / 2 | 088ea5b, 3d2ebe9 |
| US-03 Qualidade automatizada: CI e testes de arquitetura | backend | Pendente | 0 / 5 | — |
| US-04 Banco de dados com schema, índices, logins e carga | backend | Pendente | 0 / 5 | — |
| US-05 Base HTTP da API | backend | Pendente | 0 / 3 | — |
| US-06 Criar pedido com baixa atômica de estoque | backend | Pendente | 0 / 7 | — |
| US-07 Catálogo de clientes e produtos para as telas | backend | Pendente | 0 / 1 | — |
| US-08 Listar pedidos com filtros e paginação | backend | Pendente | 0 / 2 | — |
| US-09 Relatório de top 5 clientes | backend | Pendente | 0 / 2 | — |
| US-10 Parte 4: debug e otimização da query | backend | Pendente | 0 / 3 | — |
| US-11 Consultar pedido por id | backend | Pendente | 0 / 2 | — |
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
| PROP-08 a PROP-13 e ADR-0004 | `arquiteto`, etapa 2 | Ver [02-arquitetura.md](02-arquitetura.md), Desvios em relação à spec | Decididas em 2026-11-13: todas aceitas (`HL-17` a `HL-23`), aplicadas à spec e ao plano; ajustes do backlog delegados ao `product-owner` |

## Execuções de agentes

> Uma linha por delegação. Regra de modelo e escalonamento em [ADR-0001](../adr/0001-roteamento-de-modelos.md). Esta tabela alimenta a seção de custo do README.

| Data | Etapa / tarefa | Complexidade | Agente | Modelo | Esforço | Tentativa | Gate | Escalou? | Tokens |
|---|---|---|---|---|---|---|---|---|---|
| 2026-11-13 21:01 | Etapa 1, tentativa 1 | — | product-owner | opus | high | 1 | Não chegou ao gate: parado pelo desenvolvedor após 13 min sem gravar nada | Sim: esforço `high` → `medium` (`HL-09`) | 28.515 de saída (1.256.239 com cache), lidos da transcrição: o resultado veio sem uso |
| 2026-11-13 21:17 | Etapa 1, tentativa 2 (+2 correções do link) | — | product-owner | opus | medium | 2 | Rodada 1 reprovada (link quebrado), rodada 3 aprovada | Não | 127.494 |
| 2026-11-13 21:39 | Etapa 2, DESENHO | — | arquiteto | opus | high | 1 | Aprovado na rodada 1 | Não | 138.562 |
| 2026-11-13 22:06 | Etapa 1, CORRECAO rodada 2 (ajustes do backlog pedidos pela etapa 2, `HL-23`) | — | product-owner | opus | medium | 1 | Aprovado | Não | 94.284, somando a limpeza das marcas de PROP feita pelo mesmo agente |
| 2026-11-13 | Etapa 3, US-02, T-02.1, tentativas 1 e 2 | C2 | backend | sonnet | medium | 1–2 | Não chegou ao gate: `model_not_found` (aliases do `settings.json` global apontavam para um *gateway* local fora do ar). Corrigido no ambiente (`HL-24`), sem relação com a tarefa | Não | 0 (falha antes da primeira resposta) |
| 2026-11-13 22:27 | Etapa 3, US-02, T-02.1 (tentativa 3) e T-02.2 (tentativa 1), uma delegação (`HL-24`) | C2 | backend | sonnet | medium | 3 / 1 | Rodada 1 reprovada pelo orquestrador (`default!`, BE-07) | Não | 52.990 |
| 2026-11-13 22:33 | Etapa 3, US-02, CORRECAO de BE-07 (T-02.2, tentativa 2) | C2 | backend | sonnet | medium | 2 | Aprovado | Não | 32.759 |

## Log de gates

| Data | Etapa | Comandos rodados pelo orquestrador | Resultado |
|---|---|---|---|
| 2026-11-13 | 0 | Validação de YAML e JSON, `bash -n` nos hooks, testes manuais de `guard-paths.sh` e `guard-bash.sh` | Configuração válida; build e testes indisponíveis nesta máquina |
| 2026-11-13 | 0 (rodada 2) | `python .github/scripts/validar_config.py`, `bash .github/scripts/testar_hooks.sh` (local e no CI, run `43525593188`); `dotnet --list-sdks` → 8.0.428 | Aprovado: CI verde em 18 s. Docker ausente: bloqueia a etapa 3, mas não as etapas 1 e 2 |
| 2026-11-13 | 3, US-02 (rodada 1) | `dotnet build`, `dotnet test` (unit), `dotnet format --verify-no-changes`, os 2 validadores, busca por `!`, `#pragma` e `NoWarn` | Reprovado: tudo verde, mas `default!` em `ResultTests.cs:13` (BE-07) |
| 2026-11-13 | 3, US-02 (rodada 2) | Os mesmos | Aprovado: 0 aviso, 0 erro; 10/10 testes; formato sem mudanças; matriz com 0 erro; configuração válida; nenhum silenciamento. Testes de integração: o projeto compila, mas ainda não tem testes |
| 2026-11-13 | 0 (rodada 3, `HL-08`) | `bash .github/scripts/testar_rastreabilidade.sh` (9/9), `python3 .github/scripts/validar_rastreabilidade.py` (0 erros, matriz vazia), `gh api …/branches/main/protection` | Proteção da `main`, secret scanning e Dependabot ativos. O gate é o CI e o CodeQL no primeiro PR |
| 2026-11-13 | 1 (pré-condição) | `docker info` (Server Version 29.9.3, Docker Desktop); tipos de agente do projeto disponíveis; skill `/esteira` carregada | Ambiente pronto. O daemon do Docker agora responde |
| 2026-11-13 | 1 (rodada 1) | `python3 .github/scripts/validar_rastreabilidade.py` → 0 erro; `python .github/scripts/validar_config.py` → `link quebrado: esteira/rastreabilidade.md` em `01-requisitos.md` | Reprovado; devolvido ao `product-owner` |
| 2026-11-13 | 1 (rodada 2) | `validar_config.py` → mesmo erro: a regex de links não ignora crases | Reprovado; devolvido ao `product-owner` |
| 2026-11-13 | 1 (rodada 3) | `validar_rastreabilidade.py` → 0 erro; `validar_config.py` → válida. Leitura de `01-requisitos.md`, `backlog.md` (18 US, 56 tarefas: 7 C1, 34 C2, 15 C3) e `rastreabilidade.md` (75 linhas: 46 obrigatórias, 4 diferenciais, 25 de contexto) | Aprovado. Depois da spec atualizada com as PROPs, os dois validadores passaram de novo |
| 2026-11-13 | 2 (rodada 1) | Leitura de `02-arquitetura.md` (8 seções; checklist com 27 itens de backend, 8 de banco, 14 de frontend e 6 de Docker, cada um com comando) e dos ADRs 0002 a 0005; `validar_rastreabilidade.py` → 0 erro; `validar_config.py` → válida; `guard-paths.sh` do `backend` já cobre `docs/contratos/` | Aprovado. Spec e plano atualizados com `HL-17` a `HL-23`; validadores verdes de novo |
| 2026-11-13 | 1 (rodada 2, ajuste do backlog) | `validar_rastreabilidade.py` → 0 erro; `validar_config.py` → válida; `git diff --stat` só em `backlog.md` e `rastreabilidade.md` (sem mexer em Commits). 57 tarefas: 7 C1, 35 C2, 15 C3 | Aprovado |
