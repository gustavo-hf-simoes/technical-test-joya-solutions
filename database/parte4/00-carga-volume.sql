-- =============================================================================
-- parte4/00-carga-volume.sql — volume para a investigação da Parte 4.
--
-- Com as 34 linhas da carga de exemplo qualquer plano é instantâneo, e a
-- diferença entre ler tudo (scan) e ler só 2026 (seek) não aparece. Este script
-- gera, por padrão, 5 000 clientes, 200 produtos, 200 000 pedidos e cerca de
-- 600 000 itens, para que planos e leituras lógicas possam ser comparados.
--
-- FORA do db-init: o init.sh só aplica database/NN-*.sql (a raiz, não parte4/).
-- Rode à mão, como sa, num banco de desenvolvimento:
--   sqlcmd -S localhost -U sa -C -I -b -i database/parte4/00-carga-volume.sql
-- O teste Investigacao_CargaDeVolume_MedePlanosELeituras o executa no SQL Server
-- do Testcontainers. Sem paralelismo nem aleatoriedade: os valores saem de
-- aritmética sobre o número da linha, então duas execuções geram os mesmos dados.
--
-- Características do volume:
--   * 300 nomes distintos para 5 000 clientes: cada nome tem uns 17 homônimos;
--   * 1 cliente em 50 inativo (RN-17: inativo continua no ranking);
--   * pedidos de 2023-01-01 a 2026-11-13, uns 22 % deles em 2026; o Id cresce
--     com a data, como numa base real;
--   * distribuição desigual de pedidos por cliente (poucos compram muito);
--   * 1 a 5 itens por pedido, sem produto repetido (RN-03), PrecoUnitario igual
--     ao preço do produto e ValorTotal igual à soma dos itens (RN-09).
--
-- Idempotente: clientes e produtos entram pela chave natural (Email, Sku); os
-- pedidos só entram se nenhum cliente de volume tiver pedido; os itens, só nos
-- pedidos de volume que ainda não têm item. Cada passo é uma instrução atômica,
-- sem transação longa, para o log poder ser truncado entre os passos.
-- =============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

USE JoyaPedidos;
GO

DECLARE @Clientes INT = 5000;
DECLARE @Produtos INT = 200;            -- precisa ser >= 29 para os 5 itens de um pedido não repetirem produto
DECLARE @Pedidos  INT = 200000;
DECLARE @Inicio   DATETIME2(3) = '2023-01-01T00:00:00.000';
DECLARE @Dias     INT = DATEDIFF(DAY, '2023-01-01', '2026-11-14');   -- 1413 dias: até 2026-11-13

-- ---- Números de 1 a N (N = maior das três quantidades, até 1 000 000) -------
CREATE TABLE #Numeros (N INT NOT NULL PRIMARY KEY);
WITH Digito (D) AS (SELECT 0 FROM (VALUES (0), (0), (0), (0), (0), (0), (0), (0), (0), (0)) AS v (D))
INSERT INTO #Numeros (N)
SELECT TOP (SELECT MAX(q) FROM (VALUES (@Clientes), (@Produtos), (@Pedidos)) AS t (q))
       ROW_NUMBER() OVER (ORDER BY (SELECT NULL))
FROM Digito a CROSS JOIN Digito b CROSS JOIN Digito c CROSS JOIN Digito d CROSS JOIN Digito e CROSS JOIN Digito f;

-- ---- Clientes: 20 nomes × 15 sobrenomes = 300 nomes distintos ---------------
INSERT INTO dbo.Clientes (Nome, Email, Ativo)
SELECT CONCAT(
           CHOOSE(n.N % 20 + 1,
               N'Ana', N'Bruno', N'Carla', N'Daniel', N'Eduarda', N'Felipe', N'Gabriela', N'Henrique', N'Isabela', N'João',
               N'Karina', N'Lucas', N'Mariana', N'Nicolas', N'Olívia', N'Pedro', N'Renata', N'Samuel', N'Tatiana', N'Vitor'),
           N' ',
           CHOOSE(n.N / 20 % 15 + 1,
               N'Silva', N'Santos', N'Oliveira', N'Souza', N'Rodrigues', N'Ferreira', N'Alves', N'Pereira',
               N'Lima', N'Gomes', N'Costa', N'Ribeiro', N'Martins', N'Carvalho', N'Almeida')),
       CONCAT('volume.cliente', n.N, '@exemplo.com'),
       CASE WHEN n.N % 50 = 0 THEN 0 ELSE 1 END
FROM #Numeros n
WHERE n.N <= @Clientes
  AND NOT EXISTS (SELECT 1 FROM dbo.Clientes c WHERE c.Email = CONCAT('volume.cliente', n.N, '@exemplo.com'));

