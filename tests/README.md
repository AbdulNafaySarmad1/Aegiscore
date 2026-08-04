# AegisCore Stress Tests

Run the complete chained suite with one command:

```bash
bash tests/run-all.sh
```

The chain runs availability checks, wizard trust-boundary cases, concurrent session reads, demo-mode gateway isolation, oversized input handling, backend/frontend verification, migration drift detection, and Compose health checks. It writes `tests/reports/latest.md` and exits non-zero if any check fails.

Useful overrides:

```bash
BASE_URL=http://127.0.0.1:18080 CONCURRENCY=128 bash tests/run-all.sh
```
