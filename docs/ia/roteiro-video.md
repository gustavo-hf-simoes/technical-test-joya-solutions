# Roteiro do vídeo de entrega (até 10 min)

> Entrega obrigatória (`ENU-L082`): mostrar o fluxo de trabalho com o Claude Code e explicar o código. Cada bloco aponta o arquivo que fica na tela. Os tempos somam 9 min 30 s, o que deixa 30 s de folga.

| # | Tempo | Mensagem | Na tela |
|---|---|---|---|
| 1 | 0:00–0:40 | O problema, a stack e a ideia central: **a spec é a fonte de verdade, e a IA trabalha em esteira, não numa conversa só** | README (topo), [enunciado.md](../requisitos/enunciado.md) |
| 2 | 0:40–1:50 | SDD: da spec ao plano. A IA propôs, eu discordei, e cada discordância está registrada com o motivo | [spec.md](../spec.md) (RN e CA), [plan.md](../plan.md), seção Human-in-the-Loop (HL-01 a HL-08). Mostrar o CI falhando quando uma linha do enunciado fica sem teste ([rastreabilidade.md](../esteira/rastreabilidade.md)) |
| 3 | 1:50–3:10 | A esteira: 7 agentes com papéis, gates e achados que voltam ao dono. Os limites valem por hook, não só por prompt | [esteira/README.md](../esteira/README.md) (diagrama), um agente em `.claude/agents/`, [guard-paths.sh](../../.claude/hooks/guard-paths.sh) bloqueando uma escrita |
| 4 | 3:10–4:00 | Custo de IA: o modelo segue a complexidade (Haiku 1/40 do Opus) e o escalonamento é medido | [ADR-0001](../adr/0001-roteamento-de-modelos.md), tabela *Execuções de agentes* em [status.md](../esteira/status.md) |
| 5 | 4:00–5:00 | **Demo ao vivo:** `/esteira US-xx` delega uma tarefa, o hook `post-edit` compila e testa, o `verificador` confere o gate | Terminal do Claude Code |
| 6 | 5:00–6:30 | Código: baixa atômica de estoque (RN-xx, CA-10, concorrência), `Result<T>`, um teste de integração com Testcontainers | `IEstoqueRepository.TentarBaixarAsync`, teste CA-10 |
| 7 | 6:30–7:40 | Parte 4: o que estava errado na query e como investiguei com o MCP (plano de execução, índices, antes e depois) | `database/parte4/`, consulta via MCP `mssql` |
| 8 | 7:40–8:40 | Onde a IA errou e como peguei: CG-01, CG-02 e os achados ARQ/SEC/QA | [diario.md](diario.md), [plan.md](../plan.md) (Correções em código gerado) |
| 9 | 8:40–9:30 | Entrega: `docker compose up`, CI e CodeQL verdes no PR (a `main` é protegida), testes de arquitetura, Playwright, custo medido e o link do deploy | GitHub Actions, aba *Security*, seção de custo do README, aplicação online |

## Frases-chave

- "Eu não aceitei o 'passou' do agente que implementou. O gate é conferido por outro agente, e sem evidência fica `Não verificado`."
- "Uma tarefa mecânica roda no Haiku, a concorrência de estoque roda no Opus. Se o Haiku falha duas vezes, a tarefa sobe de nível, e isso fica registrado."
- "A IA errou ao dizer que o git não estava instalado. A lição virou regra: afirmação sobre o ambiente precisa da saída do comando."
- "As regras do CLAUDE.md não ficam só no prompt: o Core referenciar o EF quebra um teste de arquitetura, e uma linha do enunciado sem teste quebra o CI."
- "Antes de adotar uma biblioteca, eu conferi o NuGet. O NetArchTest, que a IA sugeriu, não lança versão desde 2021, então usei o ArchUnitNET."
- "O histórico não foi reescrito. O bootstrap tem commits lógicos com datas reais, e depois disso há um PR por história."

## Antes de gravar

- [ ] Diário e números atualizados até a última etapa.
- [ ] CI verde na `main` e link do deploy funcionando.
- [ ] Banco no ar (`docker compose up -d db db-init`) para a demonstração do MCP.
- [ ] Fonte do terminal grande, notificações desligadas, `.env` fora da tela.
