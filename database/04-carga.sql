-- =============================================================================
-- 04-carga.sql — carga de exemplo (spec §6; CA-33 e CA-35 dependem dela).
--
-- Executado por database/init.sh depois do schema, dos índices e dos logins.
-- O fixture dos testes de integração NÃO a aplica (ADR-0003, item 2): só os
-- testes Carga_* o fazem, pelo mesmo executor.
--
-- O que a carga cobre de propósito:
--   * homônimos: dois "Maria Silva" e dois "João Souza", com e-mails diferentes e
--     pedidos nos dois, para o ranking separar por ClienteId (CA-33);
--   * inativos: 2 clientes e 3 produtos com Ativo = 0 (CA-35, RN-06, RN-07);
--   * produto esgotado (Estoque = 0) e produto com saldo baixo (RN-08);
--   * pedidos de 2025 e de 2026, inclusive nas fronteiras do período
--     (31/12/2025 23:59:59.999, 01/01/2026 00:00:00, 28/02 e 01/03 de 2026);
--   * clientes sem compra em 2026 (só 2025) e sem nenhum pedido.
--
-- Idempotente:
--   * Clientes e Produtos entram pela chave natural (Email, Sku): só o que falta.
--     Rodar de novo nunca duplica nem sobrescreve estoque ou preço.
--   * Os pedidos de exemplo entram uma única vez, e só se Pedidos estiver vazia.
--     Com pedidos já gravados (pela API ou por uma execução anterior), não toca neles.
--
-- Valores: PrecoUnitario copia o Preco do produto e ValorTotal é a soma dos itens
-- (RN-09), como a API faria. A carga é histórica: não debita o estoque.
-- =============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

USE JoyaPedidos;
GO

-- ---- Clientes ---------------------------------------------------------------
INSERT INTO dbo.Clientes (Nome, Email, Ativo)
SELECT v.Nome, v.Email, v.Ativo
FROM (VALUES
    (N'Maria Silva',     'maria.silva@exemplo.com',     CAST(1 AS BIT)),
    (N'Maria Silva',     'maria.silva2@exemplo.com',    CAST(1 AS BIT)),   -- homônimo
    (N'João Souza',      'joao.souza@exemplo.com',      CAST(1 AS BIT)),
    (N'João Souza',      'joao.souza2@exemplo.com',     CAST(1 AS BIT)),   -- homônimo
    (N'Ana Pereira',     'ana.pereira@exemplo.com',     CAST(1 AS BIT)),
    (N'Carlos Lima',     'carlos.lima@exemplo.com',     CAST(1 AS BIT)),
    (N'Fernanda Costa',  'fernanda.costa@exemplo.com',  CAST(1 AS BIT)),
    (N'Paulo Almeida',   'paulo.almeida@exemplo.com',   CAST(1 AS BIT)),
    (N'Beatriz Rocha',   'beatriz.rocha@exemplo.com',   CAST(1 AS BIT)),   -- sem pedidos
    (N'Rafael Mendes',   'rafael.mendes@exemplo.com',   CAST(1 AS BIT)),   -- só 2025
    (N'Luciana Barros',  'luciana.barros@exemplo.com',  CAST(0 AS BIT)),   -- inativa
    (N'Marcos Teixeira', 'marcos.teixeira@exemplo.com', CAST(0 AS BIT))    -- inativo
) AS v (Nome, Email, Ativo)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Clientes c WHERE c.Email = v.Email);
GO

