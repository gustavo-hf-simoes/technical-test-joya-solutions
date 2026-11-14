# ADR-0004 — Unidade de trabalho sem *retry* automático; idempotência como proteção de reenvio

- **Status:** Proposto (etapa 2, arquitetura). Substitui a parte "`CreateExecutionStrategy()` com *retry* habilitado" da tarefa 3.2 e o risco correspondente no [plan.md](../plan.md).
- **Data:** 2026-11-13
- **Referências:** [plan.md](../plan.md) `D4`, `D5`, `D7`, `HL-14`; spec §4.3, RN-10, RN-14; [02-arquitetura.md](../esteira/02-arquitetura.md) §4

## Contexto

O `plan.md` (tarefa 3.2 e a tabela de riscos) previa `EnableRetryOnFailure` no provedor do SQL Server, com a transação manual envolvida por `CreateExecutionStrategy().ExecuteAsync(...)`. O `D5`, no mesmo plano, diz "sem retry". As duas coisas não convivem.

O *retry* do EF repete a operação inteira quando acontece um erro transitório. No `POST /pedidos`, o erro transitório mais perigoso é a perda da conexão **durante o `COMMIT`**: o banco pode ter gravado, mas a API não recebe a confirmação. Uma nova tentativa automática, sem chave de idempotência, cria um **segundo pedido** e debita o estoque **duas vezes**. O EF documenta esse caso e só o resolve com verificação manual de estado.

Também falta definir como a transação, que pertence à `Infrastructure`, é controlada pelo serviço, que fica no `Core` e não conhece o EF.

## Decisão

1. **Sem `EnableRetryOnFailure` e sem `CreateExecutionStrategy`.** Um erro transitório vira exceção → rollback (se a conexão ainda existir) → `500 ERRO_INTERNO`. Quem decide se tenta de novo é o cliente da API, com a mesma `Idempotency-Key` (RN-14). Proposta para a tela de criação (`PROP-13` do relatório da etapa 2, porque a spec ainda não pede): gerar a chave com `crypto.randomUUID()` quando o formulário abre e reutilizá-la em todo reenvio, para que o reenvio do usuário nunca duplique o pedido. `crypto.randomUUID()` exige contexto seguro, o que vale para `localhost` e para o deploy em HTTPS.
2. **Porta `IUnitOfWork` no `Core`:**

   ```csharp
   Task<Result<T>> ExecutarEmTransacaoAsync<T>(
       Func<CancellationToken, Task<Result<T>>> operacao, CancellationToken ct);
   ```

   A implementação abre `Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct)` e executa a operação. Com `Success`, faz commit. Com `Failure`, faz rollback e `ChangeTracker.Clear()`, para que nada da tentativa desfeita fique rastreado. Com exceção, faz rollback (pelo `await using` da transação) e deixa a exceção subir. O `Result` é o sinal de rollback, o que segue o `D4`: falha de regra não é exceção.
3. **A violação do índice único vira `Result`.** `PedidoRepository.AdicionarAsync` captura `DbUpdateException` cujo `InnerException` é `SqlException` com `Number` 2601 ou 2627 **e** cuja mensagem cita `UX_Pedidos_ChaveIdempotencia`. Nesse caso, devolve `Failure(PedidoErrors.ChaveIdempotenciaDuplicada)`. Qualquer outra violação continua sendo exceção. Esse é o único `catch` de exceção de banco na `Infrastructure`.
4. **Reapresentação depois do rollback (`HL-14`).** Depois de um rollback por estoque ou por chave duplicada, se a requisição tem chave, o serviço busca a chave de novo, **fora** da transação desfeita. Se achar, devolve o pedido original com `Reapresentado = true` (`200` + `Idempotent-Replayed: true`). Isso cobre as duas formas da corrida:
   - a segunda requisição consegue debitar e falha no `INSERT` por causa do índice único → rollback → reapresentação;
   - a primeira consome o estoque e a segunda recebe 0 linhas no `UPDATE` → rollback → a chave já existe → reapresentação, e não um `409` enganoso.
5. **Deadlock (1205) e timeout** viram `500`, sem tratamento especial. A ordem por `ProdutoId` (spec §4.3) elimina o ciclo entre pedidos. O que sobrar é raro e coberto pelo reenvio idempotente do cliente.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| `EnableRetryOnFailure` + `CreateExecutionStrategy` (plano original) | Duplica o pedido quando a confirmação do commit se perde. Contradiz o `D5` |
| *Retry* só quando há `Idempotency-Key` | Dois caminhos de execução para o mesmo endpoint, e o ganho é pequeno: o cliente já reenvia com a chave |
| `TransactionScope` | Menos explícito com `async` (exige `TransactionScopeAsyncFlowOption.Enabled`). Não traz nada que `BeginTransactionAsync` não traga |
| Expor `IDbContextTransaction` ao `Core` | Faz o `Core` depender do EF, o que viola a regra de dependência (§3) |
| `SERIALIZABLE` ou `UPDLOCK` na leitura do estoque | Desnecessário: o `UPDATE` condicional já é a verificação atômica (spec §4.3). Só acrescentaria bloqueio |

## Consequências

- **Positivas:** nenhum caminho automático cria pedido duplicado. A transação fica visível no `Core` só como "execute isto de forma atômica", testável com um `IUnitOfWork` falso que executa a operação direto. O teste `CriarAsync_EstoqueInsuficiente_RetornaConflitoSemGravar` verifica que `AdicionarAsync` não é chamado.
- **Negativas:**
  - Uma falha transitória de rede vira `500` para o usuário, que precisa clicar de novo. A tela de erro do formulário (CA-32) oferece o reenvio com a mesma chave.
  - O reconhecimento da violação depende do nome do índice na mensagem do SQL Server. Um teste de integração (CA-37) quebra se o nome mudar.
- **Ações:** a tarefa T-06.4 implementa o `UnitOfWork` **sem** *retry*. A linha de risco "Transação manual lança exceção com `EnableRetryOnFailure`" do `plan.md` deixa de valer; o orquestrador decide se a marca como superada.
