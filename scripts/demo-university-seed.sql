-- Retired: this SQL contained invalid UUIDs, DEMO_HASH credentials and
-- schema/tenant inconsistencies. It must never be used for a client install.
\set ON_ERROR_STOP on
DO $$ BEGIN
  RAISE EXCEPTION 'Use the opt-in DemoSeeder through POST /api/admin/demo-seed after real administrator login; see docs/WINDOWS_PILOT_RUNBOOK.md';
END $$;