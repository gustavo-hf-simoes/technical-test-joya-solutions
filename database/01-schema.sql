-- =============================================================================
-- 01-schema.sql — banco JoyaPedidos e as quatro tabelas (spec §6.2).
--
-- Única fonte de verdade do schema (sem migrations). Executado por database/init.sh
-- com `sqlcmd -I -b` e o login sa, no compose (db-init) e nos testes de integração
-- (ADR-0003).
--
-- Idempotente: cada objeto só é criado se ainda não existir. Rodar de novo não
-- falha, não recria nada e não derruba conexões (o ALTER DATABASE só roda se o
-- READ_COMMITTED_SNAPSHOT ainda estiver desligado).
-- =============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- obrigatório para o índice filtrado de 02-indices.sql; o sqlcmd liga com -I
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'JoyaPedidos') IS NULL
BEGIN
    CREATE DATABASE JoyaPedidos;
END;
GO

-- Spec §4.3: READ COMMITTED com versionamento de linha. Leitores (listagem, relatório)
-- não bloqueiam nem são bloqueados pela baixa de estoque.
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'JoyaPedidos' AND is_read_committed_snapshot_on = 0)
BEGIN
    ALTER DATABASE JoyaPedidos SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
END;
GO

USE JoyaPedidos;
GO

IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes (
        Id        INT IDENTITY(1,1) NOT NULL,
        Nome      NVARCHAR(150)     NOT NULL,
        Email     VARCHAR(254)      NOT NULL,
        Ativo     BIT               NOT NULL CONSTRAINT DF_Clientes_Ativo    DEFAULT (1),
        CriadoEm  DATETIME2(3)      NOT NULL CONSTRAINT DF_Clientes_CriadoEm DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Clientes            PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Clientes_Email      UNIQUE (Email),
        CONSTRAINT CK_Clientes_Nome_Vazio CHECK (LEN(LTRIM(RTRIM(Nome))) > 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.Produtos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Produtos (
        Id        INT IDENTITY(1,1) NOT NULL,
        Sku       VARCHAR(40)       NOT NULL,
        Nome      NVARCHAR(150)     NOT NULL,
        Preco     DECIMAL(18,2)     NOT NULL,
        Estoque   INT               NOT NULL,
        Ativo     BIT               NOT NULL CONSTRAINT DF_Produtos_Ativo    DEFAULT (1),
        CriadoEm  DATETIME2(3)      NOT NULL CONSTRAINT DF_Produtos_CriadoEm DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Produtos              PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Produtos_Sku          UNIQUE (Sku),
        CONSTRAINT CK_Produtos_Preco        CHECK (Preco >= 0),
        CONSTRAINT CK_Produtos_Estoque      CHECK (Estoque >= 0),   -- última linha de defesa da RN-08
        CONSTRAINT CK_Produtos_Nome_Vazio   CHECK (LEN(LTRIM(RTRIM(Nome))) > 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.Pedidos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pedidos (
        Id                 INT IDENTITY(1,1) NOT NULL,
        ClienteId          INT               NOT NULL,
        DataPedido         DATETIME2(3)      NOT NULL CONSTRAINT DF_Pedidos_DataPedido DEFAULT (SYSUTCDATETIME()),  -- HL-20
        ValorTotal         DECIMAL(18,2)     NOT NULL,
        ChaveIdempotencia  UNIQUEIDENTIFIER  NULL,
        CONSTRAINT PK_Pedidos            PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Pedidos_Clientes   FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes (Id),
        CONSTRAINT CK_Pedidos_ValorTotal CHECK (ValorTotal >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.ItensPedido', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItensPedido (
        PedidoId       INT           NOT NULL,
        ProdutoId      INT           NOT NULL,
        Quantidade     INT           NOT NULL,
        PrecoUnitario  DECIMAL(18,2) NOT NULL,
        CONSTRAINT PK_ItensPedido            PRIMARY KEY CLUSTERED (PedidoId, ProdutoId),   -- RN-03
        CONSTRAINT FK_ItensPedido_Pedidos    FOREIGN KEY (PedidoId)  REFERENCES dbo.Pedidos  (Id),
        CONSTRAINT FK_ItensPedido_Produtos   FOREIGN KEY (ProdutoId) REFERENCES dbo.Produtos (Id),
        CONSTRAINT CK_ItensPedido_Quantidade CHECK (Quantidade > 0),
        CONSTRAINT CK_ItensPedido_Preco      CHECK (PrecoUnitario >= 0)
    );
END;
GO

-- Todas as FKs usam ON DELETE NO ACTION (padrão): não há exclusão em cascata de
-- histórico de vendas (spec §6.2).
