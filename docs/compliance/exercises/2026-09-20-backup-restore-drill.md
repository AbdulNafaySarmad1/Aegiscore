# Backup / restore drill — 2026-09-20

**Control:** AC-36, AC-37, AC-70 (ISO A.8.13, A.8.14; NIST RC.RP). **Run by:** compliance build-out, on the developer workstation against the pilot PostgreSQL 17 container (`aegiscore-pilot-postgres-2`, database `aegiscore_pilot`, full migrated schema).

**Procedure:** `deploy/scripts/backup-restore-drill.sh` (marker row → `backup.sh` → `restore.sh` into a fresh database → assert).

**Output (verbatim):**

```
[DRILL] 05:40:20 Source: aegiscore_pilot  Target: aegiscore_pilot_drill_1789882820  Marker: drill-20260920T054020-130
[DRILL] 05:40:21 Encrypted archive: /tmp/aegiscore-drill.dpHeDM/aegiscore-20260920-054020.dump.enc (1.3M)
[RESTORE] 05:40:21 Checking SHA-256...
[RESTORE] 05:40:21 Decrypting...
[RESTORE] 05:40:21 Restoring into aegiscore_pilot_drill_1789882820 on 127.0.0.1...
[RESTORE] 05:40:33 Restore complete.
[DRILL] 05:40:33 Tables: source=576 restored=576  Marker rows restored: 1
[DRILL] 05:40:33 PASS — restore verified. Elapsed: 13s
```

**Result:** PASS. Archive was encrypted (OpenSSL `Salted__` header), no plaintext dump remained in the backup directory, all 576 tables and the marker row restored. RTO for a 1.3 MB pilot database: 13 s (this is not an RTO commitment for a production-sized database — measure per installation).

**Follow-ups:** the same drill now runs in CI on every push (`ci.yml`, postgres migration leg). Each University runs it quarterly against its own installation and files the output here.