-- ---- Produtos ---------------------------------------------------------------
INSERT INTO dbo.Produtos (Sku, Nome, Preco, Estoque, Ativo)
SELECT v.Sku, v.Nome, v.Preco, v.Estoque, v.Ativo
FROM (VALUES
    ('PRD-001', N'Caneta esferográfica azul',   CAST(  2.50 AS DECIMAL(18,2)), 500, CAST(1 AS BIT)),
    ('PRD-002', N'Caderno universitário',       CAST( 24.90 AS DECIMAL(18,2)), 120, CAST(1 AS BIT)),
    ('PRD-003', N'Mochila escolar',             CAST(149.90 AS DECIMAL(18,2)),  40, CAST(1 AS BIT)),
    ('PRD-004', N'Lápis preto HB',              CAST(  1.80 AS DECIMAL(18,2)), 800, CAST(1 AS BIT)),
    ('PRD-005', N'Borracha branca',             CAST(  1.20 AS DECIMAL(18,2)), 600, CAST(1 AS BIT)),
    ('PRD-006', N'Régua de 30 cm',              CAST(  4.75 AS DECIMAL(18,2)), 300, CAST(1 AS BIT)),
    ('PRD-007', N'Calculadora científica',      CAST( 89.00 AS DECIMAL(18,2)),  25, CAST(1 AS BIT)),
    ('PRD-008', N'Marcador de texto amarelo',   CAST(  5.90 AS DECIMAL(18,2)), 250, CAST(1 AS BIT)),
    ('PRD-009', N'Pasta com elástico',          CAST( 12.30 AS DECIMAL(18,2)),   3, CAST(1 AS BIT)),   -- saldo baixo
    ('PRD-010', N'Papel sulfite A4 (500 fls)',  CAST( 32.00 AS DECIMAL(18,2)), 200, CAST(1 AS BIT)),
    ('PRD-011', N'Agenda 2026',                 CAST( 39.90 AS DECIMAL(18,2)),   0, CAST(1 AS BIT)),   -- esgotado
    ('PRD-012', N'Estojo duplo',                CAST( 27.50 AS DECIMAL(18,2)),  60, CAST(1 AS BIT)),
    ('PRD-013', N'Tesoura escolar',             CAST(  9.99 AS DECIMAL(18,2)), 150, CAST(0 AS BIT)),   -- inativo
    ('PRD-014', N'Cola bastão',                 CAST(  6.40 AS DECIMAL(18,2)), 220, CAST(0 AS BIT)),   -- inativo
    ('PRD-015', N'Agenda 2025',                 CAST( 34.90 AS DECIMAL(18,2)),  10, CAST(0 AS BIT))    -- inativo
) AS v (Sku, Nome, Preco, Estoque, Ativo)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Produtos p WHERE p.Sku = v.Sku);
GO

