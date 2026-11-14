# ADR-0001 — Roteamento de modelos e esforço por complexidade da tarefa

- **Status:** Aceito (decisão humana HL-05, ver `docs/plan.md`)
- **Data:** 2026-11-13
- **Contexto da esteira:** [docs/esteira/README.md](../esteira/README.md)

## Contexto

A esteira delega cada etapa a um subagente. Sem configuração, todo subagente herda o modelo da sessão principal (Opus 5.5). Com isso, gerar um `sealed record` custaria o mesmo que desenhar a baixa atômica de estoque. Os tokens dos subagentes contam no mesmo limite de uso da sessão.

Preços de tabela da API, por milhão de tokens (referência oficial da Anthropic, cache de 2026-11-03):

| Modelo | Entrada | Saída | Custo relativo ao Opus 5.5 |
|---|---|---|---|
| Claude Fable 5.2 | $8,00 | $40,00 | 2× |
| Claude Opus 5.5 | $4,00 | $20,00 | 1× |
| Claude Sonnet 5.5 | $2,00 | $10,00 | 0,5× |
| Claude Haiku 5.5 | $0,10 | $0,50 | 0,025× |

O Claude Code resolve o modelo de um subagente nesta ordem:
1. o parâmetro `model` da chamada;
2. o `model` do frontmatter do agente;
3. a variável `CLAUDE_CODE_SUBAGENT_MODEL`;
4. o modelo da sessão.

O `effort` segue a mesma lógica: a chamada prevalece sobre o frontmatter. Um subagente em outro modelo **não herda** o esforço da sessão. Por isso o esforço é declarado explicitamente.

## Decisão

### 1. Modelo padrão por agente (frontmatter)

| Agente | Modelo | Esforço | Justificativa |
|---|---|---|---|
| Orquestrador (sessão principal) | Opus 5.5 | high | Gates, roteamento e decisões. Escolhido pelo desenvolvedor na sessão |
| `product-owner` | `opus` | medium | Um requisito perdido aqui se propaga para todas as etapas. O esforço caiu de `high` para `medium` pela `HL-09`: no `high`, o PO raciocinou 13 min sem gravar nenhum artefato |
| `arquiteto` | `opus` | high | Desenho e revisão de conformidade exigem julgamento |
| `ciberseguranca` | `opus` | high | Falso negativo em segurança tem o custo mais alto |
| `backend` | `sonnet` | medium | Padrão; o orquestrador sobe ou desce por tarefa (item 2) |
| `frontend` | `sonnet` | medium | Idem |
| `qa` | `sonnet` | high | Volume grande de verificação com evidência. O veredito final é conferido pelo orquestrador (Opus) |
| `verificador` | `haiku` | low | Roda comandos e resume logs. Mantém saídas longas de build e teste fora do contexto do Opus |

**Fable 5.2 não é padrão de nenhum agente.** Ele custa o dobro do Opus 5.5, e nenhuma etapa deste escopo (3 endpoints, 2 telas) mostrou precisar dele. Fica como último degrau de escalonamento.

### 2. Complexidade da tarefa → modelo (parâmetro da chamada)

O `product-owner` marca cada tarefa do backlog com `C1`, `C2` ou `C3`. Na hora de delegar, o orquestrador passa `model` e `effort` conforme a marca:

| Nível | Critério | Exemplos neste projeto | Modelo | Esforço |
|---|---|---|---|---|
| `C1` — mecânica | A spec define o resultado por completo (nomes, tipos, campos). Não há decisão, concorrência nem consulta nova. O teste já está descrito | DTOs `sealed record`, script de carga (`02-seed.sql`), `Dockerfile` seguindo o modelo, template Tailwind de uma tela já especificada | `haiku` | medium |
| `C2` — padrão | Segue um padrão já estabelecido no repositório (`/new-endpoint`), com regras `RN-xx` explícitas | Validators, `PedidoService` (exceto a baixa), controllers, consultas de listagem, telas com formulário tipado, CI | `sonnet` | medium |
| `C3` — crítica | Concorrência, desempenho, segurança ou decisão sem padrão anterior | `IEstoqueRepository.TentarBaixarAsync` + teste CA-10, Parte 4 (debug da query e índices), mapeamento EF sobre os scripts, idempotência, deploy | `opus` | high |

Na dúvida entre dois níveis, vale o mais alto.

### 3. Escalonamento

- Se o gate de uma tarefa falha **duas vezes** no mesmo nível, ela sobe um degrau: `haiku` → `sonnet` → `opus`. Depois de duas falhas no `opus`, o orquestrador pergunta ao desenvolvedor se tenta o `fable` ou se o caso vai para intervenção manual. O ciclo de achados `ARQ`/`SEC`/`QA` é separado e mantém as 2 rodadas próprias.
- Cada escalonamento é registrado em `docs/esteira/status.md`, na seção *Execuções de agentes*. Se uma tarefa `C1` escalar com frequência, isso indica que o critério de `C1` está frouxo ou a spec está incompleta. A correção vai na spec ou no critério, não só no modelo.
- Uma tarefa nunca desce de nível depois de uma falha.

### 4. Medição

Para cada delegação, o orquestrador registra em *Execuções de agentes*:
- tarefa;
- agente;
- modelo e esforço;
- tentativa;
- resultado do gate;
- se escalou;
- tokens, quando o app informar (`/tasks` ou o uso da sessão).

A seção "Como usei o Claude" do README traz o resumo medido: distribuição de tarefas por nível, taxa de escalonamento e comparação com o custo estimado de rodar tudo no Opus.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Tudo no modelo da sessão (`inherit`) | É o mais simples, mas paga preço de Opus por trabalho mecânico. Não demonstra controle de custo |
| Tudo no Fable 5.2 | O dobro do custo do Opus, sem ganho mensurável neste escopo |
| Sonnet como piso para quem implementa | Mais seguro, mas abre mão de 20× de economia nas tarefas `C1`. A combinação de gate automático, zero warnings e escalonamento contém o risco do Haiku |
| Forçar um modelo global (`CLAUDE_CODE_SUBAGENT_MODEL_FORCE`) | Elimina o roteamento por tarefa |

## Consequências

- **Positivas:**
  - O custo acompanha a dificuldade.
  - Logs longos ficam fora do contexto mais caro.
  - O critério é auditável: a marca `C1`/`C2`/`C3` aparece no backlog, a escolha do modelo no `status.md` e o frontmatter no repositório.
- **Negativas e riscos:**
  - O cache de prompt é separado por modelo. Uma tarefa que escala paga as duas tentativas, então a métrica certa é **custo por tarefa concluída**, não custo por chamada.
  - O Haiku pode produzir código que compila mas é fraco. Isso é mitigado porque `C1` exige spec completa e teste descrito, e o revisor `arquiteto` confere o código.
  - Preços de tabela valem para a API. Numa assinatura do Claude Code, o efeito aparece como consumo do limite de uso, e não como fatura. A proporção entre os modelos é a mesma.
