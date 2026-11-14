# Status da esteira

> Mantido **somente pelo orquestrador** (skill `/esteira`). Cada linha de gate traz a saída resumida dos comandos que o orquestrador rodou. Um relatório de agente não vale como evidência.

## Próximo passo

**PR da rodada** (`us-07-catalogo` → `main`: US-07, US-08, US-09, US-14 e US-15), só com autorização. Depois, pela `HL-26`: US-10 (Parte 4, Opus) e US-13 (compose) em paralelo, US-18 (README); em seguida US-16, US-11, US-17 e US-12. A ENU-L036 fica sem commit até o teste de contrato contra o `openapi.json` (T-11.2) e a ENU-L065 até a T-13.2. Pendências: o comando do item BE-23 do checklist precisa aceitar `LimitesHttp.PoliticaCriarPedido` e o do BE-15 tem falsos positivos (dono: `arquiteto`); o deploy (T-17.x) precisa de `Proxy__ForwardedLimit=2`; o `Location` do 201 passa a `CreatedAtAction` na T-08.2; para o MCP `mssql`, reinicie o Claude Code com o banco de pé (`CG-05`).

## Etapas

| # | Etapa | Agente | Status | Rodada | Gate | Data |
|---|---|---|---|---|---|---|
| 0 | Configuração AI-first (spec, plano, CLAUDE.md, hooks, skills, MCP, agentes) | orquestrador | Concluída | 2 | Configuração validada no CI (`config`). Build e testes .NET ainda sem código; Docker ausente na máquina (ver diário E04) | 2026-11-13 |
| 1 | Requisitos e backlog | product-owner | Concluída | 1 (tentativa 2) | Aprovado: matriz com 0 erro, configuração válida, 7 PROPs decididas (`HL-10` a `HL-16`) | 2026-11-13 |
| 2 | Arquitetura | arquiteto | Concluída | 1 | Aprovado: checklist com 55 itens verificáveis, 4 ADRs (0002 a 0005), matriz com 0 erro, configuração válida, 6 PROPs e o ADR-0004 decididos (`HL-17` a `HL-23`) | 2026-11-13 |
| 3 | Backend, por história | backend | Em andamento (US-02, US-03 parte 1, US-04 e US-05 integradas; US-06 pronta) | 1 (US-06) | US-06 aprovada: 0 aviso, 64/64 unitários, 77/77 de integração (2 execuções), com 1 correção do orquestrador na T-06.6 (`CG-06`). US-05 aprovada na rodada 1: 0 aviso, 19/19 unitários, 35/35 de integração. US-04 aprovada na rodada 2: 0 aviso, 19/19 unitários, 21/21 de integração contra SQL Server 2022 real, compose com `db` saudável e `db-init` com saída 0. Rodada 1 reprovada por collation (`CG-04`). US-02 aprovada na rodada 2: build com 0 aviso, 10/10 testes unitários, `dotnet format` sem mudanças, matriz com 0 erro, configuração válida. Rodada 1 reprovada por `default!` (BE-07, `CG-03`). Integração: não há testes ainda | 2026-11-13 |
| 4 | Frontend, por história | frontend | Pendente | — | — | — |
| 4b | Revisão de arquitetura | arquiteto | Pendente | — | — | — |
| 5 | Segurança | ciberseguranca | Pendente | — | — | — |
| 6 | QA final | qa | Pendente | — | — | — |

## Histórias

