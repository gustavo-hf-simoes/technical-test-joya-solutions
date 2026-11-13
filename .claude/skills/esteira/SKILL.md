---
name: esteira
description: Orquestra a esteira SDD por etapas (PO → arquitetura → backend → frontend → segurança → QA) do projeto Mini Gestão de Pedidos. Lê docs/esteira/status.md, delega a etapa corrente ao agente dono, confere o gate por conta própria, roteia achados ao dono, pede decisões humanas e propõe um commit por tarefa. Use com "/esteira" (próximo passo), "/esteira status", "/esteira etapa <n>" ou "/esteira US-xx".
---

# Orquestrador da esteira SDD

Você, **a sessão principal**, é o orquestrador. Esta skill não roda como subagente porque só a sessão principal tem `AskUserQuestion`, e os gates humanos dependem disso. Os agentes de `.claude/agents/` executam as etapas. Você delega, confere, roteia e registra.

Leia antes de agir: `docs/esteira/README.md` (regras), `docs/esteira/status.md` (estado) e, quando a etapa pedir, `docs/esteira/backlog.md`.

## Argumentos

| Chamada | O que fazer |
|---|---|
| `/esteira` | Executar o **próximo passo** indicado em `status.md` |
| `/esteira status` | Resumir o estado: etapa, gate, achados abertos por dono, rodadas. Não delega nada |
| `/esteira etapa <n>` | Executar ou reexecutar a etapa `n`, se os gates anteriores estiverem aprovados |
| `/esteira US-xx` | Rodar as etapas 3 e 4 só para a história indicada |
| `/esteira achados` | Rodar uma rodada de correção dos achados abertos |

## Etapas e donos

| # | Etapa | Agente | Saída | Gate (você confere) |
|---|---|---|---|---|
| 1 | Requisitos e backlog | `product-owner` | `01-requisitos.md`, `backlog.md` | Toda linha `Obrigatório` do enunciado tem tarefa; `PROP-xx` decididas pelo humano |
| 2 | Arquitetura | `arquiteto` (DESENHO) | `02-arquitetura.md`, `docs/adr/` | Checklist de conformidade com comando de verificação; `PROP-xx` decididas |
| 3 | Backend, por história | `backend` | código + `03-backend.md` | `dotnet build`, testes unitários e de integração, `dotnet format` verdes, **rodados por você** |
| 4 | Frontend, por história | `frontend` | código + `04-frontend.md` | lint, build, testes e E2E verdes, rodados por você |
| 4b | Revisão de arquitetura | `arquiteto` (REVISAO) | `revisoes/arquitetura-<r>.md` | Nenhum `ARQ` Alto em aberto |
| 5 | Segurança | `ciberseguranca` | `05-seguranca.md` | Nenhum `SEC` Crítico ou Alto em aberto |
| 6 | QA final | `qa` | `06-qa.md` | Todas as linhas obrigatórias como `Atende`, ou aceite humano registrado |

As etapas 1 e 2 são globais. As etapas 3 e 4 iteram pelas ondas do backlog, uma história por vez: a história de backend antes da de frontend que depende dela. As etapas 4b, 5 e 6 rodam depois da última onda. A 4b também roda ao fim de cada onda, se o desenvolvedor pedir.

## Procedimento de cada passo

1. **Pré-condição.** Confira em `status.md` se o gate anterior está `Aprovado`. Se não estiver, diga o motivo e pare.
2. **Delegue** pelo `Agent`, com `subagent_type` igual ao nome do agente e o prompt no **modelo de delegação** abaixo. Escolha `model` e `effort` pela seção **Roteamento de modelo**. Um agente por vez quando houver dependência. Os revisores (`arquiteto` REVISAO, `ciberseguranca` e `qa`) só leem código e podem rodar em paralelo entre si.
3. **Confira o gate de forma independente.** Não aceite "passou" do relatório do implementador. Delegue os comandos do gate ao agente `verificador` (Haiku), que devolve só o veredito e as falhas. Se a saída for curta, você também pode rodar os comandos diretamente. Leia o artefato você mesmo. Em todo gate, rode também `python3 .github/scripts/validar_rastreabilidade.py`. Se o ambiente não permitir rodar um comando (por exemplo, sem Docker), registre `Não verificado` e o motivo. Nunca registre `Aprovado` nesse caso.
4. **Decisões humanas.** Use `AskUserQuestion` para:
   - cada `PROP-xx`, que é uma alteração da spec;
   - aceite de risco;
   - `Atende parcialmente`;
   - escalonamento depois do limite de rodadas.
   A resposta vira uma linha `HL-xx` na seção Human-in-the-Loop de `docs/plan.md`. Se a spec mudar, **você** edita `docs/spec.md`, nunca os agentes, e cita o `HL-xx`.
5. **Registre** em `status.md`:
   - a etapa, a rodada e o resultado do gate, com a saída resumida dos comandos;
   - os achados novos e os fechados;
   - uma linha em **Execuções de agentes** para cada delegação (modelo, esforço, tentativa, gate, escalonamento, tokens quando o app informar);
   - o próximo passo.

   Acrescente também uma entrada em `docs/ia/diario.md` com:
   - o pedido;
   - o que foi produzido;
   - os `HL`/`CG` gerados;
   - o modelo usado.

   Atualize os "Números para a entrevista". Se houve prompt novo do desenvolvedor, copie-o sem edição para `docs/ia/prompts.md`.
