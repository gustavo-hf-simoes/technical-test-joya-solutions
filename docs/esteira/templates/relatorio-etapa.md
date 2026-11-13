# Relatório da etapa <n> — <nome> (`<agente>`)

> Modelo de relatório. Mantenha as rodadas anteriores: acrescente rodadas novas e não reescreva as antigas.

## Rodada <r> — <data> — <US-xx | CORRECAO>

### Tarefas

| Tarefa | Status | Mensagem de commit prevista | Arquivos principais |
|---|---|---|---|
| T-xx.1 | Pronta / Bloqueada | `feat(core): ...` | `backend/src/...` |

### Testes criados ou alterados

| Teste | Tipo (unit / integração / E2E) | Cobre |
|---|---|---|
| `CriarAsync_EstoqueInsuficiente_RetornaConflito` | unit | CA-08, RN-07 |

### Verificações executadas

| Comando | Resultado | Resumo da saída |
|---|---|---|
| `dotnet build backend/Joya.Pedidos.sln` | Passou / Falhou / Não executado (motivo) | `0 Warning(s) 0 Error(s)` |

### Evidência no banco (MCP `mssql`), quando houver

```sql
-- consulta executada
```

Resultado resumido:

### Desvios em relação à spec

| PROP | Texto atual da spec | Proposta | Motivo |
|---|---|---|---|
| — | — | — | — |

### Bloqueios e perguntas

- —

## Resposta a achados

| Achado | Rodada | O que foi feito | Onde (arquivo:linha) | Teste que prova | Status declarado |
|---|---|---|---|---|---|
| SEC-01 | 1 | ... | ... | ... | Corrigido / Contestado (motivo) |
