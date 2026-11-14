-- =============================================================================
-- parte4/01-query-original.sql — a query da Parte 4 EXATAMENTE como veio no
-- enunciado (ENU-L042 a ENU-L048). Não corrija este arquivo: ele existe para
-- reproduzir os defeitos. A correção está em 02-query-corrigida.sql e a análise,
-- com planos e medições, em investigacao.md.
--
-- Fora do db-init: o init.sh só aplica database/NN-*.sql (a raiz, não parte4/).
-- É a única exceção do checklist DB-03 (função sobre DataPedido).
--
-- Defeitos, cada um provado por teste em
-- backend/tests/Joya.Pedidos.IntegrationTests/Parte4/QueryRankingTests.cs:
--   1. GROUP BY c.Nome soma clientes homônimos numa linha só, e a saída nem
--      traz o Id para distingui-los (RN-17).
--      → QueryOriginal_ClientesHomonimos_SomaIndevidamente
--   2. ORDER BY Total é crescente: o "ranking" começa por quem MENOS comprou,
--      e não há desempate (empates saem em ordem arbitrária).
--      → QueryOriginal_Ordenacao_RetornaMenorTotalPrimeiro
--   3. A função YEAR() sobre a coluna DataPedido não é sargável: o otimizador não
--      pode delimitar um seek por data, lê todos os pedidos e calcula a função
--      linha a linha (RN-15). Com volume, é um scan de Pedidos inteira
--      (200 000 linhas para usar 44 867, ver investigacao.md).
--      → QueryOriginal_Plano_LeTodosOsPedidosComFuncaoSobreDataPedido
--      → Investigacao_CargaDeVolume_MedePlanosELeituras
--   4. O WHERE sobre a tabela da direita anula o LEFT JOIN de Pedidos: a linha
--      de um cliente sem pedido tem DataPedido NULL e cai no filtro. É um INNER
--      JOIN disfarçado, e a intenção do autor fica ambígua.
-- =============================================================================
USE JoyaPedidos;
GO
-- parte4:original
SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id
LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE YEAR(p.DataPedido) = 2026
GROUP BY c.Nome
ORDER BY Total