CREATE TABLE #Clientes (Seq INT NOT NULL PRIMARY KEY, Id INT NOT NULL UNIQUE);
INSERT INTO #Clientes (Seq, Id)
SELECT ROW_NUMBER() OVER (ORDER BY c.Id) - 1, c.Id
FROM dbo.Clientes c
WHERE c.Email LIKE 'volume.cliente%@exemplo.com';

-- ---- Produtos: preço de 1,00 a 500,99 ---------------------------------------
INSERT INTO dbo.Produtos (Sku, Nome, Preco, Estoque, Ativo)
SELECT CONCAT('VOL-', RIGHT(CONCAT('0000', n.N), 4)),
       CONCAT(N'Produto de volume ', n.N),
       CAST((100 + n.N * 7919 % 50000) / 100.0 AS DECIMAL(18,2)),
       1000,
       1
FROM #Numeros n
WHERE n.N <= @Produtos
  AND NOT EXISTS (SELECT 1 FROM dbo.Produtos p WHERE p.Sku = CONCAT('VOL-', RIGHT(CONCAT('0000', n.N), 4)));

CREATE TABLE #Produtos (Seq INT NOT NULL PRIMARY KEY, Id INT NOT NULL, Preco DECIMAL(18,2) NOT NULL);
INSERT INTO #Produtos (Seq, Id, Preco)
SELECT ROW_NUMBER() OVER (ORDER BY p.Id) - 1, p.Id, p.Preco
FROM dbo.Produtos p
WHERE p.Sku LIKE 'VOL-%';

DECLARE @QtdProdutos INT = (SELECT COUNT(*) FROM #Produtos);

-- ---- Pedidos: inseridos em ordem de data, para o Id crescer com a data ------
-- O cliente sai de u² × @Clientes, com u pseudoaleatório em [0, 1): os primeiros
-- clientes concentram mais pedidos, como numa base real.
IF NOT EXISTS (SELECT 1 FROM dbo.Pedidos p WHERE p.ClienteId IN (SELECT Id FROM #Clientes))
BEGIN
    INSERT INTO dbo.Pedidos (ClienteId, DataPedido, ValorTotal)
    SELECT c.Id, x.DataPedido, 0
    FROM #Numeros n
    CROSS APPLY (
        SELECT CAST(SQUARE((n.N * 7919 % 10007) / 10007.0) * (SELECT COUNT(*) FROM #Clientes) AS INT) AS Seq,
               DATEADD(SECOND, CAST(CAST(n.N AS BIGINT) * 7907 % 86400 AS INT),
                   DATEADD(DAY, CAST(CAST(n.N AS BIGINT) * 104729 % @Dias AS INT), @Inicio)) AS DataPedido
    ) AS x
    JOIN #Clientes c ON c.Seq = x.Seq
    WHERE n.N <= @Pedidos
    ORDER BY x.DataPedido, n.N;
END;

-- ---- Itens: 1 a 5 por pedido (média 3), produtos distintos dentro do pedido --
INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario)
SELECT p.Id, pr.Id, (p.Id + k.K) % 4 + 1, pr.Preco
FROM dbo.Pedidos p
JOIN #Clientes c ON c.Id = p.ClienteId
JOIN (VALUES (0), (1), (2), (3), (4)) AS k (K) ON k.K <= p.Id % 5
JOIN #Produtos pr ON pr.Seq = (p.Id * 31 + k.K * 7) % @QtdProdutos
WHERE NOT EXISTS (SELECT 1 FROM dbo.ItensPedido i WHERE i.PedidoId = p.Id);

-- ---- ValorTotal = soma dos itens (RN-09) -------------------------------------
UPDATE p
SET ValorTotal = t.Total
FROM dbo.Pedidos p
JOIN #Clientes c ON c.Id = p.ClienteId
JOIN (
    SELECT i.PedidoId, SUM(i.Quantidade * i.PrecoUnitario) AS Total
    FROM dbo.ItensPedido i
    GROUP BY i.PedidoId
) AS t ON t.PedidoId = p.Id
WHERE p.ValorTotal <> t.Total;

DROP TABLE #Produtos, #Clientes, #Numeros;

-- Estatísticas completas: os planos medidos não dependem de amostragem.
UPDATE STATISTICS dbo.Clientes WITH FULLSCAN;
UPDATE STATISTICS dbo.Produtos WITH FULLSCAN;
UPDATE STATISTICS dbo.Pedidos WITH FULLSCAN;
UPDATE STATISTICS dbo.ItensPedido WITH FULLSCAN;
GO
