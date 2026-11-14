# Status da esteira

> Mantido **somente pelo orquestrador** (skill `/esteira`). Cada linha de gate traz a saída resumida dos comandos que o orquestrador rodou. Um relatório de agente não vale como evidência.

## Próximo passo

**Commits da correção dos achados** (branch `chore/achados-revisao-1`, sobre a `us-03-ci`), só com autorização: um por achado e um de docs. Antes, o **merge do PR #20** (CI de integração, frontend e E2E; os 8 checks verdes). Depois: QA final (etapa 6, agente `qa`), US-17 e US-12, e a T-11.2 (`HL-28`). Pendências: `Proxy__ForwardedLimit=2`, `ASPNETCORE_ENVIRONMENT=Production` e `/api/swagger` com 404 no deploy (US-17); reavaliar a base Alpine antes do deploy (`HL-30`); MCP `mssql` com `AUTH_FAILED` (`CG-05`); T-18.6 e T-18.7.

## Etapas

| # | Etapa | Agente | Status | Rodada | Gate | Data |
|---|---|---|---|---|---|---|
| 0 | Configuração AI-first (spec, plano, CLAUDE.md, hooks, skills, MCP, agentes) | orquestrador | Concluída | 2 | Configuração validada no CI (`config`). Build e testes .NET ainda sem código; Docker ausente na máquina (ver diário E04) | 2026-11-13 |
| 1 | Requisitos e backlog | product-owner | Concluída | 1 (tentativa 2) | Aprovado: matriz com 0 erro, configuração válida, 7 PROPs decididas (`HL-10` a `HL-16`) | 2026-11-13 |
| 2 | Arquitetura | arquiteto | Concluída | 1 | Aprovado: checklist com 55 itens verificáveis, 4 ADRs (0002 a 0005), matriz com 0 erro, configuração válida, 6 PROPs e o ADR-0004 decididos (`HL-17` a `HL-23`) | 2026-11-13 |
| 3 | Backend, por história | backend | Em andamento (US-02, US-03 parte 1, US-04 e US-05 integradas; US-06 pronta) | 1 (US-06) | US-06 aprovada: 0 aviso, 64/64 unitários, 77/77 de integração (2 execuções), com 1 correção do orquestrador na T-06.6 (`CG-06`). US-05 aprovada na rodada 1: 0 aviso, 19/19 unitários, 35/35 de integração. US-04 aprovada na rodada 2: 0 aviso, 19/19 unitários, 21/21 de integração contra SQL Server 2022 real, compose com `db` saudável e `db-init` com saída 0. Rodada 1 reprovada por collation (`CG-04`). US-02 aprovada na rodada 2: build com 0 aviso, 10/10 testes unitários, `dotnet format` sem mudanças, matriz com 0 erro, configuração válida. Rodada 1 reprovada por `default!` (BE-07, `CG-03`). Integração: não há testes ainda | 2026-11-13 |
| 4 | Frontend, por história | frontend | US-14, US-15 e US-16 integradas | 1 (US-16 na rodada 2) | US-16 aprovada: E2E 1/1 três vezes contra o compose | 2026-11-14 |
| 4b | Revisão de arquitetura | arquiteto | Concluída (`HL-28`: antes da US-17 e da US-12) | 3 | Aprovado: 4 achados (ARQ-01 a ARQ-04), todos Resolvidos; ARQ-02 precisou de 2 rodadas de correção, com prova de mutação repetida pelo orquestrador | 2026-11-14 |
| 5 | Segurança | ciberseguranca | Concluída (`HL-28`) | 3 | Aprovado: 0 Crítico/Alto; SEC-02 a SEC-04 e SEC-06 a SEC-08 Resolvidos; SEC-01 e SEC-05 aceitos (`HL-29`); resíduo da base Alpine aceito (`HL-30`) | 2026-11-14 |
| 6 | QA final | qa | Pendente | — | — | — |

## Histórias

