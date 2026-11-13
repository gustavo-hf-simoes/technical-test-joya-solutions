# Modelo de épico, história e tarefa

> Usado pelo `product-owner` em `docs/esteira/backlog.md`. Cada tarefa vira **exatamente um commit**.

## EP-01 — <nome do épico>

**Objetivo de negócio.**
**Critério de sucesso.**
**Linhas do enunciado cobertas.** ENU-Lxxx a ENU-Lyyy.

---

### US-01 — <título>

| Campo | Valor |
|---|---|
| História | Como **\<papel\>**, quero **\<capacidade\>**, para **\<benefício\>**. |
| Prioridade | P1 (obrigatório do enunciado) / P2 (qualidade exigida) / P3 (diferencial) |
| Dono | backend / frontend / orquestrador |
| Requisitos | RF-xx, RNF-xx, ENT-xx |
| Regras e critérios | RN-xx, CA-xx |
| Depende de | US-xx |
| Linhas do enunciado | ENU-Lxxx |

**Critérios de aceite**

```gherkin
Funcionalidade: <nome>

  Cenário: <CA-xx — descrição>
    Dado <contexto>
    Quando <ação>
    Então <resultado observável>
```

**Tarefas**

| Tarefa | [P] | Complexidade | Descrição | Arquivos | Teste que prova | Verificação | Commit |
|---|---|---|---|---|---|---|---|
| T-01.1 | | C2 | ... | `database/01-schema.sql` | `Schema_TabelasCriadas_...` (integração) | `dotnet test ... --filter ...` | `feat(db): cria schema de clientes, produtos e pedidos` |
| T-01.2 | [P] | C1 | ... | ... | ... | ... | ... |

> **Complexidade** (critérios em `docs/adr/0001-roteamento-de-modelos.md`): `C1` é mecânica e tem spec completa (Haiku). `C2` segue um padrão existente (Sonnet). `C3` envolve concorrência, desempenho, segurança ou decisão nova (Opus).

**Definição de pronto**

- [ ] Todos os critérios de aceite têm teste automatizado passando.
- [ ] O build está sem warnings e o lint está limpo.
- [ ] O relatório da etapa foi atualizado.
- [ ] O commit foi feito com a mensagem prevista, depois da autorização do desenvolvedor.
