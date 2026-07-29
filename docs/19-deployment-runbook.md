# Nocturne Deployment Runbook

## CI/CD

Push to `main` runs backend tests, portal build, mobile typecheck, Docker build, image push, and Kubernetes rollout when `KUBE_CONFIG` is configured in GitHub Actions secrets.

## Required Secrets

- `KUBE_CONFIG`
- `NOCTURNE_DB_CONNECTION`
- `NOCTURNE_JWT_SECRET`
- `LICENSE_HMAC_SECRET`
- `NOCTURNE_PQC_KEY_BASE64`
- `GOVERNANCE_FEEDBACK_KEY_BASE64`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`

## Kubernetes Deploy

1. Build and push `ghcr.io/<owner>/<repo>/nocturne-api:<sha>`.
2. Create `nocturne-runtime-secrets` from the runtime secret store.
3. Apply `k8s/namespace.yaml`.
4. Apply the remaining manifests.
5. Run `kubectl -n nocturne rollout status deploy/nocturne-api --timeout=180s`.
6. Run `curl -f https://api.nocturne.university.edu.pk/health`.

## Database Migrations

The baseline EF migration is in `AegisCore.Infrastructure/Migrations`.

For controlled production rollout, run the idempotent script:

`psql "$NOCTURNE_DB_CONNECTION" -f AegisCore.Infrastructure/Migrations/V1.0_NocturneBaseline.sql`

For automated rollout, set `Database__AutoMigrate=true` on one deployment window only, then switch it back to `false`.

## Local Compose

Copy `.env.example` to `.env` and replace all example secrets, or run `docker compose --env-file .env.example up -d --build` for disposable local smoke testing only.

The Compose file requires secrets through environment interpolation so runtime manifests do not carry dummy credentials directly.

## Rollback

1. `kubectl -n nocturne rollout undo deploy/nocturne-api`
2. `kubectl -n nocturne rollout status deploy/nocturne-api --timeout=180s`
3. Confirm `/health` returns 200.

## Store Distribution

React Native builds use `Nocturne.Mobile/eas.json`.

1. `cd Nocturne.Mobile`
2. `eas build --platform android --profile production`
3. `eas build --platform ios --profile production`
4. Submit AAB/IPA through Play Console and App Store Connect with university demo credentials, privacy policy, terms, and six screenshots per store.

## Portal

Cloudflare Pages uses `Nocturne.Portal/wrangler.toml` and publishes `dist` after `npm run build`.
