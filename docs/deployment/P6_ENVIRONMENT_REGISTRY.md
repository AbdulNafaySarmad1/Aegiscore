# P6 — Production Environment Registry

## Outcome

`EnvironmentValidator` is the single registry for 26 production variables. It validates required, conditional, integer, URL, and allowed-value rules. Production defaults to strict readiness even when `Deployment:FailOnReadinessError` is omitted. A missing or invalid required value logs `STARTUP BLOCKED` and throws `ApplicationException` before database or Redis probes run.

Setup mode registers the same validator for the secured wizard work. Validation is enforced by deployment readiness after installation; this allows a new installation to start before the wizard has collected its settings.

## Registry

| Category | Variables | Rule |
|---|---|---|
| Database | `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASS` | Required; port is a positive integer. |
| Auth / SSO | `SSO_PROVIDER`, `SSO_CLIENT_ID`, `SSO_CLIENT_SECRET`, `SSO_TENANT_ID`, `SSO_CALLBACK_URL` | Provider optional. Client ID, secret, and callback are required for non-local SSO; tenant ID is additionally required for Entra. |
| AI / Helpbot | `AI_PROVIDER`, `AI_ENDPOINT`, `AI_MODEL`, `AI_API_KEY` | Provider, endpoint, and model required. API key required for non-local providers. |
| Security / PQC | `PQC_MLKEM_PUBLIC_KEY`, `PQC_MLKEM_PRIVATE_KEY`, `PQC_MLDSA_PUBLIC_KEY`, `PQC_MLDSA_PRIVATE_KEY` | Required. |
| Cloudflare | `CF_TURNSTILE_SITE_KEY`, `CF_TURNSTILE_SECRET_KEY` | Required. Runtime verification reads the canonical secret-key name. |
| App | `APP_URL`, `APP_ENV`, `APP_SECRET`, `SESSION_LIFETIME`, `AEGISCORE_SETUP_TOKEN`, `AEGISCORE_ENCRYPTION_KEY` | Required; session lifetime is a positive integer and URLs/environments are validated. |

The accepted `AI_PROVIDER` values are `ollama`, `openai`, `granite`, and `custom`. `ollama` is treated as the local provider described as `ollama-local` by the conditional requirement. Accepted SSO values are `okta`, `entra`, `google`, `saml`, and `local`.

## Canonical-name migration

| Previous name/config path | Canonical name | Updated consumers |
|---|---|---|
| `DB_USERNAME` | `DB_USER` | Runtime DbContext builder, design-time factory, support database connection, wizard-generated environment file. |
| `DB_PASSWORD` | `DB_PASS` | Runtime DbContext builder, design-time factory, support database connection, wizard-generated environment file. |
| `OLLAMA_BASE_URL`, `AI:Ollama:BaseUrl`, `AI:OpenAI:BaseUrl`, `AI_OPENAI_BASE_URL` | `AI_ENDPOINT` | Ollama/OpenAI completion and embedding providers, application settings, all Compose variants, bootstrap generator. |
| `OLLAMA_MODEL`, `OPENAI_MODEL`, provider-specific model paths | `AI_MODEL` | Ollama/OpenAI completion and embedding providers, application settings, Compose, bootstrap generator. |
| `OPENAI_API_KEY`, `AI_OPENAI_API_KEY`, `AI:OpenAI:ApiKey` | `AI_API_KEY` | OpenAI completion/embedding providers, application settings, Compose, bootstrap generator. |
| `AI:Provider`, `AI__Provider` | `AI_PROVIDER` | Provider factory, application settings, Compose. |
| `Turnstile:SecretKey` | `CF_TURNSTILE_SECRET_KEY` | Turnstile middleware. |

`POSTGRES_*` and `APP_DB_*` remain only as container-database bootstrap variables. They are not application registry aliases; Compose maps the application-facing `DB_*` names explicitly.

## Deployment files

- `.env.example` contains all registry variables with type/requirement comments and non-secret placeholders.
- `k8s/secrets.yaml` contains Base64 placeholder values and Sealed Secrets instructions. It must never be applied unchanged or populated with real secrets in Git.
- `docker-compose.yml`, `deploy/docker-compose.prod.yml`, and `docker-compose.uni.yml` inject the canonical names.
- `scripts/bootstrap-env.sh` emits the canonical database, AI, app, SSO, Cloudflare, and PQC variables and masks generated secrets in its summary.

## Verification

- Registry unit tests cover a complete local configuration, Entra/OpenAI conditional requirements, invalid integer/URL values, and production startup blocking.
- Both main and production Compose configurations parse successfully.
- The bootstrap shell script passes `bash -n`.