| US | Dono | Status | Tarefas prontas / total | Commits |
|---|---|---|---|---|
| US-01 Fundação SDD e rastreabilidade | orquestrador | Concluída (PR #2) | 3 / 3 | 501ae41, 125952a, abf8b91 |
| US-02 Esqueleto da solução .NET | backend | Concluída (PR #4) | 2 / 2 | 088ea5b, 3d2ebe9 |
| US-03 Qualidade automatizada: CI e testes de arquitetura | backend | Em andamento: T-03.1 e T-03.2 integradas (PR #5); T-03.3 a T-03.5 dependem das US-04, US-14 e US-16 | 2 / 5 | 8b0384e, 86f7b47 |
| US-04 Banco de dados com schema, índices, logins e carga | backend | Concluída (PR #6) | 5 / 5 | 027a0ff, 75b834d, 97a88c1, d2c6c85, 994e5c0 |
| US-05 Base HTTP da API | backend | Concluída (PR #7) | 3 / 3 | 17b4a14, b786353, 09f0eb6 |
| US-06 Criar pedido com baixa atômica de estoque | backend | Concluída (PR #8) | 7 / 7 | 10d064c, 4f33672, 1b6564d, dac6332, 0506fe7, 22132a4, f969a53 |
| US-07 Catálogo de clientes e produtos para as telas | backend | Commitada, PR pendente | 1 / 1 | 61bb1c2 |
| US-08 Listar pedidos com filtros e paginação | backend | Commitada, PR pendente | 2 / 2 | 8b98d09, 59eae2d |
| US-09 Relatório de top 5 clientes | backend | Commitada, PR pendente | 2 / 2 | 15ae8c8, 9caca95 |
| US-10 Parte 4: debug e otimização da query | backend | Pendente | 0 / 3 | — |
| US-11 Consultar pedido por id | backend | Pendente | 0 / 2 | — |
| US-12 Idempotência na criação | backend | Pendente | 0 / 2 | — |
| US-13 Aplicação completa com um comando | backend | Pendente | 0 / 2 | — |
| US-14 Tela de listagem de pedidos | frontend | Commitada, PR pendente (contrato da T-14.2 a conferir na T-11.2) | 5 / 5 | 1779aa4, 9c9d50c, d1318ef, fe9cb83 |
| US-15 Tela de criação de pedido | frontend | Commitada, PR pendente | 3 / 3 | c757543, 32bc744 |
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
| 2026-11-13 22:45 | Etapa 3, US-03, T-03.1 e T-03.2 (uma delegação, `HL-24`) | C2 | backend | sonnet | medium | 1 | Aprovado na rodada 1 | Não | 76.027 |
| 2026-11-13 | Etapa 3, US-04, T-04.1 a T-04.3 (delegação 1 de 2, por nível) | C3 | backend | opus | high | 1 | Reprovado: 2 de 17 testes de integração com erro de collation (`CG-04`). O Docker caiu antes por disco C: cheio; o gate só rodou depois de liberado | Não | 149.220 |
| 2026-11-13 | Etapa 3, US-04, CORRECAO de `CG-04` (T-04.1 e T-04.2) | C3 | backend | opus | high | 2 | Aprovado: 17/17 | Não | 49.914 |
| 2026-11-13 | Etapa 3, US-04, T-04.4 (C1) e T-04.5 (C2) (delegação 2 de 2) | C2 | backend | sonnet | medium | 1 | Aprovado na rodada 1; PROP-04-01 era configuração, resolvida pelo orquestrador (`CG-05`) | Não | 91.113 |
| 2026-11-13 | Etapa 3, US-05, T-05.1 e T-05.2 (delegação 1 de 2) | C2 | backend | sonnet | medium | 1 | Aprovado na rodada 1 | Não | 84.048 |
| 2026-11-13 | Etapa 3, US-05, T-05.3 (delegação 2 de 2) | C3 | backend | opus | high | 1 | Aprovado na rodada 1 | Não | 113.288 |
| 2026-11-14 | Etapa 3, US-06, T-06.1 a T-06.3 (delegação 1 de 4) | C2 | backend | sonnet | medium | 1 | Aprovado na rodada 1 | Não | 125.657 |
| 2026-11-14 | Etapa 3, US-06, T-06.4 e T-06.5 (delegação 2 de 4) | C3 | backend | opus | high | 1 | Aprovado na rodada 1 | Não | 123.092 |
| 2026-11-14 | Etapa 3, US-06, T-06.6 (delegação 3 de 4, com 1 rodada de CORRECAO, `CG-06`) | C2 | backend | sonnet | medium | 1 | Aprovado na rodada 2 | Não | 113.804 |
| 2026-11-14 | Etapa 3, US-06, T-06.7 (delegação 4 de 4) | C3 | backend | opus | high | 1 | Aprovado na rodada 1 | Não | 99.429 |
| 2026-11-14 | Etapa 3, US-07, T-07.1 (delegação única, `HL-25`) | C2 | backend | sonnet | medium | 1 | Aprovado na rodada 1 | Não | 49.610 |
| 2026-11-14 | Etapa 4, US-14, T-14.1 a T-14.5 (delegação única, `HL-25`) | C2 | frontend | sonnet | medium | 1 | Aprovado na rodada 1 | Não | 91.895 |
| 2026-11-14 | Etapa 3, US-08 e T-09.1 (delegação única, com 1 rodada de CORRECAO: `default!` em teste) | C2 | backend | sonnet | medium | 1 | Aprovado na rodada 2 | Não | 117.647 |
| 2026-11-14 | Etapa 4, US-15, T-15.1 a T-15.3 (delegação única, `HL-25`) | C2 | frontend | sonnet | medium | 1 | Aprovado na rodada 1 | Não | 83.772 |
| 2026-11-14 | Etapa 3, US-09, T-09.2 | C3 | backend | opus | high | 1 | Aprovado na rodada 1 | Não | 121.362 |

## Log de gates

| Data | Etapa | Comandos rodados pelo orquestrador | Resultado |
|---|---|---|---|
| 2026-11-13 | 0 | Validação de YAML e JSON, `bash -n` nos hooks, testes manuais de `guard-paths.sh` e `guard-bash.sh` | Configuração válida; build e testes indisponíveis nesta máquina |
| 2026-11-13 | 0 (rodada 2) | `python .github/scripts/validar_config.py`, `bash .github/scripts/testar_hooks.sh` (local e no CI, run `43525593188`); `dotnet --list-sdks` → 8.0.428 | Aprovado: CI verde em 18 s. Docker ausente: bloqueia a etapa 3, mas não as etapas 1 e 2 |
| 2026-11-13 | 3, US-02 (rodada 1) | `dotnet build`, `dotnet test` (unit), `dotnet format --verify-no-changes`, os 2 validadores, busca por `!`, `#pragma` e `NoWarn` | Reprovado: tudo verde, mas `default!` em `ResultTests.cs:13` (BE-07) |
| 2026-11-13 | 3, US-02 (rodada 2) | Os mesmos | Aprovado: 0 aviso, 0 erro; 10/10 testes; formato sem mudanças; matriz com 0 erro; configuração válida; nenhum silenciamento. Testes de integração: o projeto compila, mas ainda não tem testes |
| 2026-11-13 | 3, US-03 (rodada 1) | `dotnet build`, `dotnet test` (unit), `dotnet format --verify-no-changes`, os 2 validadores, busca por silenciamento, SHA do `setup-dotnet` conferido contra a tag v6.1.0 (`gh api`), leitura dos 9 testes | Aprovado: 0 aviso; 19/19 testes; formato sem mudanças; matriz com 0 erro; configuração válida. O job `backend` do CI só fica provado no PR. A falha da regra do Core com EF foi provada pelo agente e revertida, sem reprodução pelo orquestrador |
| 2026-11-13 | 0 (rodada 3, `HL-08`) | `bash .github/scripts/testar_rastreabilidade.sh` (9/9), `python3 .github/scripts/validar_rastreabilidade.py` (0 erros, matriz vazia), `gh api …/branches/main/protection` | Proteção da `main`, secret scanning e Dependabot ativos. O gate é o CI e o CodeQL no primeiro PR |
| 2026-11-13 | 1 (pré-condição) | `docker info` (Server Version 29.9.3, Docker Desktop); tipos de agente do projeto disponíveis; skill `/esteira` carregada | Ambiente pronto. O daemon do Docker agora responde |
| 2026-11-13 | 1 (rodada 1) | `python3 .github/scripts/validar_rastreabilidade.py` → 0 erro; `python .github/scripts/validar_config.py` → `link quebrado: esteira/rastreabilidade.md` em `01-requisitos.md` | Reprovado; devolvido ao `product-owner` |
| 2026-11-13 | 1 (rodada 2) | `validar_config.py` → mesmo erro: a regex de links não ignora crases | Reprovado; devolvido ao `product-owner` |
| 2026-11-13 | 1 (rodada 3) | `validar_rastreabilidade.py` → 0 erro; `validar_config.py` → válida. Leitura de `01-requisitos.md`, `backlog.md` (18 US, 56 tarefas: 7 C1, 34 C2, 15 C3) e `rastreabilidade.md` (75 linhas: 46 obrigatórias, 4 diferenciais, 25 de contexto) | Aprovado. Depois da spec atualizada com as PROPs, os dois validadores passaram de novo |
| 2026-11-13 | 2 (rodada 1) | Leitura de `02-arquitetura.md` (8 seções; checklist com 27 itens de backend, 8 de banco, 14 de frontend e 6 de Docker, cada um com comando) e dos ADRs 0002 a 0005; `validar_rastreabilidade.py` → 0 erro; `validar_config.py` → válida; `guard-paths.sh` do `backend` já cobre `docs/contratos/` | Aprovado. Spec e plano atualizados com `HL-17` a `HL-23`; validadores verdes de novo |
| 2026-11-13 | 1 (rodada 2, ajuste do backlog) | `validar_rastreabilidade.py` → 0 erro; `validar_config.py` → válida; `git diff --stat` só em `backlog.md` e `rastreabilidade.md` (sem mexer em Commits). 57 tarefas: 7 C1, 35 C2, 15 C3 | Aprovado |
| 2026-11-13 | 3, US-04 (rodada 1) | `dotnet test` de integração (Testcontainers, `mssql/server:2022-latest`) | Reprovado: 15/17; erro 468 de collation nas consultas de catálogo de `SchemaTests` e `IndicesTests` (`CG-04`) |
| 2026-11-13 | 3, US-04 (rodada 2, T-04.1 a T-04.3) | `dotnet build`, `dotnet test` (unit e integração), `dotnet format --verify-no-changes`, os 2 validadores, busca por silenciamento | Aprovado: 0 aviso; 19/19 e 17/17; formato sem mudanças; matriz com 0 erro; configuração válida; nenhum silenciamento |
| 2026-11-13 | 3, US-04 (T-04.4 e T-04.5) | Os mesmos, mais `docker compose ps -a` e `validar_config.py` depois de ajustar o `.mcp.json` | Aprovado: 0 aviso; 19/19 e 21/21; `db` healthy, `db-init` com saída 0. Evidência no banco colhida pelo agente com `sqlcmd` (12 clientes, 15 produtos, 34 pedidos, 68 itens; `DELETE` negado aos dois logins); MCP não verificado nesta sessão (a senha nova só vale depois de reiniciar) |
| 2026-11-13 | 3, US-04 (PR #6) | `gh pr checks 6`: backend, CodeQL (actions, python) e configuração | Aprovado; integrado com merge commit `4162800` |
| 2026-11-13 | 3, US-05 (T-05.1 e T-05.2) | `dotnet build`, `dotnet test` (unit e integração), `dotnet format --verify-no-changes`, os 2 validadores, busca por silenciamento, pacotes contra a lista do BE-17 | Aprovado: 0 aviso; 19/19 e 28/28; pacotes novos (Swashbuckle, Mvc.Testing, EF SqlServer) todos na lista |
| 2026-11-13 | 3, US-05 (T-05.3) | Os mesmos | Aprovado: 0 aviso; 19/19 e 35/35; nenhum pacote novo. O 413 de corpo *chunked* contra o Kestrel real fica para o QA |
| 2026-11-13 | 3, US-05 (PR #7) | `gh pr checks 7` | Aprovado; integrado com merge commit `ecef84b` |
| 2026-11-14 | 3, US-06 (T-06.1 a T-06.3) | `dotnet build`, `dotnet test` unitários, `dotnet format --verify-no-changes`, busca por silenciamento, rastreabilidade | Aprovado: 0 aviso; 64/64 (inclui arquitetura) |
| 2026-11-14 | 3, US-06 (T-06.4 e T-06.5) | Os mesmos, mais integração | Aprovado: 0 aviso; 64/64 e 59/59. MCP não verificado (`AUTH_FAILED`, `CG-05`) |
| 2026-11-14 | 3, US-06 (T-06.6) | Os mesmos | Rodada 1: testes verdes (74/74), mas `title` fora da spec 5.2 (`CG-06`), devolvido. Rodada 2: aprovado, 0 aviso; 64/64 e 74/74 |
| 2026-11-14 | 3, US-06 (T-06.7) | Os mesmos, integração 2 vezes, e `validar_config.py` | Aprovado: 0 aviso; 64/64 e 77/77 nas 2 execuções |
| 2026-11-14 | 3, US-07 (T-07.1) | Build, unitários, integração, format, busca por silenciamento, rastreabilidade e `validar_config.py` | Aprovado: 0 aviso; 64/64 e 83/83. MCP não verificado (`CG-05`) |
| 2026-11-14 | 4, US-14 (T-14.1 a T-14.5) | lint, build e testes (ChromeHeadless do Chrome 157 já instalado, cache npm no D:) | Aprovado: lint limpo, build sem aviso, 12/12 |
| 2026-11-14 | 3, US-08 e T-09.1 | Os do backend | Rodada 1: 101/101 e 103/103, mas `default!` em `PedidoConsultaServiceTests.cs:53` (regra de ouro 1), devolvido. Rodada 2: aprovado, busca limpa, 0 aviso, 101/101 |
| 2026-11-14 | 4, US-15 (T-15.1 a T-15.3) | lint, build e testes | Aprovado: lint limpo, build sem aviso, 24/24 |
| 2026-11-14 | 3, US-09 (T-09.2) | Os do backend | Aprovado: 0 aviso, busca limpa; 109/109 e 118/118. MCP não verificado (`CG-05`); SQL gerado no relatório |
