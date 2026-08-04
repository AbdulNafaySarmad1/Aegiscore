# AegisCore Deployment Runbook

## CI/CD

Push to `main` runs backend tests, portal build, mobile typecheck, Docker build, image push, and Kubernetes rollout when `KUBE_CONFIG` is configured in GitHub Actions secrets.

## Required Secrets

- `KUBE_CONFIG`
- `AEGISCORE_DB_CONNECTION`
- `AEGISCORE_JWT_SECRET`
- `LICENSE_HMAC_SECRET`
- `AEGISCORE_PQC_KEY_BASE64`
- `GOVERNANCE_FEEDBACK_KEY_BASE64`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `POSTGRES_ADMIN_USER`
- `POSTGRES_ADMIN_PASSWORD`

## Kubernetes Deploy

1. Build and push `ghcr.io/<owner>/<repo>/aegiscore-api:<sha>`.
2. Create `aegiscore-runtime-secrets` from the runtime secret store.
3. Apply `k8s/namespace.yaml`.
4. Apply the remaining manifests.
5. Run `kubectl -n aegiscore rollout status deploy/aegiscore-api --timeout=180s`.
6. Run `curl -f https://api.aegiscore.university.edu.pk/health`.

## Database Migrations

The baseline EF migration is in `AegisCore.Infrastructure/Migrations`.

For controlled production rollout, run the idempotent script:

`psql "$AEGISCORE_DB_CONNECTION" -f AegisCore.Infrastructure/Migrations/V1.0_AegisCoreBaseline.sql`

For automated rollout, set `Database__AutoMigrate=true` on one deployment window only, then switch it back to `false`.

The API connection must use a role with `NOSUPERUSER NOBYPASSRLS`. Fresh Compose and Kubernetes databases create this role automatically. For an existing database, connect with a database administrator, create a dedicated login, transfer database ownership, and rotate `AEGISCORE_DB_CONNECTION` before deploying:

```sql
CREATE ROLE aegiscore LOGIN PASSWORD '<generated-secret>'
  NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
ALTER DATABASE aegiscore OWNER TO aegiscore;
GRANT ALL ON SCHEMA public TO aegiscore;
```

Verify the API role independently:

```sql
SELECT rolname, rolsuper, rolbypassrls FROM pg_roles WHERE rolname = 'aegiscore';
```

Both privilege flags must be `false`; strict deployment readiness rejects an unsafe role.

## Local Compose

Copy `.env.example` to `.env` and replace all example secrets, or run `docker compose --env-file .env.example up -d --build` for disposable local smoke testing only.

The Compose file requires secrets through environment interpolation so runtime manifests do not carry dummy credentials directly.

## Rollback

1. `kubectl -n aegiscore rollout undo deploy/aegiscore-api`
2. `kubectl -n aegiscore rollout status deploy/aegiscore-api --timeout=180s`
3. Confirm `/health` returns 200.

## Store Distribution

React Native builds use `AegisCore.Mobile/eas.json`.

1. `cd AegisCore.Mobile`
2. `eas build --platform android --profile production`
3. `eas build --platform ios --profile production`
4. Submit AAB/IPA through Play Console and App Store Connect with university demo credentials, privacy policy, terms, and six screenshots per store.

## Portal

Cloudflare Pages uses `AegisCore.Portal/wrangler.toml` and publishes `dist` after `npm run build`.
