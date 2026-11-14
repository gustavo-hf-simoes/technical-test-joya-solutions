# Diário de bordo do trabalho com IA

> Registro cronológico de **como** o projeto foi construído com o Claude Code: o que o desenvolvedor pediu, o que a IA produziu, o que foi corrigido ou rejeitado, e onde está a evidência. Este diário cobre a parte que não aparece no código. Os commits cobrem o resto.
>
> - Mantido pelo orquestrador (sessão principal), com uma entrada por interação relevante. A regra está na skill [`/esteira`](../../.claude/skills/esteira/SKILL.md), passo 5.
> - Horários em BRT (UTC-3), tirados do log da sessão do Claude Code.
> - Os prompts do desenvolvedor, na íntegra, estão em [prompts.md](prompts.md). O roteiro do vídeo de entrega está em [roteiro-video.md](roteiro-video.md).

## Como ler

| Coluna / termo | Significado |
|---|---|
| `HL-xx` | Decisão humana: o desenvolvedor discordou, ajustou ou acrescentou algo à proposta da IA ([plan.md](../plan.md), seção Human-in-the-Loop) |
| `CG-xx` | Erro da IA detectado e corrigido ([plan.md](../plan.md), seção Correções em código gerado) |
| `PROP-xx` | Proposta de mudança na spec levantada por um agente, que espera decisão humana |
| Modelo | Modelo que executou a etapa. Na fase de configuração foi tudo na sessão principal (Opus 5.5). A partir da etapa 1, cada delegação também aparece em [status.md](../esteira/status.md), seção *Execuções de agentes* |

## Linha do tempo

### E01 — Especificação e configuração AI-first · 2026-11-13 11:47 → 12:00