| US | Dono | Status | Tarefas prontas / total | Commits |
|---|---|---|---|---|
| US-01 Fundação SDD e rastreabilidade | orquestrador | Concluída (PR #2) | 3 / 3 | 501ae41, 125952a, abf8b91 |
| US-02 Esqueleto da solução .NET | backend | Concluída (PR #4) | 2 / 2 | 088ea5b, 3d2ebe9 |
| US-03 Qualidade automatizada: CI e testes de arquitetura | backend | T-03.1 e T-03.2 integradas (PR #5); T-03.3 a T-03.5 no PR #20, CI verde (integração com 128 testes, E2E contra o compose) | 5 / 5 | 8b0384e, 86f7b47, 5503aa1, c5ebbaa, 181d359 |
| US-04 Banco de dados com schema, índices, logins e carga | backend | Concluída (PR #6) | 5 / 5 | 027a0ff, 75b834d, 97a88c1, d2c6c85, 994e5c0 |
| US-05 Base HTTP da API | backend | Concluída (PR #7) | 3 / 3 | 17b4a14, b786353, 09f0eb6 |
| US-06 Criar pedido com baixa atômica de estoque | backend | Concluída (PR #8) | 7 / 7 | 10d064c, 4f33672, 1b6564d, dac6332, 0506fe7, 22132a4, f969a53 |
| US-07 Catálogo de clientes e produtos para as telas | backend | Concluída (PR #9) | 1 / 1 | 61bb1c2 |
| US-08 Listar pedidos com filtros e paginação | backend | Concluída (PR #9) | 2 / 2 | 8b98d09, 59eae2d |
| US-09 Relatório de top 5 clientes | backend | Concluída (PR #9) | 2 / 2 | 15ae8c8, 9caca95 |
| US-10 Parte 4: debug e otimização da query | backend | Concluída (PR #15) | 3 / 3 | f37e391, 894a406, 9eb76b7 |
| US-11 Consultar pedido por id | backend | T-11.1 integrada (PR #19, merge `fa235a0`); T-11.2 depois da US-12 (`HL-28`) | 1 / 2 | a586754 |
| US-12 Idempotência na criação | backend | Pendente | 0 / 2 | — |
| US-13 Aplicação completa com um comando | backend | Concluída (PR #15) | 2 / 2 | ba65994, ffca2da |
| US-14 Tela de listagem de pedidos | frontend | Concluída (PR #9; contrato da T-14.2 a conferir na T-11.2) | 5 / 5 | 1779aa4, 9c9d50c, d1318ef, fe9cb83 |
| US-15 Tela de criação de pedido | frontend | Concluída (PR #9) | 3 / 3 | c757543, 32bc744 |
| US-16 Teste end to end com Playwright | frontend | Concluída (PR #17, merge `2b58763`) | 1 / 1 | b292800 |
| US-17 Deploy online | backend | Pendente | 0 / 2 | — |
| US-18 README e entrega | orquestrador | Integrada (PR #16, merge `287eb42`); T-18.6 e T-18.7 abertas | 5 / 7 | 2a47d6e, 061162b, e48c12e, 2e88278, 8e7c669 |

## Achados abertos

| ID | Severidade | Dono | Rodada | Status | Resumo |
|---|---|---|---|---|---|
| — | — | — | — | — | Nenhum aberto. Revisões 4b e 5 fechadas em 2026-11-14: ver [arquitetura-1.md](revisoes/arquitetura-1.md) e [05-seguranca.md](05-seguranca.md) |

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
| 2026-11-14 | Etapa 3, US-13, T-13.1 e T-13.2 (com 1 rodada de CORRECAO: erro 904 do db-init na recuperação do banco) | C2 | backend | sonnet | medium | 1 | Aprovado na rodada 2 | Não | 58.799 |
| 2026-11-14 | Etapa 3, US-10, T-10.1 a T-10.3 (41 min: 21,5 min em testes com carga de 800 mil linhas; ver `HL-27`) | C3 | backend | opus | high | 1 | Aprovado na rodada 1 | Não | 111.236 |
| 2026-11-14 | Etapa 4, US-16, T-16.1 (com 1 rodada de CORRECAO: quantidade 1 → 2 unidades do CA-30, erro da instrução do orquestrador) | C2 | frontend | sonnet | medium | 1 | Aprovado na rodada 2 | Não | 86.031 (42.127 + 43.904) |
| 2026-11-14 | Etapa 3, US-11, T-11.1 | C2 | backend | sonnet | medium | 1 | Aprovado na rodada 1 | Não | 67.691 |
| 2026-11-14 | Etapa 3, US-03, T-03.3 a T-03.5 (jobs de CI, `HL-28`) | C2 | backend | sonnet | medium | 1 | Aprovado na rodada 1; provado no CI do PR #20 | Não | 45.637 |
| 2026-11-14 | Etapa 4b, REVISAO rodada 1 (`HL-28`) | — | arquiteto | opus | high | 1 | 4 achados (1 Médio, 3 Baixos), nenhum Alto | Não | 54.172 |
| 2026-11-14 | Etapa 5, revisão de segurança (`HL-28`) | — | ciberseguranca | opus | high | 1 | 7 achados (2 Médios, 5 Baixos), nenhum Crítico ou Alto | Não | 144.414 |
| 2026-11-14 | CORRECAO rodada 1: ARQ-03, ARQ-04, SEC-02, SEC-07 (`HL-29`) | C2 | frontend | sonnet | medium | 1 | Aprovado no gate do orquestrador | Não | 51.587 |
| 2026-11-14 | CORRECAO rodada 1: ARQ-02, SEC-03, SEC-04 (`HL-29`) | C2 | backend | sonnet | medium | 1 | Aprovado no gate do orquestrador | Não | 77.451 |
| 2026-11-14 | Etapa 4b, re-revisão rodadas 2 e 3 (ARQ-01 a ARQ-04; ajustes de texto na ADR-0002 e no desenho) | — | arquiteto | opus | high | 1 | Rodada 2: ARQ-02 Parcial (Baixa → Média). Rodada 3: todos Resolvidos | Não | 105.382 (cumulativo da mesma instância; 87.701 na rodada 2) |
| 2026-11-14 | CORRECAO rodada 2: ARQ-02 (seletores do ArchUnitNET) | C2 | backend | sonnet | medium | 2 | Aprovado; mutação repetida pelo orquestrador | Não | 88.025 (cumulativo da instância da rodada 1) |
| 2026-11-14 | Etapa 5, re-revisão rodadas 2 e 3 (SEC-01 a SEC-08) | — | ciberseguranca | opus | high | 1 | Rodada 2: SEC-02 Parcial e SEC-08 novo (Média). Rodada 3: todos Resolvidos ou aceitos | Não | 120.271 (cumulativo da mesma instância; inclui a troca para "Aceito (HL-30)") |
| 2026-11-14 | CORRECAO rodada 2: SEC-02 (teste de cabeçalhos), SEC-08 (Nginx 1.30) e remoção do `curl` (`HL-30`) | C2 | frontend | sonnet | medium | 2 | Aprovado no gate do orquestrador | Não | 57.357 (cumulativo da instância da rodada 1) |

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
| 2026-11-14 | 3, US-13 (T-13.1 e T-13.2) | `docker compose config -q`, `docker compose up -d`, `curl` em 8 rotas de `:4200` (inclusive o proxy `/api`) e `:8080/swagger`, `id -u` nos contêineres, `docker compose down` sem `-v` | Rodada 1: `db-init` falhou com erro 904 (banco em recuperação), devolvido. Rodada 2: aprovado; `db`, `api` e `frontend` saudáveis, `db-init` com saída 0, 8/8 rotas com 200, uid 1654 (api) e 101 (frontend) |
| 2026-11-14 | 3, US-10 com US-13 (T-10.1 a T-10.3) | Os do backend, com a integração inteira (inclui a medição) | Aprovado: 0 aviso; 109/109 e 126/126 (6 min 24 s, dos quais 2 a 3 min da medição); format sem mudanças; busca limpa; matriz com 0 erro; configuração válida. MCP não verificado (`CG-05`); planos colhidos com o login `claude_mcp` no Testcontainers |
| 2026-11-14 | 4, US-16 (T-16.1) | `docker compose up -d`, `npm --prefix e2e test` três vezes seguidas sem limpar o banco, `docker compose down` sem `-v`, matriz e configuração | Aprovado: 1/1 nas três execuções (3,4 s na última); matriz com 0 erro; configuração válida. Lint, build e testes do frontend não rodados: `frontend/` não mudou |
| 2026-11-14 | 3, US-11 (T-11.1) | Os do backend, com integração `Categoria!=Medicao` (`HL-27`), e `git grep CreatedAtAction` | Aprovado: 0 aviso, busca limpa; 111/111 e 128/128 (contagem do log do CI do PR #20, que já inclui a T-11.1) |
| 2026-11-14 | 3, US-03 (T-03.3 a T-03.5) | SHAs de `setup-node` e `upload-artifact` conferidos pela API do GitHub; `gh pr checks 20` e log do job de integração | Aprovado: os 8 checks verdes; o job de integração rodou 128 testes (32 s); E2E contra o compose em 1 min 54 s |
| 2026-11-14 | Correção de achados, rodada 1 (`HL-29`) | Backend completo; frontend (lint, build, testes, `tsc` do e2e); `docker compose up -d --build --wait frontend`; `curl -D -` em `/`, `/pedidos`, `/api/clientes`, `/api/pedidos?dataFim=9999-12-31` e `main-*.js`; `docker compose ps`; `npm --prefix e2e test`; console do navegador | Aprovado: 0 aviso; 113/113 e 130/130; format e busca limpos; matriz com 0 erro; lint limpo, 27/27; os 4 cabeçalhos e a CSP em todas as respostas; `dataFim=9999-12-31` → 400; portas só em 127.0.0.1; E2E 1/1; nenhuma violação de CSP e CSS sem `media=print` |
| 2026-11-14 | Correção de achados, rodada 2 (ARQ-02, SEC-02, SEC-08, `HL-30`) | Build, unitários, format, busca; mutação independente (controller temporário dependendo do `PedidosDbContext`); `docker compose build --pull frontend` e `up --wait`; `nginx -t`, `nginx -v`, `id -u`, `command -v curl` no contêiner; `npm --prefix e2e test`; `docker scout cves --only-severity critical,high` | Aprovado: 0 aviso, 113/113; regra dos controllers vermelha com a mutação e verde sem ela; nginx 1.30.6, uid 101, sem `curl`, healthy; E2E 2/2 (CA-30 e cabeçalhos); resíduo crítico/alto só em brotli, expat, libxml2, libxslt, musl e zlib (aceito, `HL-30`) |
