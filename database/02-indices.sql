-- =============================================================================
-- 02-indices.sql — índices não clusterizados da spec §6.3.
--
-- O SQL Server não indexa FKs sozinho. Cada índice existe por causa de uma consulta
-- concreta (tabela "Consultas → índice → operador esperado" de
-- docs/esteira/02-arquitetura.md §6). Os índices moram só aqui: o modelo EF não
-- declara HasIndex.
--
-- Idempotente: cada índice só é criado se ainda não existir.
-- =============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- obrigatório para o índice filtrado UX_Pedidos_ChaveIdempotencia
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

USE JoyaPedidos;
GO

-- GET /pedidos?clienteId=… (com ou sem período): seek por cliente, já na ordem da listagem.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Pedidos') AND name = N'IX_Pedidos_ClienteId_DataPedido')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Pedidos_ClienteId_DataPedido
        ON dbo.Pedidos (ClienteId, DataPedido DESC, Id DESC)
        INCLUDE (ValorTotal);
END;
GO

-- GET /pedidos por período (ou sem filtro) e GET /relatorios/top-clientes:
-- range seek em DataPedido, cobrindo ClienteId e ValorTotal (sem key lookup).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Pedidos') AND name = N'IX_Pedidos_DataPedido')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Pedidos_DataPedido
        ON dbo.Pedidos (DataPedido DESC, Id DESC)
        INCLUDE (ClienteId, ValorTotal);
END;
GO

-- RN-14: no máximo um pedido por chave de idempotência; NULLs ficam fora do índice.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Pedidos') AND name = N'UX_Pedidos_ChaveIdempotencia')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Pedidos_ChaveIdempotencia
        ON dbo.Pedidos (ChaveIdempotencia)
        WHERE ChaveIdempotencia IS NOT NULL;
END;
GO

-- Suporte à FK ItensPedido → Produtos (validação de integridade e consultas por produto).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ItensPedido') AND name = N'IX_ItensPedido_ProdutoId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ItensPedido_ProdutoId
        ON dbo.ItensPedido (ProdutoId);
END;
GO
