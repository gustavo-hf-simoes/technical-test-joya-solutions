# Status da esteira

> Mantido **somente pelo orquestrador** (skill `/esteira`). Cada linha de gate traz a saída resumida dos comandos que o orquestrador rodou. Um relatório de agente não vale como evidência.

## Próximo passo

**Etapa 1 — Requisitos e backlog** (`product-owner`). Aguarda autorização do desenvolvedor.

## Etapas

| # | Etapa | Agente | Status | Rodada | Gate | Data |
|---|---|---|---|---|---|---|
| 0 | Configuração AI-first (spec, plano, CLAUDE.md, hooks, skills, MCP, agentes) | orquestrador | Concluída | 2 | Configuração validada no CI (`config`). Build e testes .NET ainda sem código; Docker ausente na máquina (ver diário E04) | 2026-11-13 |
| 1 | Requisitos e backlog | product-owner | Pendente | — | — | — |
| 2 | Arquitetura | arquiteto | Pendente | — | — | — |
| 3 | Backend, por história | backend | Pendente | — | — | — |
| 4 | Frontend, por história | frontend | Pendente | — | — | — |
| 4b | Revisão de arquitetura | arquiteto | Pendente | — | — | — |
| 5 | Segurança | ciberseguranca | Pendente | — | — | — |
| 6 | QA final | qa | Pendente | — | — | — |

## Histórias

| US | Dono | Status | Tarefas prontas / total | Commits |
|---|---|---|---|---|
| — | — | Backlog ainda não gerado | — | — |

## Achados abertos

| ID | Severidade | Dono | Rodada | Status | Resumo |
|---|---|---|---|---|---|
| — | — | — | — | — | — |

## Decisões humanas pendentes

| PROP / pergunta | Origem | Pergunta | Status |
|---|---|---|---|
| — | — | — | — |

## Execuções de agentes

> Uma linha por delegação. Regra de modelo e escalonamento em [ADR-0001](../adr/0001-roteamento-de-modelos.md). Esta tabela alimenta a seção de custo do README.

| Data | Etapa / tarefa | Complexidade | Agente | Modelo | Esforço | Tentativa | Gate | Escalou? | Tokens |
|---|---|---|---|---|---|---|---|---|---|
| — | — | — | — | — | — | — | — | — | — |

## Log de gates

| Data | Etapa | Comandos rodados pelo orquestrador | Resultado |
|---|---|---|---|
| 2026-11-13 | 0 | Validação de YAML e JSON, `bash -n` nos hooks, testes manuais de `guard-paths.sh` e `guard-bash.sh` | Configuração válida; build e testes indisponíveis nesta máquina |
| 2026-11-13 | 0 (rodada 2) | `python .github/scripts/validar_config.py`, `bash .github/scripts/testar_hooks.sh` (local e no CI, run `43525593188`); `dotnet --list-sdks` → 8.0.428 | Aprovado: CI verde em 18 s. Docker ausente: bloqueia a etapa 3, mas não as etapas 1 e 2 |
