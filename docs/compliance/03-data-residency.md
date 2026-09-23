# 03 — Data residency by deployment model

> **Each installation records its own answer.** Deployment model, database, file-storage, backup and
> replica locations are settings at **Administration → Compliance** (`21-configurable-decisions.md`),
> reported as unmet until filled in. What follows describes what each deployment model *can* do and
> where the boundaries of responsibility fall; it does not assert where any particular university's
> data actually is.

**As of:** 2026-09-20. Residency matters for the draft PDPA's cross-border transfer clauses, the NDGP 2026's data-sovereignty rules for public-sector data, and whatever HEC eventually asks. The answer differs per compose file, so each is stated separately.

## Model A — Per-university container (`docker-compose.client.yml`) — the primary offer

| Component | Where it physically runs | Controlled by | Evidence |
|---|---|---|---|
| API + Portal containers (`app-a`, `app-b`, `ingress`) | University host (on-prem or the university's chosen VPS) | University | `docker-compose.client.yml:38-140` |
| PostgreSQL | **Not shipped** — `AEGISCORE_DB_CONNECTION` is required and points at a database the university provides | University | `:53 ${AEGISCORE_DB_CONNECTION:?Required}` |
| Redis | Same — `AEGISCORE_REDIS_CONNECTION` required | University | `:54` |
| Files (`/app/storage`) | Docker volume `university_storage` on the host | University | `:177` |
| Secrets vault, generated keys, JWT keys, Data Protection ring | Docker volume `university_config` | University | `:178` |
| CLIP sidecar + model cache | Same host, `clip_models` volume | University | `:141-159,179` |
| Backups | Wherever the university points `backup.sh`; S3 only if credentials are set | University | `deploy/scripts/backup.sh` |
| Licence heartbeat | Outbound to `license.aegiscore.systems` (vendor-hosted; hosting location **to be recorded** in the DPA) — no personal data | Vendor | `LicenseService.cs:173-176` |

**Residency statement for Model A:** all personal data stays on infrastructure the university selects. If the university picks a Pakistani host, no personal data leaves Pakistan unless the university itself enables an optional integration in `02-data-flow-map.md` §3 (OpenAI, Azure OCR, Microsoft Graph, Twilio, S3 backups, Cloudflare Turnstile). The vendor cannot reach the data.

Caveat to state honestly: **Nocturne cannot certify the university's own host** — the disk encryption, physical security and backup location of the university's PostgreSQL are the university's controls and go in the DPA as controller obligations.

## Model B — Vendor-hosted cloud (`docker-compose.cloud.yml`)

| Component | Where | Controlled by | Evidence |
|---|---|---|---|
| API + Portal | A VPS chosen at deploy time (`deploy-cloud.yml` takes an SSH `target_host` input) | Vendor | `.github/workflows/deploy-cloud.yml:17-45` |
| PostgreSQL / Redis | Managed services the operator provisions (`DATABASE_URL` required — "AWS RDS / Azure Database / GCP Cloud SQL") | Vendor | `docker-compose.cloud.yml:4-8,22` |
| Files | `AEGISCORE_STORAGE_PROVIDER` — S3/Azure/R2 in practice | Vendor | `Cloud/Storage/` |
| Images | `ghcr.io/abdulnafaysarmad1/aegiscore-*:${IMAGE_TAG:-latest}` | Vendor | `docker-compose.cloud.yml:18,37` |

**Residency statement for Model B:** *unknown until an actual region is chosen.* `docs/06-security-architecture-document.md` names Hetzner Helsinki (primary) and Nuremberg (DR) — i.e. **Germany/Finland, inside the EU**. That is 48 mentions across `docs/` and `README.md`, none backed by a config file in this repository. Two consequences:

1. If Model B is ever sold to a Pakistani public university, student data would be hosted outside Pakistan. The NDGP 2026 draft treats government data as sovereign and regulates cross-border transfer; expect this to be disallowed or to require approval for public-sector data once notified.
2. Hosting in the EU does **not** make GDPR apply to Pakistani students' data, but it does put the vendor under EU jurisdiction as a data processor for whatever it hosts.

**Decision required (Phase 2):** either (a) commit to a Pakistani hosting region for Model B and document it, or (b) restrict Model B to demos and non-production pilots and state that in the DPA. Until decided, do not describe Model B as available for production student data.

## Model C — Hybrid (`docker-compose.hybrid.yml`)

Local PostgreSQL primary with WAL archiving to S3-compatible storage. Residency = the local host **plus** wherever the WAL bucket lives. Same rule as Model A's S3 backups: the bucket region must be recorded per install, and the archive is a full copy of the database.

## Model D — Kubernetes (`k8s/`)

Manifests only; residency follows the cluster. Not assessed further in Phase 1 because no client runs it.

## Cross-border transfer register (to maintain from Phase 2)

| Transfer | Model | Personal data? | Destination | Legal basis today | NDGP/PDPA risk |
|---|---|---|---|---|---|
| Licence heartbeat | A, B, C | No (5 metadata fields) | Vendor licence authority | Contract | Low — state hosting location |
| OpenAI (if enabled) | any | Yes — free text | USA | University's choice; needs notice | High for public-sector data |
| Azure Document Intelligence (if enabled) | any | Yes — document images | Azure region chosen | Same | High |
| Microsoft Graph (if enabled) | any | Yes — names, emails | Microsoft 365 tenant region | University already has M365 | Medium |
| Twilio / WhatsApp / FCM (if enabled) | any | Yes — phone, message | USA / global | Notice | Medium |
| S3 backups / WAL archive (if enabled) | A, C | Yes — everything | Bucket region | Must be encrypted and in-region | High if out of country |
| Managed DB / storage | B | Yes — everything | Provider region | — | High until region fixed |
| Cloudflare Turnstile (if enabled) | any | IP address only | Cloudflare edge | Notice | Low |
| Hugging Face model pull | any | No | — | — | None (inbound only) |