- **Pedido** ([prompt 1](prompts.md#prompt-1)). Analisar o enunciado e criar, sem código de implementação:
  - `spec.md` e `plan.md`, este com a seção "Proposta Inicial da IA";
  - `CLAUDE.md`;
  - um hook de build e testes;
  - a skill `/new-endpoint`;
  - o MCP do SQL Server.
- **O que a IA produziu:**
  - [spec.md](../spec.md): regras `RN-xx`, contratos, modelo de dados e critérios `CA-xx` em BDD;
  - [plan.md](../plan.md): fases 0–9 e as decisões `D1`…;
  - [CLAUDE.md](../../CLAUDE.md): arquitetura, regras de ouro, nomenclatura e comandos;
  - [settings.json](../../.claude/settings.json) e [post-edit.sh](../../.claude/hooks/post-edit.sh);
  - [new-endpoint](../../.claude/skills/new-endpoint/SKILL.md);
  - MCP com o DBHub ([.mcp.json](../../.mcp.json), [dbhub.toml](../../.claude/mcp/dbhub.toml)), com login só de leitura.
- **Decisão técnica da IA, aceita:**
  - o prompt pedia `.claude/mcp.json`, mas o Claude Code lê os servidores do projeto em `.mcp.json`, na raiz;
  - foi criado um lançador em Node ([dbhub.mjs](../../.claude/mcp/dbhub.mjs)) para o mesmo `.mcp.json` funcionar em Windows, macOS e Linux.
- **Modelo:** Opus 5.5, na sessão principal.

### E02 — Da execução "numa pancada" para a esteira SDD · 2026-11-13 19:21 → 19:50

- **Pedido** ([prompt 2](prompts.md#prompt-2)). O desenvolvedor rejeitou o plano executado de uma vez por uma única sessão. Pediu:
  - uma esteira com papéis: PO → arquitetura → backend → frontend → segurança → QA;
  - backlog épico → história → tarefa;
  - achados que voltam ao dono e são re-revisados pelo mesmo revisor;
  - pesquisa no skills.sh, no GitHub e no mercado antes de criar qualquer coisa do zero.
- **Pesquisa feita pela IA:**
  - comparou kits de convenção (GitHub Spec Kit, Kiro), frameworks de papéis (BMAD) e agentes e skills nativos do Claude Code;
  - auditou skills oficiais (dotnet, testcontainers, angular, getsentry) antes de vendorizar.
- **Pergunta ao desenvolvedor.** Abordagem: kit, framework ou híbrido? Resposta: **"Sim, híbrido"**.
- **O que a IA produziu:**
  - [esteira/README.md](../esteira/README.md), que inclui o ADR-0000 da abordagem;
  - 6 agentes em [.claude/agents/](../../.claude/agents/);
  - a skill [/esteira](../../.claude/skills/esteira/SKILL.md), em que o orquestrador é a sessão principal;
  - os hooks [guard-paths.sh](../../.claude/hooks/guard-paths.sh) e [guard-bash.sh](../../.claude/hooks/guard-bash.sh), que impõem os limites de cada agente pelo harness e não só pelo prompt;
  - 10 skills vendorizadas com procedência em [VENDORED.md](../../.claude/skills/VENDORED.md);
  - [enunciado.md](../requisitos/enunciado.md) com as linhas numeradas (`ENU-Lxxx`), que servem de rastreabilidade;
  - os templates e o [status.md](../esteira/status.md).
- **Decisões humanas:**
  - `HL-01`: esteira por etapas;
  - `HL-02`: vendorizar skills oficiais em vez de criar genéricas;
  - `HL-03`: autenticação fora do escopo, registrada como risco aceito.
- **Erro da IA (`CG-01`).** A IA afirmou que subagentes não podem criar subagentes. Ela conferiu na documentação oficial e corrigiu: o aninhamento é permitido. O motivo real para o orquestrador ser a sessão principal é que só ela tem `AskUserQuestion`, de que os gates humanos dependem.
- **Modelo:** Opus 5.5. A sessão teve o contexto compactado duas vezes nesta fase. Os documentos em `docs/` funcionaram como memória persistente, e é por isso que a spec é a fonte de verdade, e não a conversa.

### E03 — Custo de IA, diferenciais e estratégia de commits · 2026-11-13 19:50 → 20:00

- **Pedido** ([prompt 3](prompts.md#prompt-3)). O desenvolvedor pediu três coisas:
  - modelo forte para análise e tarefas complexas e modelo barato para tarefas simples e bem documentadas, de forma visível ao avaliador;
  - Playwright, GitHub Actions e deploy online como compromisso, e não "se sobrar tempo";
  - um plano para os arquivos que ainda não tinham nenhum commit.
- **Pesquisa feita pela IA:**
  - preços oficiais por milhão de tokens: Haiku 5.5 custa 1/40 do Opus 5.5, e o Fable 5.2 custa o dobro do Opus;
  - ordem de resolução de `model` e `effort` dos subagentes no Claude Code;
  - requisito de memória do SQL Server 2022 (≥ 2 GB, x86-64) para o deploy.
- **Perguntas ao desenvolvedor** (todas respondidas com a opção recomendada):
  - deploy numa VM com compose e Caddy;
  - roteamento em 3 níveis com escalonamento;
  - cerca de 12 commits lógicos no bootstrap;
  - uma branch por história, com PR.
- **O que a IA produziu:**
  - [ADR-0001](../adr/0001-roteamento-de-modelos.md), o roteamento de modelos:
    - modelo padrão por agente no frontmatter;
    - complexidade `C1`/`C2`/`C3` por tarefa do backlog, que escolhe Haiku, Sonnet ou Opus;
    - escalonamento depois de 2 falhas de gate;
  - o agente [verificador](../../.claude/agents/verificador.md), no Haiku, que roda os gates e mantém os logs longos fora do contexto do Opus;
  - a tabela *Execuções de agentes* no [status.md](../esteira/status.md).
- **Decisões humanas:**
  - `HL-04`: os diferenciais viram compromisso, com CI logo depois do esqueleto e deploy cedo com CD;
  - `HL-05`: roteamento de modelos;
  - `HL-06`: estratégia de commits.
- **Modelo:** Opus 5.5.

### E04 — Auditoria do ambiente, repositório e CI · 2026-11-13 20:05 → 20:25

- **Pedido** ([prompt 4](prompts.md#prompt-4)). O desenvolvedor contestou a afirmação de que o git não estava instalado. Pediu:
  - investigar a causa;
  - preparar o ambiente;
  - criar o repositório e o GitHub Actions;
  - criar este diário para a entrevista e o vídeo;
  - validar se dá para iniciar e sugerir diferenciais.
- **Erro da IA (`CG-02`).** Na primeira verificação do ambiente (11:50), o comando devolveu `git version 2.55.0.windows.5`, ou seja, o git estava instalado. A informação do ambiente "*Is a git repository: false*" dizia apenas que a **pasta** ainda não era um repositório. A IA confundiu "pasta sem repositório" com "git ausente" e repetiu isso no `status.md` e na resposta ao desenvolvedor. Os outros dois diagnósticos estavam certos:
  - o .NET tinha só o runtime 10, sem SDK (`No .NET SDKs were found`);
  - o Docker não estava instalado.
- **Lição registrada:** cada afirmação sobre o ambiente precisa citar a saída do comando que a sustenta. É a mesma regra do gate da esteira, "sem evidência, `Não verificado`", aplicada à própria IA.
- **Ambiente verificado, com o comando de cada linha:**

  | Ferramenta | Estado | Evidência |
  |---|---|---|
  | git | 2.55.0, sem `user.name`/`user.email` global | `git --version`, `git config --show-origin --list` |
  | GitHub CLI | 2.103.3, logado como `gustavo-hf-simoes`, escopos `repo` e `workflow` | `gh auth status` |
  | .NET SDK | 8.0.428, **instalado nesta etapa** com autorização (`winget install Microsoft.DotNet.SDK.8`) | `dotnet --list-sdks` |
  | Node / npm | 24.19.0 / 11.17.0 | `node -v`, `npm -v` |
  | Docker | **ausente**; a distro Ubuntu do WSL 2 está quebrada (`ERROR_PATH_NOT_FOUND` no `.vhdx`) | `which docker`, `wsl -l -v` |

- **Decisões humanas:**
  - repositório público, `technical-test-joya-solutions`;
  - autor dos commits com o e-mail `noreply` do GitHub;
  - a IA instala o .NET e o desenvolvedor instala o Docker Desktop;
  - `HL-07`: este diário.
- **O que a IA fez:**
  - `.gitattributes` com LF obrigatório para os `.sh`. O git do Windows usa `core.autocrlf=true`, e com CRLF os hooks quebram no Linux;
  - `.gitignore`;
  - o workflow [ci.yml](../../.github/workflows/ci.yml), com o job `config`, que valida o frontmatter, os JSON, os links e 18 casos de teste dos hooks de limite. A action é fixada por SHA, com `permissions: contents: read`;
  - um README inicial;
  - `git init -b main` e 15 commits de bootstrap na ordem real de criação, com a data real (`HL-06`);
  - a criação de [github.com/gustavo-hf-simoes/technical-test-joya-solutions](https://github.com/gustavo-hf-simoes/technical-test-joya-solutions), público;
  - merge commit como único método de merge no GitHub, com squash e rebase desligados. Assim a `HL-06` é imposta pela plataforma, e não só combinada.
- **Gate:** o CI passou na primeira execução, run `43525593188`, em 18 s.
- **Modelo:** Opus 5.5.

### E05 — Diferenciais além do enunciado (`HL-08`) · 2026-11-13 20:29 → 20:45

- **Pedido** ([prompt 5](prompts.md#prompt-5)). Das 8 sugestões da IA (4 recomendadas e 4 opcionais), o desenvolvedor escolheu as **4 recomendadas**.
- **Verificação antes de adotar.** A IA conferiu o histórico no NuGet antes de colocar a biblioteca de testes de arquitetura no plano:
  - o `NetArchTest.Rules`, que ela mesma tinha sugerido, não tem versão nova desde 2021-05-23;
  - o `TngTech.ArchUnitNET` teve a versão 0.13.5 em 2026-10-26, sob Apache-2.0.
  O plano ficou com o ArchUnitNET. A mudança está registrada na `HL-08`, e não como `CG`, porque foi pega antes de virar código.
- **O que a IA fez:**
  1. **Segurança da cadeia, no GitHub:**
     - `main` protegida: PR obrigatório; checks exigidos `Configuração AI-first`, `CodeQL (actions)` e `CodeQL (python)`; branch atualizada antes do merge; conversas resolvidas; sem force-push nem exclusão; vale também para administradores;
     - *secret scanning* com *push protection*;
     - alertas e atualizações de segurança do Dependabot;
     - relato privado de vulnerabilidades;
     - [codeql.yml](../../.github/workflows/codeql.yml), com `security-extended` e uma execução semanal;
     - [dependabot.yml](../../.github/dependabot.yml).
     O C# e o TypeScript entram na matriz do CodeQL e no Dependabot junto com o esqueleto. Sem código, o CodeQL falha.
  2. **Rastreabilidade:**
     - [rastreabilidade.md](../esteira/rastreabilidade.md), com as regras e a tabela, que o PO preenche na etapa 1;
     - [validar_rastreabilidade.py](../../.github/scripts/validar_rastreabilidade.py), que confere 6 regras, entre elas: toda linha do enunciado (75 não vazias) classificada, requisito obrigatório com teste, e teste e commit citados existentes de fato;
     - 9 casos de teste do próprio validador em [testar_rastreabilidade.sh](../../.github/scripts/testar_rastreabilidade.sh);
     - os agentes `product-owner` e `qa` e a skill `/esteira` passaram a usar a matriz.
  3. **Testes de arquitetura:** a decisão e a justificativa da dependência estão na `HL-08`. As tarefas saem no backlog da etapa 1.
  4. **Custo medido:** a skill `/esteira` passou a copiar os tokens de cada delegação para *Execuções de agentes*. A seção de custo do README sai dessa tabela.
  - Junto: o runner do CI foi fixado em `ubuntu-24.04`, porque o GitHub avisou que `ubuntu-latest` vai migrar para o Ubuntu 26.
- **Consequência para o processo.** Com a `main` protegida, a partir daqui **toda** mudança entra por PR, inclusive esta. O primeiro PR do repositório é o desta entrada.
- **Modelo:** Opus 5.5.

### E06 — Etapa 1: requisitos e backlog (`product-owner`) · 2026-11-13 20:53 → 21:35

- **Pedido** ([prompts 6 a 8](prompts.md#prompt-6)): com o Docker instalado, iniciar a etapa 1 da esteira.
- **Episódio da sessão anterior.** A sessão que recebeu os prompts 6 a 8 não conseguiu rodar a etapa, por dois motivos:
  - a skill falhou com `Unknown skill: esteira` (20:53), e os agentes de `.claude/agents/` também não apareciam como tipos de agente. Eles tinham sido criados durante a própria sessão e só são carregados quando a sessão começa;
  - o PATH da sessão era anterior à instalação do Docker, e `docker` respondeu `docker: command not found` (20:59). O daemon também ainda não estava no ar.

  Ela registrou o próximo passo em `status.md` e a etapa foi para uma sessão nova. Esta sessão conferiu os três pontos antes de delegar (`CG-02`): `docker info` com `Server Version: 29.9.3`, os agentes do projeto disponíveis e a skill `/esteira` carregada.
- **Tentativa 1 parada pelo desenvolvedor, e o porquê** ([prompts 9 a 11](prompts.md#prompt-9)). O `product-owner` (Opus, esforço `high`) rodou 13 min sem gravar nada. A IA mediu a transcrição:
  - leu 25 arquivos em 70 s, inclusive agentes de outros papéis, a skill `/new-endpoint` e `.git/logs/HEAD`;
  - raciocinou 3,5 min, com 23.018 tokens de saída, e só depois fez dois `Grep`;
  - entrou numa segunda geração de cerca de 8 min, perdida na parada.

  A causa foi raciocínio demais, e não a leitura. Parte da culpa foi da delegação, que não limitou as entradas nem a ordem de escrita. **`HL-09`:** o esforço do PO caiu para `medium`, o modelo continua Opus, e a delegação passou a ter lista fechada de entradas e escrita incremental.
- **Tentativa 2:** 9,6 min, três artefatos gravados:
  - [01-requisitos.md](../esteira/01-requisitos.md): 11 RF, 9 RNF, 23 ENT e 7 PROPs;
  - [backlog.md](../esteira/backlog.md): 1 épico, 18 histórias e 56 tarefas (7 C1, 34 C2, 15 C3);
  - [rastreabilidade.md](../esteira/rastreabilidade.md): as 75 linhas do enunciado classificadas, com 46 obrigatórias, todas com tarefa.
- **Gate (rodado pelo orquestrador):**
  - `validar_rastreabilidade.py` deu 0 erro;
  - `validar_config.py` reprovou duas vezes por um link quebrado no texto da PROP-07. Na primeira correção, o PO pôs o link entre crases, mas a regex do validador não ignora crases. Na segunda, tirou a sintaxe de link;
  - na terceira rodada, os dois validadores passaram.
- **Decisões humanas:** as 7 PROPs foram aceitas com a opção recomendada, em 2 rodadas de `AskUserQuestion`, e viraram as `HL-10` a `HL-16`. O orquestrador aplicou o texto na [spec](../spec.md):
  - CA-33 a CA-38;
  - RNF-08;
  - RN-14 com a corrida na idempotência;
  - RNF-04 e RNF-06;
  - `413` e `429` no catálogo de erros;
  - premissa P7;
  - a §9 apontando a matriz oficial.

  O `CLAUDE.md` passou a usar `docker compose up` sem flags (`HL-15`). O código `LIMITE_DE_TAXA` do `429` foi nomeado pelo orquestrador ao aplicar a PROP-03, e isso está registrado na `HL-12`.
- **Modelo:** orquestrador no Opus 5.5 (esforço high); `product-owner` no Opus, `high` na tentativa 1 e `medium` na tentativa 2. Tokens em [status.md](../esteira/status.md), *Execuções de agentes*.

### E07 — Integração da etapa 1 e etapa 2: arquitetura (`arquiteto`) · 2026-11-13 21:36 → 22:12

- **Pedido** ([prompts 12 a 14](prompts.md#prompt-12)): autorizar os commits e o PR da etapa 1, integrar o PR, seguir para a etapa 2 e, no fim, autorizar os commits e o PR dela.
- **Etapa 1 integrada:**
  - 6 commits, um por tarefa. A `HL-09` foi separada do `plan.md` por staging parcial com `git apply --cached`;
  - o commit `docs(esteira): rastreia US-01` preencheu a coluna Commits (`HL-08`);
  - o PR #2 passou no CI (CodeQL e Configuração AI-first) e foi integrado com merge commit (`97196cd`).
- **Delegação ao `arquiteto`** (Opus, `high`). A lição da `HL-09` foi aplicada sem mudar o esforço: lista fechada de entradas e escrita incremental. Em 18,6 min, ele gravou:
  - [02-arquitetura.md](../esteira/02-arquitetura.md), com as restrições da vaga, o C4 nível 2, as camadas com 9 testes de arquitetura, a sequência do `POST /pedidos`, os contratos, os índices com o operador esperado e um checklist de 55 itens, cada um com o comando que o verifica;
  - os ADRs [0002](../adr/0002-limites-contra-abuso.md) (32 KiB e 10 `POST`s a cada 10 s por IP), [0003](../adr/0003-banco-real-nos-testes-de-integracao.md) (o mesmo `init.sh` no compose e no Testcontainers), [0004](../adr/0004-unidade-de-trabalho-sem-retentativa.md) (sem *retry* automático) e [0005](../adr/0005-contrato-openapi-versionado.md) (contrato OpenAPI versionado).
- **Gate (rodado pelo orquestrador):** li o documento e o checklist. Os dois validadores passaram. O próprio orquestrador respondeu a pergunta sobre a permissão de `docs/contratos/` lendo o `guard-paths.sh` do `backend`, que já cobre o caminho.
- **Decisões humanas:** 8 perguntas em 2 rodadas, todas aceitas na opção recomendada:
  - as PROP-08 a PROP-13 e o ADR-0004 viraram as `HL-17` a `HL-23`. O ADR-0004 revê a tarefa 3.2 e uma linha de risco do plano, que usavam `CreateExecutionStrategy`;
  - o orquestrador aplicou o texto na spec (RNF-02, RN-13, §5.1, §5.5, RNF-06 e §8.4) e no `plan.md`;
  - a oitava pergunta mandou os ajustes do backlog para o `product-owner`.
- **Ajuste do backlog** (`product-owner`, Opus `medium`, 3 min):
  - tarefa nova T-11.2 (contrato), agora com 57 tarefas;
  - T-03.2 com as 9 regras de arquitetura; o `init.sh` foi para a T-04.1;
  - T-06.4 sem *retry*, T-05.3 sem a condição, T-15.2 com a `Idempotency-Key`;
  - testes novos para as `HL-18` a `HL-20`;
  - numa segunda passada, as marcas de PROP já decididas viraram referências às `HL`.
- **Modelo:** orquestrador no Opus 5.5; `arquiteto` no Opus `high`; `product-owner` no Opus `medium`. Tokens em [status.md](../esteira/status.md).

### E08 — Ambiente de modelos e etapa 3: US-02, esqueleto (`backend`) · 2026-11-13 22:12 → 22:40

- **Pedido** ([prompts 15 a 19](prompts.md#prompt-15)): integrar a etapa 2 e seguir para a etapa 3. Depois da falha dos subagentes, o desenvolvedor pediu para verificar se o processo era *overengineering* e aprovou a alternativa como exceção (`HL-24`).
- **Falha de ambiente:**
  - a T-02.1 falhou duas vezes com `model_not_found`;
  - o `~/.claude/settings.json` global levava `haiku`/`sonnet`/`opus` a um *gateway* local (`localhost:20128`) fora do ar. Com ele no ar, C2 e C3 cairiam no mesmo modelo, e o ADR-0001 viraria só rótulo;
  - autocrítica: o orquestrador gastou tempo procurando o programa do *gateway* quando `curl → 000` já bastava para decidir.
- **Correção (`HL-24`):** com aval explícito, o orquestrador removeu as substituições da configuração global (com cópia de segurança). Mais duas lições:
  - reiniciar só a sessão não bastou, porque o app desktop guarda o `env` lido quando abre. Foi preciso sair do app pela bandeja;
  - um teste de 11 mil tokens com o `verificador` confirmou o conserto antes da tarefa real.
- **Esteira mais leve (`HL-24`):** a T-02.1 e a T-02.2 foram numa delegação só, com um registro por história e um commit por tarefa.
- **Delegação ao `backend`** (Sonnet `medium`, 4 min): solução com 5 projetos, `Directory.Build.props` (`TreatWarningsAsErrors`, `AnalysisLevel=latest`), `.editorconfig`, `global.json`, `Result<T>`, `Error`/`TipoErro` e `PaginaResponse<T>`, com 10 testes. O próprio teste apanhou um bug: `Value` não lançava na falha.
- **Gate (rodado pelo orquestrador):**
  - rodada 1: tudo verde, mas a busca por `!` achou um `default!` no teste (`CG-03`). O caso voltou ao dono e foi corrigido em 40 s;
  - rodada 2: aprovada.
- **Decisões a conferir pelo desenvolvedor:**
  - `AnalysisLevel=latest`, e não `latest-recommended`, porque o modo recomendado barra os nomes `Error` e `Result<T>.Failure`, fixados pela spec;
  - CA1707 desligado só em `tests/**`, pela convenção `Metodo_Cenario_Resultado`.
- **Modelo:** orquestrador no Opus 5.5; `backend` no Sonnet `medium`; `verificador` no Haiku (teste de ambiente).

### E09 — Etapa 3: US-03, parte 1, CI e testes de arquitetura (`backend`) · 2026-11-13 22:40 → 22:50

- **Pedido** ([prompt 21](prompts.md#prompt-21)): integrar o PR #4 (US-02, merge `a0a29ce` com o CI verde) e seguir para a US-03.
- **Escopo:** só a T-03.1 e a T-03.2. A T-03.3 depende da US-04, a T-03.4 da US-14 e a T-03.5 da US-16. Foram numa delegação só (`HL-24`).
- **Delegação ao `backend`** (Sonnet `medium`, 5 min):
  - job `backend` no `ci.yml`, com as actions fixadas por SHA, `persist-credentials: false` e `permissions: contents: read`;
  - 9 testes de arquitetura com ArchUnitNET e reflexão (`HL-08`).
- **Regras sem alvo:** as regras que ainda não têm tipos para verificar usam `WithoutRequiringPositiveResults()` com comentário. Duas só entram em vigor quando os tipos nascerem: setter das entidades e `CancellationToken` nos `*Async`.
- **Prova do critério de aceite:** o agente pôs EF Core no Core, viu a regra falhar e reverteu.
- **Gate (rodado pelo orquestrador):** aprovado na rodada 1. O orquestrador conferiu o SHA do `setup-dotnet` contra a tag e leu os testes para descartar asserção vazia.
- **Modelo:** orquestrador no Opus 5.5; `backend` no Sonnet `medium`.

### E10 — Etapa 3: US-04, banco de dados (`backend`) · 2026-11-13

- **Pedido** ([prompt 23](prompts.md#prompt-23) e [prompt 24](prompts.md#prompt-24)): integrar o PR #5 e seguir para a US-04; depois, continuar quando o disco C: foi liberado.
- **Duas delegações por nível** (`HL-24`): T-04.1 a T-04.3 (C3) no Opus `high`; T-04.4 (C1) e T-04.5 (C2) no Sonnet `medium`.
- **Ambiente:** o *pull* da imagem do SQL Server encheu o disco C: e derrubou o Docker. O orquestrador não registrou o gate até rodar os testes de verdade.
- **`CG-04`:** os testes de catálogo compilavam e passavam no format, mas falharam contra o servidor real (erro 468 de collation). Voltaram ao `backend` no mesmo nível e passaram sem mudar nenhuma asserção. Os textos esperados do catálogo bateram sem ajuste.
- **`CG-05`:** a senha padrão do MCP, escrita na fase de configuração, continha o nome do login, e o SQL Server a recusa (erro 33064). O `backend` achou ao subir o compose. O orquestrador corrigiu o `.mcp.json`, que é configuração e não spec.
- **Entregas:** schema com `READ_COMMITTED_SNAPSHOT`, índices da spec, logins de menor privilégio (`DENY DELETE`), carga idempotente com homônimos e pedidos de 2025 e 2026, e compose com `db` e `db-init`. 21 testes de integração.
- **Gate (rodado pelo orquestrador):** reprovado na rodada 1 e aprovado na rodada 2.
- **Modelo:** orquestrador no Opus 5.5; `backend` no Opus `high` (C3) e no Sonnet `medium` (C1/C2).

### E11 — Etapa 3: US-05, base HTTP da API (`backend`) · 2026-11-13

- **Pedido** ([prompt 25](prompts.md#prompt-25)): acompanhar o CI do PR #6 e seguir a esteira. O CI passou, e o orquestrador integrou o PR (`4162800`).
- **Duas delegações em sequência**, porque as três tarefas mexem no `Program.cs`. T-05.1 e T-05.2 (C2) foram no Sonnet `medium`; a T-05.3 (C3) foi no Opus `high`. Cada bloco do `Program.cs` foi marcado com a tarefa, para um commit por tarefa (`HL-24`).
- **Entregas:** Problem Details com `code` e `traceId`, 500 sem stack trace (CA-38), 415, Swagger, health checks `live` e `ready` com 503 quando o banco cai (CA-36), limite de corpo (413) e de taxa (429) por IP, configurável (`HL-12`, `HL-19`).
- **Desenho sem PROP:** o `POST /pedidos` só nasce na US-06. O `backend` aplicou a política nomeada a uma rota só de teste e deixou o roteiro para a T-06.4. O TestServer não aplica o limite do Kestrel; por isso o 413 sai de um filtro global (opção 2 do ADR-0002), e o caso *chunked* fica para o QA.
- **Gate (rodado pelo orquestrador):** aprovado na rodada 1 nas duas delegações.
- **Modelo:** orquestrador no Opus 5.5; `backend` no Sonnet `medium` (C2) e no Opus `high` (C3).

### E12 — Etapa 3: US-06, criar pedido com baixa atômica (`backend`) · 2026-11-14

- **Pedido** ([prompt 26](prompts.md#prompt-26) e [prompt 27](prompts.md#prompt-27)): commits e PR da US-05; depois, integrar o PR #7 (`ecef84b`) e rodar a US-06.
- **Quatro delegações, uma por nível e na ordem das dependências** (`HL-24`): T-06.1 a T-06.3 no Core (Sonnet `medium`), T-06.4 e T-06.5 na Infrastructure (Opus `high`), T-06.6 na Api (Sonnet `medium`) e T-06.7, concorrência (Opus `high`).
- **Entregas:** entidades com total em `decimal`, validator RN-01 a RN-04, `PedidoService.CriarAsync` com baixa em ordem de `ProdutoId`, mapeamento EF sobre o schema sem migrations, `UnitOfWork` sem retry (`HL-23`), `UPDATE … OUTPUT inserted.Preco` atômico, `POST /pedidos` com 201, 400, 409, 413, 422 e 429 e o teste de 20 requisições simultâneas (CA-10). O agente provou o teste com três mutações (sem `Estoque >= q`, *check-then-act*, sem ordenação), todas detectadas.
- **Correção do orquestrador (`CG-06`):** na T-06.6 o `title` dos erros saía genérico, fora da spec 5.2. O agente sugeriu PROP; o orquestrador recusou o desvio e devolveu a tarefa, resolvida na Api sem mudar a spec.
- **Gate (rodado pelo orquestrador):** aprovado. 0 aviso, 64/64 unitários e 77/77 de integração. MCP `mssql` não verificado (`CG-05`, falta reiniciar).
- **Modelo:** orquestrador no Opus 5.5; `backend` no Sonnet `medium` (C2) e no Opus `high` (C3).

### E13 — Etapas 3 e 4 em paralelo: US-07, US-08, US-09, US-14 e US-15 · 2026-11-14

- **Pedido** ([prompt 28](prompts.md#prompt-28), [prompt 29](prompts.md#prompt-29) e [prompt 30](prompts.md#prompt-30)): commits e PR da US-06; depois, integrar o PR #8 e acelerar, com prazo em 2026-11-15 12h25, menos créditos e menos disco no C:.
- **Decisões humanas:** `HL-25` (uma delegação por história ou par de histórias pequenas, Opus só nas tarefas C3 críticas, backend e frontend em paralelo em pastas disjuntas, integração uma vez por história, um PR por rodada) e `HL-26` (obrigatório primeiro; extras depois, na ordem US-16, US-11, US-17, US-12). O desenvolvedor autorizou os commits por tarefa da rodada inteira de uma vez.
- **Cinco delegações:** US-07 (Sonnet), US-14 (Sonnet, em paralelo), US-08 com T-09.1 (Sonnet), T-09.2 (Opus `high`) e US-15 (Sonnet, em paralelo com a T-09.2). Cache do npm e testes do Angular no Chrome já instalado, sem baixar nada no C:.
- **Correção do orquestrador:** a busca por silenciamento do gate pegou um `default!` num teste unitário da T-08.2 (regra de ouro 1). Devolvido em CORRECAO e resolvido em uma rodada. Também corrigi na rastreabilidade o nome de um teste que não existia (`GetProdutos_AtivoInvalido_Retorna400` → `GetCatalogo_AtivoInvalido_Retorna400`).
- **Desvio de processo:** T-14.3 com T-14.4, e T-15.1 com T-15.3, saíram em um commit cada, porque dividem o mesmo componente; separar trechos de template à mão custaria tempo sem ganho (`HL-25`).
- **Gate (rodado pelo orquestrador):** aprovado. Backend: 0 aviso, 109/109 unitários e 118/118 de integração. Frontend: lint limpo, build sem aviso, 24/24. MCP `mssql` não verificado (`CG-05`); a T-09.2 trouxe o SQL gerado pelo EF como evidência.
- **Modelo:** orquestrador no Opus 5.5; `backend` e `frontend` no Sonnet `medium` (C2) e `backend` no Opus `high` (T-09.2).

<!-- Próximas entradas: E14 — Etapa 3 (backend), ... Uma por etapa da esteira, com o modelo usado, o gate e os HL/CG gerados. -->

## Números para a entrevista

Atualize a cada etapa.

| Métrica | Valor |
|---|---|
| Decisões humanas registradas (`HL`) | 26 |
| Erros da IA detectados e corrigidos (`CG`) | 6, mais 1 recomendação revista antes de virar código (NetArchTest → ArchUnitNET) |
| Perguntas estruturadas da IA ao desenvolvedor | 27, em 9 rodadas |
| Compactações de contexto da sessão | 8 |
| Tarefas por nível (`C1`/`C2`/`C3`) | 7 / 35 / 15 (57 tarefas em 18 histórias) |
| Taxa de escalonamento de modelo | 0 de 21 delegações (mais 2 que não rodaram por erro de ambiente, `HL-24`). O ajuste da `HL-09` baixou o esforço, e não subiu o modelo |