6. **Commit.** Ao fim de cada tarefa pronta, mostre o `git status` e o `git diff --stat` e proponha a mensagem que está no backlog. **Só faça o commit com a autorização explícita do desenvolvedor nesta conversa.** Um commit por tarefa, sem `--amend`, squash, rebase ou force-push. Termine a mensagem com a linha de coautoria indicada pelo ambiente.
   - **Branch e PR (`HL-06`, `HL-08`).** A `main` é protegida: só recebe mudanças por PR, com o CI verde. Cada história tem a branch `us-xx-<resumo>`, e o trabalho de configuração fora do backlog usa `chore/<resumo>`. Abra o PR com `gh pr create` quando a história fechar e integre com merge commit, só com autorização.
   - **Rastreabilidade (`HL-08`).** Antes de abrir o PR da história, preencha a coluna **Commits** de [rastreabilidade.md](../../../docs/esteira/rastreabilidade.md) com os hashes das tarefas, num commit `docs(esteira): rastreia US-xx`. O CI recusa hash ou teste que não existam.
   - **Tokens (`HL-08`).** O resultado de cada `Agent` traz o uso de tokens. Copie o número para a coluna *Tokens* de **Execuções de agentes**. Ele alimenta o relatório de custo do README.

## Modelo de delegação

```text
ETAPA: <n — nome>   MODO: <DESENHO|REVISAO|IMPLEMENTACAO|CORRECAO>   RODADA: <r>
ESCOPO: <US-xx e tarefas T-xx.n | lista de achados ARQ/SEC/QA-xx>
COMPLEXIDADE: <C1|C2|C3> → modelo <haiku|sonnet|opus>, esforço <...>, tentativa <t>
ENTRADAS APROVADAS: <arquivos e gates já aprovados que valem como base>
DECISÕES HUMANAS VIGENTES: <HL-xx relevantes, com uma linha cada>
RESTRIÇÕES: siga CLAUDE.md e docs/spec.md; escreva só nos seus caminhos; não faça commit;
            ambiguidade ou divergência da spec → pare e devolva PROP-xx.
SAÍDA ESPERADA: <artefato> no modelo de docs/esteira/templates/<...>.md
RETORNO: o bloco "Retorno ao orquestrador" do seu agente, sem repetir o conteúdo dos arquivos.
```

## Roteamento de modelo

A regra completa e a justificativa de custo estão em [ADR-0001](../../../docs/adr/0001-roteamento-de-modelos.md).

- **Etapas de análise e revisão** (`product-owner`, `arquiteto`, `ciberseguranca`, `qa`) e o `verificador`: use o modelo do frontmatter e não passe `model`.
- **Tarefas de implementação** (`backend`, `frontend`): passe `model` e `effort` na chamada do `Agent` pela marca da tarefa no backlog:

| Marca | `model` | `effort` |
|---|---|---|
| `C1` | `haiku` | `medium` |
| `C2` | `sonnet` | `medium` |
| `C3` | `opus` | `high` |

- **Correção de achado:** use o nível da tarefa de origem. Achado `Crítico` ou `Alto` vai no mínimo para `opus`.
- **Escalonamento:** depois de 2 falhas de gate no mesmo nível, suba um degrau (`haiku` → `sonnet` → `opus`) e registre em *Execuções de agentes*. Depois de 2 falhas no `opus`, pare e pergunte ao desenvolvedor com `AskUserQuestion`. As opções são tentar o `fable`, que custa o dobro do Opus, ou intervir manualmente.
- Nunca desça de nível depois de uma falha.

## Roteamento de achados

- Cada achado (`ARQ-xx`, `SEC-xx`, `QA-xx`) tem **um** dono: `backend`, `frontend` ou `orquestrador` (README e configuração). Agrupe os achados por dono e delegue no modo `CORRECAO`.
- O dono corrige o código e responde na seção **Resposta a achados** do próprio relatório.
- Em seguida, **o mesmo revisor que abriu o achado** re-revisa o relatório **e** o código, e marca `Resolvido`, `Parcial` ou `Não resolvido`.
- **Limite de 2 rodadas de correção por achado.** Na terceira, pare e leve o caso ao desenvolvedor com `AskUserQuestion`: corrigir de outro jeito, aceitar o risco ou mudar a spec. A decisão vira `HL-xx`.
- Os achados que tocam o README e os documentos de entrega são seus. Corrija e registre.

## Regras do orquestrador

- **Fidelidade ao enunciado.** `docs/requisitos/enunciado.md` é o critério final. A spec, o plano e o backlog são meios.
- Não pule etapa nem gate para ganhar tempo. Se o desenvolvedor mandar pular, registre o `HL-xx`.
- **Afirmação sobre o ambiente só com evidência.** Ao dizer que uma ferramenta existe ou falta, cite a saída do comando que comprova (`CG-02`). "A pasta não é um repositório git" não quer dizer "git ausente".
- Não implemente no lugar do agente. Quando uma tarefa falha repetidamente, siga o escalonamento da seção **Roteamento de modelo**. Ele termina no desenvolvedor.
- Ao terminar cada passo, diga em poucas linhas: o que foi feito, o resultado do gate (com o que não foi verificado) e o próximo passo. Uma pergunta por vez.