-- ---- Pedidos e itens (histórico de 2025 e 2026) -----------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Pedidos)
BEGIN
    BEGIN TRANSACTION;

    CREATE TABLE #Semente (Ref INT NOT NULL PRIMARY KEY, Email VARCHAR(254) NOT NULL, DataPedido DATETIME2(3) NOT NULL);
    INSERT INTO #Semente (Ref, Email, DataPedido) VALUES
        ( 1, 'maria.silva@exemplo.com',     '2025-03-10T10:00:00.000'),
        ( 2, 'joao.souza@exemplo.com',      '2025-04-02T14:30:00.000'),
        ( 3, 'ana.pereira@exemplo.com',     '2025-05-20T09:15:00.000'),
        ( 4, 'carlos.lima@exemplo.com',     '2025-06-11T16:45:00.000'),
        ( 5, 'fernanda.costa@exemplo.com',  '2025-07-07T11:00:00.000'),
        ( 6, 'rafael.mendes@exemplo.com',   '2025-08-19T13:20:00.000'),
        ( 7, 'luciana.barros@exemplo.com',  '2025-09-05T10:10:00.000'),
        ( 8, 'maria.silva2@exemplo.com',    '2025-10-14T15:00:00.000'),
        ( 9, 'paulo.almeida@exemplo.com',   '2025-11-25T12:00:00.000'),
        (10, 'rafael.mendes@exemplo.com',   '2025-12-15T18:30:00.000'),
        (11, 'ana.pereira@exemplo.com',     '2025-12-31T23:59:59.999'),   -- último instante de 2025
        (12, 'carlos.lima@exemplo.com',     '2026-01-01T00:00:00.000'),   -- primeiro instante de 2026
        (13, 'maria.silva@exemplo.com',     '2026-01-15T09:00:00.000'),
        (14, 'maria.silva2@exemplo.com',    '2026-01-28T10:30:00.000'),
        (15, 'joao.souza@exemplo.com',      '2026-02-03T14:00:00.000'),
        (16, 'joao.souza2@exemplo.com',     '2026-02-28T23:59:59.999'),   -- último instante de fevereiro
        (17, 'ana.pereira@exemplo.com',     '2026-03-01T00:00:00.000'),   -- primeiro instante de março
        (18, 'fernanda.costa@exemplo.com',  '2026-03-18T16:20:00.000'),
        (19, 'paulo.almeida@exemplo.com',   '2026-04-01T00:00:00.000'),
        (20, 'carlos.lima@exemplo.com',     '2026-04-09T11:11:00.000'),
        (21, 'marcos.teixeira@exemplo.com', '2026-04-22T08:45:00.000'),
        (22, 'maria.silva@exemplo.com',     '2026-05-06T17:05:00.000'),
        (23, 'ana.pereira@exemplo.com',     '2026-05-19T10:00:00.000'),
        (24, 'joao.souza2@exemplo.com',     '2026-06-02T13:30:00.000'),
        (25, 'fernanda.costa@exemplo.com',  '2026-06-23T09:40:00.000'),
        (26, 'carlos.lima@exemplo.com',     '2026-07-14T15:15:00.000'),
        (27, 'maria.silva2@exemplo.com',    '2026-07-30T12:00:00.000'),
        (28, 'paulo.almeida@exemplo.com',   '2026-08-11T14:45:00.000'),
        (29, 'ana.pereira@exemplo.com',     '2026-09-03T10:20:00.000'),
        (30, 'joao.souza@exemplo.com',      '2026-09-21T16:00:00.000'),
        (31, 'fernanda.costa@exemplo.com',  '2026-10-08T11:30:00.000'),
        (32, 'maria.silva@exemplo.com',     '2026-10-27T09:05:00.000'),
        (33, 'carlos.lima@exemplo.com',     '2026-11-05T13:00:00.000'),
        (34, 'ana.pereira@exemplo.com',     '2026-11-12T10:00:00.000');

    -- MERGE ... ON 1 = 0 é o jeito de devolver, junto do Id gerado, a chave de origem.
    CREATE TABLE #Mapa (Ref INT NOT NULL PRIMARY KEY, PedidoId INT NOT NULL);
    MERGE dbo.Pedidos AS destino
    USING (
        SELECT s.Ref, c.Id AS ClienteId, s.DataPedido
        FROM #Semente s
        JOIN dbo.Clientes c ON c.Email = s.Email
    ) AS origem
        ON 1 = 0
    WHEN NOT MATCHED THEN
        INSERT (ClienteId, DataPedido, ValorTotal)
        VALUES (origem.ClienteId, origem.DataPedido, 0)
    OUTPUT origem.Ref, inserted.Id INTO #Mapa (Ref, PedidoId);

    -- Itens: de 1 a 3 por pedido. Os produtos escolhidos (passo de 5 módulo 15)
    -- nunca se repetem dentro do pedido (RN-03).
    CREATE TABLE #Sequencia (Seq INT NOT NULL PRIMARY KEY, ProdutoId INT NOT NULL);
    INSERT INTO #Sequencia (Seq, ProdutoId)
    SELECT ROW_NUMBER() OVER (ORDER BY Sku) - 1, Id
    FROM dbo.Produtos
    WHERE Sku LIKE 'PRD-0[01][0-9]';

    INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario)
    SELECT m.PedidoId, p.Id, ((m.Ref + k.K) % 4) + 1, p.Preco
    FROM #Mapa m
    JOIN (VALUES (0), (1), (2)) AS k (K) ON k.K <= m.Ref % 3
    JOIN #Sequencia q ON q.Seq = (m.Ref * 3 + k.K * 5) % 15
    JOIN dbo.Produtos p ON p.Id = q.ProdutoId;

    UPDATE ped
    SET ValorTotal = t.Total
    FROM dbo.Pedidos ped
    JOIN (
        SELECT PedidoId, SUM(Quantidade * PrecoUnitario) AS Total
        FROM dbo.ItensPedido
        GROUP BY PedidoId
    ) t ON t.PedidoId = ped.Id;

    COMMIT TRANSACTION;
    DROP TABLE #Sequencia, #Mapa, #Semente;
END;
GO
