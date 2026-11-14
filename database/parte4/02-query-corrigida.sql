-- =============================================================================
-- parte4/02-query-corrigida.sql — ranking dos clientes que mais compraram em
-- 2026, corrigido e otimizado (CA-33, HL-10). Análise em investigacao.md.
--
-- Fora do db-init: o init.sh só aplica database/NN-*.sql (a raiz, não parte4/).
--
-- O que mudou em relação a 01-query-original.sql:
--   * GROUP BY c.Id, c.Nome e o Id na saída: homônimos ficam em linhas
--     separadas (RN-17, CA-23).
--   * ORDER BY Total DESC, c.Id: maior total primeiro, desempate determinístico
--     pelo menor Id (RN-17, CA-24).
--   * Intervalo semiaberto sobre a coluna, sem função (RN-15, CA-22):
--     >= 2026-01-01 00:00 e < 2027-01-01 00:00. Pega até 31/12 23:59:59.999 e
--     permite seek em IX_Pedidos_DataPedido. A conversão fica nos literais (no
--     plano, um Merge Interval que produz uma única faixa); a coluna fica intacta.
--   * INNER JOIN explícito: só entra quem comprou no ano, que é o que o WHERE da
--     original já fazia na prática.
--   * Começa por Pedidos, que é o lado filtrado.
--
-- Ranking COMPLETO, sem TOP (HL-10, PROP-01): a original não limita. Os 5
-- primeiros coincidem com GET /relatorios/top-clientes de 2026 (CA-33).
-- Índices: nenhum novo. IX_Pedidos_DataPedido (DataPedido DESC, Id DESC)
-- INCLUDE (ClienteId, ValorTotal) cobre Pedidos, e PK_ItensPedido, clusterizada
-- por (PedidoId, ProdutoId), cobre os itens (spec §6.3, arquitetura Q12).
-- =============================================================================
USE JoyaPedidos;
GO
-- parte4:corrigida
SELECT c.Id AS ClienteId, c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM dbo.Pedidos AS p
INNER JOIN dbo.Clientes AS c ON c.Id = p.ClienteId
INNER JOIN dbo.ItensPedido AS i ON i.PedidoId = p.Id
WHERE p.DataPedido >= '2026-01-01T00:00:00.000'
  AND p.DataPedido <  '2027-01-01T00:00:00.000'
GROUP BY c.Id, c.Nome
ORDER BY Total DESC, c.Id;
