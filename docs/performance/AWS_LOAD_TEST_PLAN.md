# AegisCore AWS_LOAD_TEST_PLAN.md

Load-test preparation for AWS deployment. Generated 2026-08-11, branch `agent/prod-hardening`.
No expensive cloud infrastructure has been run. This is the approved plan for a future authorized run. Target scale: ~10,000 users per institution.

## 1. Objectives

- Establish baseline p50/p95/p99, throughput, error rate at 100 → 500 → 1,000 → 2,500 → 5,000 → 10,000 concurrent users.
- Quantify CPU, memory, GC, allocations, DB saturation, Redis saturation, connection-pool pressure.
- Identify the first-saturated resource (expected: Postgres via license read + OBE/report hot paths; validated here).
- Produce regression gates for future A-queue optimization.

## 2. Environment (AWS, when authorized)

- Suggest: ECS Fargate or EKS clone of `k8s/` manifests; RDS PostgreSQL (pgvector) + ElastiCache Redis 7; API behind Cloudflare or ALB for origin testing.
- Metrics: CloudWatch + Prometheus (`/metrics` already exported) + `pg_stat_statements` (enabled in uni compose) + Redis INFO + k6 (Grafana Cloud or self-hosted).
- Disposable only; tear down after each run.

## 3. Workload model (realistic traffic mix)

| Scenario | Share | Endpoints exercised | Notes |
|---|---|---|---|
| Login | 5% | `POST /api/auth/login`, `/api/session` | Argon2 + GeoIP + license read |
| Student dashboard | 25% | `/api/session`, `/api/student-portal/me`, `/attendance/history`, `/finance/v2/student-portal/:id` | replicates portal waterfall |
| Faculty dashboard/classes | 10% | `/api/session`, `/api/courses/my`, `/api/attendance/roster/:course` | |
| Course registration | 8% | registration GET+POST | conflict/seat/fee queries |
| Attendance marking | 8% | `/api/attendance/mark` | batched writes |
| Grade entry | 5% | `/api/faculty-grade-entry/*` | N+1 grading risk |
| Grade approval | 5% | `/api/grade-approvals`, approval review | |
| OBE attainment/report | 7% | `/api/obe/v2/*`, signed-report | N+1 + synchronous PDF |
| Finance dashboards | 10% | `/api/finance/v2/dashboard/*` | aggregate queries |
| Document generation | 4% | transcript/official, finance signed reports | synchronous PDF/PQC/QR |
| Verification | 5% | transcript verify, QR verify | anonymous endpoints |
| Admin/search | 8% | admin lists, search | pagination behavior |

## 4. Stage matrix

| Stage | Concurrent users | Ramp | Duration | Pass criteria (initial hypothesis — to validate) |
|---|---|---|---|---|
| S1 | 100 | 1 min | 10 min | p95 < 300ms CRUD; 0% 5xx; DB pool < 40% |
| S2 | 500 | 2 min | 15 min | p95 < 400ms; error < 0.5% |
| S3 | 1,000 | 3 min | 20 min | p95 < 500ms; error < 1%; GC gen0 < 100/s |
| S4 | 2,500 | 5 min | 25 min | p95 < 700ms; error < 1%; pool < 70% |
| S5 | 5,000 | 8 min | 30 min | p95 < 1s CRUD; reports p95 < 3s; pool < 85% |
| S6 | 10,000 | 12 min | 40 min | system stable; no connection exhaustion; identify first bottleneck |

## 5. Metrics to capture

- Latency percentiles: p50 / p95 / p99 per scenario.
- Throughput: req/s, VUs.
- Error rate: 4xx/5xx breakdown.
- API: CPU (expected ~1vCPU per X req/s — measure), memory, GC counts/allocations (dotnet-counters on a pod), threadpool starvation signals.
- Postgres: `pg_stat_statements` top queries (expect license read, OBE CourseGrades scan, attendance scan), `pg_stat_activity` active/queued, max_connections 100 limit, lock waits, `EXPLAIN ANALYZE` on top queries.
- Redis: `INFO commandstats` latency, memory, evictions (512MB/ allkeys-lru in uni).
- Connection pool: EF `MaxPoolSize=200` saturation, Npgsql pool waits.
- Autoscaling signals: k8s HPA CPU 70%, Kestrel 1000-conn cap, DB pool %, Redis memory.

## 6. Resilience scenarios (chaos)

| Scenario | Procedure | Expected behavior | Gate |
|---|---|---|---|
| Redis unavailable | Stop Redis | AbortOnConnectFail=false → cache degradation; auth refresh fails; replay/idempotency degrade | App stays up; graceful errors |
| Slow PostgreSQL | Introduce latency/`pg_sleep` | License read + hot queries stretch p95 | p95 degradation bounded; no cascade |
| License authority unavailable | Block license server outbound | Heartbeat fails; after 2 failures lockout engages (fail-closed) | Documented lockout + override path works |
| External dependency timeout | Point gateway/GeoIP to blackhole | Timeouts honored (30s/3s); no threadpool exhaustion | Error surface bounded |
| API restart | Kill pod during load | k8s rollout; readiness gate; no stuck connections | Zero sustained errors |
| Slow client | k6 slow-read | KeepAlive 120s; connection cap 1000 | No connection leak |
| Malformed input | Fuzz auth/session endpoints | Validation middleware; 400s; no 500/exception leaks | No unhandled exceptions |
| Low-rate fuzzing | 1 req/s aggressive payloads on expensive endpoints | Targeted rate-limit (post-F queue) holds; no amplification | Origin limits effective |
| Large upload | 50MB document-vault | Kestrel cap enforced; parser path timed out safely | No OOM |
| Expensive authenticated request | Repeated transcript/OBE/report generation | Queue/limits hold; no thread starvation | p95 of healthy traffic unaffected |

## 7. Reporting

Output: per-stage results table + top-10 `pg_stat_statements` + flamegraph/dotnet-trace highlights + bottleneck identification + recommendations mapped to `AGENT_HANDOFF.md` queues (A1/A2/A4/A9, G1-G4).

## 8. Authorization note

Do NOT run this plan unless explicitly authorized. It provisions paid AWS resources. This document is the approved blueprint only.
