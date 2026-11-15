# Uso do MCP `mssql` durante o desenvolvimento

O MCP ([.mcp.json](../../.mcp.json)) liga o Claude ao SQL Server do compose pelo login `claude_mcp`. Esse login só lê: tem `db_datareader`, `SHOWPLAN` e `VIEW DATABASE STATE`. Para usar o MCP:

1. Suba só o banco com `docker compose up -d db db-init`.
2. Abra a sessão do Claude Code na raiz do repositório.

## Histórico

| Data | Situação |
|---|---|
| 2026-11-13 | A primeira conexão falhou porque a senha padrão continha o nome do login, e o SQL Server recusa isso (erro 33064). A senha foi trocada (`CG-05`) |
| 2026-11-13 e 14 | O processo do MCP tinha sido iniciado antes da troca e continuou devolvendo `AUTH_FAILED`. Na Parte 4, os planos foram colhidos com o mesmo login, por um teste de integração |
| 2026-11-14 | Com a sessão reiniciada, o MCP passou a responder. Isso mostrou que o `AUTH_FAILED` vinha do processo antigo, e não da configuração |
| 2026-11-15 | Usos 2 a 4 abaixo, repetidos pelo orquestrador na rodada de correção (`HL-32`). O MCP devolve `AUTH_FAILED` enquanto o `db-init` não termina, porque é ele que cria o login |

## Usos reais

### 1. Conferir a Parte 4 contra a base de demonstração (agente `qa`, 2026-11-14)

O agente `qa` rodou pelo MCP a query original e a corrigida ([log](../esteira/evidencias/qa-mcp-parte4.log)):

- **Original:** 9 linhas, em ordem crescente, com os clientes homônimos somados.
- **Corrigida:** 11 linhas, em ordem decrescente, com cada cliente separado pelo `Id`.
- **Conferência:** os totais da corrigida, somados, batem com os da original.

Os defeitos descritos no README aparecem nos dados reais, e não só nos testes.

### 2. Plano de uma variante simplificada da query original (`explain_sql`)

A variante soma `ValorTotal` em vez de `Quantidade * PrecoUnitario` e usa `JOIN` no lugar dos dois `LEFT JOIN` do enunciado. Os defeitos que o plano mostra (função sobre a coluna, agrupamento por nome, ordem crescente) são os mesmos. A query exata está em [01-query-original.sql](../../database/parte4/01-query-original.sql).

```sql
SELECT c.Nome, SUM(p.ValorTotal) AS Total FROM Clientes c JOIN Pedidos p ON p.ClienteId = c.Id
WHERE YEAR(p.DataPedido) = 2026 GROUP BY c.Nome ORDER BY Total
```

No plano estimado:

- **Acesso a `Pedidos`:** um *Index Seek* em `IX_Pedidos_ClienteId_DataPedido`, com a chave de busca só em `ClienteId`.
- **Filtro de data:** `datepart(year, DataPedido) = (2026)` aparece como `Predicate` residual. Ele é avaliado linha a linha depois do *seek* e não entra na chave de busca. O período, portanto, não restringe a leitura do índice.
- **Agregação:** é um `Stream Aggregate` com `GROUP BY c.Nome`. Clientes homônimos caem no mesmo grupo.
- **Ordenação:** o `Sort` final está em `Ascending="1"` sobre o total. É o ranking invertido.

O plano confirma, no banco do compose, a conclusão da [investigação](../../database/parte4/investigacao.md): o defeito é a função aplicada à coluna, e não a falta de índice.

### 3. Coerência dos totais gravados (`execute_sql`)

```sql
SELECT COUNT(*) AS Pedidos, SUM(CASE WHEN p.ValorTotal <> s.Soma THEN 1 ELSE 0 END) AS Divergentes
FROM Pedidos p
CROSS APPLY (SELECT SUM(i.Quantidade * i.PrecoUnitario) AS Soma FROM ItensPedido i WHERE i.PedidoId = p.Id) s
```

Em 2026-11-15, o resultado foi **61 pedidos e 0 divergentes** (o número de pedidos muda conforme se usa a base), contando os pedidos criados nos cenários exploratórios do QA. Isso sustenta a premissa da query `03` da Parte 4: o `ValorTotal` é gravado na mesma transação que os itens.

### 4. O MCP achou um índice fora dos scripts (`search_objects`)

A listagem dos índices de `Pedidos` trouxe `IDX_Pedidos_DataPedidos`, com chave em `DataPedido`. Esse índice **não existe em nenhum script de `database/`** nem no histórico do git (`git log --all -S`). Ele tem dois problemas: o nome foge do padrão `IX_`, e ele duplica o `IX_Pedidos_DataPedido`.

- As estatísticas dele são de 2026-11-14 às 16:03 UTC, durante a rodada exploratória do QA.
- A origem não foi identificada. A suspeita é uma alteração manual no banco de demonstração.
- Os testes de integração não são afetados, porque o Testcontainers cria um banco novo a partir dos scripts.
- O `db-init` não remove esse índice, pois só cria o que falta. Para recriar o banco sem ele, rode `docker compose down -v` e depois `docker compose up`. Esse é o passo previsto antes de gravar o vídeo.

Esse é o uso mais valioso do MCP neste projeto: comparar o banco real com a fonte de verdade (`database/*.sql`) sem dar ao Claude permissão de escrita.

## O que o MCP não prova

A recusa de escrita do login `claude_mcp` é provada pelo teste de integração `LoginMcp_TentaEscrever_PermissaoNegada`, e não por uma tentativa feita pelo MCP.
