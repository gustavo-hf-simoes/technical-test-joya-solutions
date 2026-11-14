-- =============================================================================
-- 03-logins.sql — logins de menor privilégio (spec §6.5).
--
--   joya_app    API .NET: SELECT, INSERT e UPDATE no schema dbo. Sem DELETE (RN-12)
--               e sem DDL.
--   claude_mcp  Servidor MCP do Claude Code: db_datareader, VIEW DEFINITION,
--               SHOWPLAN e VIEW DATABASE STATE. Somente leitura e diagnóstico.
--
-- As senhas NUNCA ficam neste arquivo: chegam como variáveis do sqlcmd (-v),
-- passadas por database/init.sh a partir de APP_SQL_PASSWORD e MCP_SQL_PASSWORD.
-- O init.sh recusa senha com aspa simples, que quebraria o literal abaixo.
--
-- Idempotente: cria o que falta; se o login já existe e a senha mudou, troca a
-- senha; religa o usuário ao login (caso o login tenha sido recriado com outro SID).
-- =============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

USE master;
GO

-- ---- Logins (nível de servidor) ---------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'joya_app')
BEGIN
    CREATE LOGIN joya_app
        WITH PASSWORD = N'$(APP_SQL_PASSWORD)',
             DEFAULT_DATABASE = JoyaPedidos,
             CHECK_POLICY = ON,
             CHECK_EXPIRATION = OFF;
END
ELSE IF EXISTS (SELECT 1 FROM sys.sql_logins
                WHERE name = N'joya_app' AND PWDCOMPARE(N'$(APP_SQL_PASSWORD)', password_hash) = 0)
BEGIN
    ALTER LOGIN joya_app WITH PASSWORD = N'$(APP_SQL_PASSWORD)';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'claude_mcp')
BEGIN
    CREATE LOGIN claude_mcp
        WITH PASSWORD = N'$(MCP_SQL_PASSWORD)',
             DEFAULT_DATABASE = JoyaPedidos,
             CHECK_POLICY = ON,
             CHECK_EXPIRATION = OFF;
END
ELSE IF EXISTS (SELECT 1 FROM sys.sql_logins
                WHERE name = N'claude_mcp' AND PWDCOMPARE(N'$(MCP_SQL_PASSWORD)', password_hash) = 0)
BEGIN
    ALTER LOGIN claude_mcp WITH PASSWORD = N'$(MCP_SQL_PASSWORD)';
END;
GO

USE JoyaPedidos;
GO

-- ---- joya_app (API) ------------------------------------------------------------
IF DATABASE_PRINCIPAL_ID(N'joya_app') IS NULL
    CREATE USER joya_app FOR LOGIN joya_app WITH DEFAULT_SCHEMA = dbo;
ELSE
    ALTER USER joya_app WITH LOGIN = joya_app;
GO

GRANT SELECT, INSERT, UPDATE ON SCHEMA::dbo TO joya_app;
-- RN-12: pedido e itens são imutáveis e nada é excluído. O DENY explícito mantém a
-- regra mesmo se alguém, no futuro, puser o usuário num papel com escrita ampla.
DENY DELETE ON SCHEMA::dbo TO joya_app;
GO

-- ---- claude_mcp (MCP, somente leitura) ----------------------------------------
IF DATABASE_PRINCIPAL_ID(N'claude_mcp') IS NULL
    CREATE USER claude_mcp FOR LOGIN claude_mcp WITH DEFAULT_SCHEMA = dbo;
ELSE
    ALTER USER claude_mcp WITH LOGIN = claude_mcp;
GO

IF NOT EXISTS (SELECT 1
               FROM sys.database_role_members rm
               WHERE rm.role_principal_id = DATABASE_PRINCIPAL_ID(N'db_datareader')
                 AND rm.member_principal_id = DATABASE_PRINCIPAL_ID(N'claude_mcp'))
    ALTER ROLE db_datareader ADD MEMBER claude_mcp;
GO

GRANT VIEW DEFINITION, SHOWPLAN, VIEW DATABASE STATE TO claude_mcp;
-- Somente leitura: o DENY vence qualquer GRANT herdado de papel.
DENY INSERT, UPDATE, DELETE ON SCHEMA::dbo TO claude_mcp;
GO
