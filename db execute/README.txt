YasGMP DB Alignment & Smoke Tests
=================================

Contents
- 01_align_core.sql     â€” Aligns DB schema to match the app models
- 02_smoke_tests.sql    â€” Exercises CAPA/Deviation/Parameters/API/Logins

Connection (example)
- Server   : localhost
- Database : yasgmp
- User     : root
- Password : Jasenka1
- Charset  : utf8mb4

Run (PowerShell)
1) Align core schema
   Get-Content -Raw "01_align_core.sql" | mysql --protocol=tcp -h localhost -u root -pJasenka1 --default-character-set=utf8mb4 -D yasgmp

2) Run smoke tests
   Get-Content -Raw "02_smoke_tests.sql" | mysql --protocol=tcp -h localhost -u root -pJasenka1 --default-character-set=utf8mb4 -D yasgmp

Run (CMD)
  mysql --protocol=tcp -h localhost -u root -pJasenka1 --default-character-set=utf8mb4 -D yasgmp < 01_align_core.sql
  mysql --protocol=tcp -h localhost -u root -pJasenka1 --default-character-set=utf8mb4 -D yasgmp < 02_smoke_tests.sql

Verification
- Recent triggers:
  SELECT COUNT(*) FROM system_event_log WHERE event_time > NOW() - INTERVAL 10 MINUTE;

- Column checks:
  SELECT column_name FROM information_schema.columns WHERE table_schema='yasgmp' AND table_name='system_event_log';
  SELECT column_name FROM information_schema.columns WHERE table_schema='yasgmp' AND table_name='user_login_audit';

Notes
- The CAPA status enum is standardized to: otvoren | u_tijeku | zatvoren.
  If your environment already uses a broader set, adjust 01_align_core.sql accordingly before running.


3) Generate per-model add-column script (03_schema_sync.sql)
   PowerShell:
     .\generate_schema_sync.ps1

   CMD:
     generate_schema_sync.cmd

  This writes 03_schema_sync.sql with ALTER TABLE ADD COLUMN statements for all models
  where the column is missing, based on current DB metadata and compiled models.

If CAPA insert fails due to missing ref tables/procedure
- Error example: Table 'yasgmp.ref_domain' doesn't exist
- Run the reference support script, then re-run smoke tests:
  Get-Content -Raw "01a_ref_support.sql" | mysql --protocol=tcp -h localhost -u root -pJasenka1 --default-character-set=utf8mb4 -D yasgmp
