-- =============================================================================
-- parte4/03-variante-valor-total.sql — mesmo ranking de 02-query-corrigida.sql,
-- calculado sobre Pedidos.ValorTotal em vez de somar ItensPedido.
--
-- Serve de comparação na investigação (investigacao.md): é a forma que a API
-- usa em GET /relatorios/top-clientes (RelatorioConsultas, arquitetura Q5).
-- Dá o mesmo resultado porque ValorTotal é a soma dos itens (RN-09) e o pedido
-- é imutável (RN-12, spec §6.4). O teste
-- Investigacao_CargaDeVolume_MedePlanosELeituras compara as duas linha a linha.
--
-- Agrega por ClienteId antes do join: o nome só é buscado uma vez por cliente.
-- Fora do db-init: o init.sh só aplica database/NN-*.sql (a raiz, não parte4/).
-- =============================================================================
USE JoyaPedidos;
GO
-- parte4:variante-valor-total
SELECT c.Id AS ClienteId, c.Nome, t.Total
FROM (
    SELECT p.ClienteId, SUM(p.ValorTotal) AS Total
    FROM dbo.Pedidos AS p
    WHERE p.DataPedido >= '2026-01-01T00:00:00.000'
      AND p.DataPedido <  '2027-01-01T00:00:00.000'
    GROUP BY p.ClienteId
) AS t
INNER JOIN dbo.Clientes AS c ON c.Id = t.ClienteId
ORDER BY t.Total DESC, c.Id;
