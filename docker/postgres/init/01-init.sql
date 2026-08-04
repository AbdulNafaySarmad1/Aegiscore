-- -------------------------------------------------------------------
-- 01-init.sql — AegisCore database bootstrap
--
-- Runs as the Postgres superuser during container initialization.
-- Ensures the aegiscore database exists and the pgvector extension
-- is available for AI/embedding workloads.
-- -------------------------------------------------------------------

SELECT 'CREATE DATABASE aegiscore'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'aegiscore')\gexec

\c aegiscore

CREATE EXTENSION IF NOT EXISTS vector;

DO $$
BEGIN
    RAISE NOTICE 'AegisCore database initialized: pgvector extension is ready.';
END $$;
