# Installing AegisCore

This is for someone who has been handed an image name and nothing else. It assumes you know how to
run Docker and nothing about this system. Everything here has been done, in this order, against a
fresh machine; where a step can fail, the failure and what it looks like are named.

---

## 1. What you need

| | |
|---|---|
| A machine | 4 CPU, 8 GB RAM, 40 GB disk for a single-campus installation. |
| Docker | Docker Engine 24 or newer, with the Compose plugin. |
| The image | The name and tag you were given, e.g. `registry.example/aegiscore:1.4.0`. |
| A port | 8080 by default, or whatever you set as `AEGISCORE_PORT`. |
| Nothing else | No cloud account, no API keys, no certificate authority, no internet connection after the image is pulled. |

The installation talks to no third party unless you configure one. There is no telemetry, no CDN,
no font service and no map service. A licence authority is contacted only if you configure one; an
offline licence key is verified on the machine (see §6).

---

## 2. Start it

```bash
curl -O https://<where you were given it>/docker-compose.quickstart.yml

AEGISCORE_RELEASE_IMAGE=registry.example/aegiscore:1.4.0 \
POSTGRES_PASSWORD=$(openssl rand -base64 24) \
APP_URL=https://register.your-university.edu \
docker compose -f docker-compose.quickstart.yml up -d
```

That is the whole installation. It starts PostgreSQL, Redis and the application, each with a named
volume, and nothing needs editing inside any container.

Set `POSTGRES_PASSWORD` before the first start on anything but a laptop. Set `APP_URL` to the
address people will actually type — the portal refuses cross-origin writes that do not match it,
and links in outgoing email are built from it.

### What happens on that first start

The application generates, once, into the `config` volume, and reuses on every restart afterwards:

- the tenant id for this institution;
- the setup token you will need in a moment;
- the encryption key and the post-quantum signing key material;
- the Data Protection key-ring certificate and its password;
- the RSA key pair that signs access tokens, with its key id.

Then it looks at the database and does one of three things, saying which in the log:

| What it finds | What it does |
|---|---|
| An empty database | Applies every migration from zero and says how many. |
| A database behind this version | Applies the pending migrations by name, then verifies the result. |
| A database that differs in a way no migration explains | **Refuses to start**, naming the tables and columns that differ. |

The third case is deliberate. A schema that does not match the code will serve wrong answers
quietly, so the container stops instead. See the troubleshooting runbook.

---

## 3. Finish the installation

```bash
docker compose -f docker-compose.quickstart.yml logs app | grep "Setup token"
```

Open `http://localhost:8080/setup`, enter that token, and answer the wizard: the institution's name
and short name, the first administrator's name, email and password, and the locale and currency.

The wizard writes the institution's identity, creates the first administrator, and seals itself: the
installation endpoints answer `410 installation_sealed` from then on, and the setup controllers are
not even loaded into the running application.

When the last step lands, the container restarts itself once so that it comes up configured. The
wizard's final screen says "Applying the installation" and holds until the register is answering
again — ten to twenty seconds — before it offers the way in. Sign in with the administrator email
and password you set; there is no other account.

**The password you set is the only way in.** There is no default account, no vendor account and no
recovery back door. If you lose it, see the runbook — recovery means an administrator with database
access setting a new password hash, and that is by design.

---

## 4. Check that it is actually working

Four checks, in order. Each one answers a question the others do not. The signed-in ones use a
cookie jar, because the portal keeps the session in a cookie and never hands the token itself to a
browser; `-H "Origin: ..."` is the cross-site check the portal applies to every write, and must
match `APP_URL`.

```bash
# 1. The process is alive.
curl -s http://localhost:8080/health
# Healthy

# 2. A real person can sign in — the administrator the wizard created.
curl -s -c jar.txt -X POST -H 'content-type: application/json' -H 'Origin: http://localhost:8080'   -d '{"email":"you@your-university.edu","password":"..."}'   http://localhost:8080/api/auth/login
# {"authenticated":true}
curl -s -b jar.txt http://localhost:8080/api/session | jq '.name, .roles'

# 3. Everything it depends on is reachable, and every module is wired.
curl -s -b jar.txt http://localhost:8080/health/modules | jq '.status, .summary, .infrastructure'

# 4. The database holds what the code expects.
curl -s -b jar.txt http://localhost:8080/health/schema | jq '.status, .summary'
```

The same report is a screen: **System health** on the administrator's dashboard (`/admin/health`).

`/health/modules` is the one to watch. It reports, per module, whether its code is wired, whether
its tables exist, and whether the services it needs answer — and, for infrastructure, it *proves*
each one rather than assuming it:

- **database** — reachable, and every table and column the code expects is present;
- **redis** — reachable, with the round-trip time;
- **storage** — an object is written, read back and compared. A provider that accepts files and
  loses them reports `unavailable` here, which is the whole point of the check;
- **smtp** — the configured mail host accepts a connection;
- **licence authority** — reachable, or offline mode is on.

It answers `503` when any of those is unavailable. A fresh installation with no mail server
configured will say so; that is honest, not broken.

---

## 5. What to configure, and where

Nothing in this list requires editing a file inside a container. Everything is an environment
variable on the `app` service, or a setting on **Settings → University settings** once you are in.

### Identity and conventions (also editable in the portal)

| Variable | What it is |
|---|---|
| `TENANT_ID` | Fixes the tenant id instead of generating one. Only for restoring an existing installation. |
| `APP_URL` | The address people type. Used for CSRF origin checks and for links in email. |
| `AEGISCORE_SEAL_URL` | An image of the institution's seal, printed on transcripts and certificates. |

### Storage — where uploaded files live

| Variable | Default | What it is |
|---|---|---|
| `AEGISCORE_STORAGE_PROVIDER` | `local` | `local` (a mounted volume) or `s3` (S3, Cloudflare R2, MinIO, or anything S3-compatible). |
| `AEGISCORE_STORAGE_LOCAL_PATH` | `/app/storage` | Where local storage writes. Must be a mounted volume, or you will lose documents on the next `docker pull`. |
| `AEGISCORE_STORAGE_ENDPOINT` | — | For S3-compatible endpoints: the service URL. |
| `AEGISCORE_STORAGE_ACCESS_KEY` / `_SECRET_KEY` | — | Credentials, if the endpoint needs them rather than an instance role. |
| `AEGISCORE_STORAGE_REGION` | `us-east-1` | The region, for S3 proper. |

A provider this build cannot actually store files with refuses at startup and names the
alternative. It never accepts a file it cannot keep.

### Mail

| Variable | What it is |
|---|---|
| `Email__SmtpHost`, `Email__SmtpPort` | Where to send outgoing mail. Without these, activation links and notifications are not delivered and `/health/modules` says so. |

### Rate limits — raise these for a large institution

| Variable | Default | What it is |
|---|---|---|
| `RateLimit__PerAddressPerMinute` | 600 | Requests a minute from one client address. Behind a NAT, a whole campus can share one address; raise it. |
| `RateLimit__PerUserPerMinute` | 600 | Requests a minute from one signed-in person. |
| `RateLimit__SignInFailuresPerMinute` | 20 | **Failed** sign-ins a minute from one address before it is paused. Successful sign-ins are not counted, so a cohort arriving together is not locked out. |
| `RateLimit__SignInLockoutMinutes` | 5 | How long that pause lasts. |

### Sessions and migrations

| Variable | Default | What it is |
|---|---|---|
| `SESSION_LIFETIME` | 900 | Access-token lifetime in seconds. Refresh tokens last 7 days. |
| `Database__AutoMigrate` | `true` | With `false`, a database that is behind stops the container instead of being changed. Use it when a DBA applies migrations by hand. |

### If you bring your own PostgreSQL

Point `AEGISCORE_DB_CONNECTION` at it and make sure of one thing:

```
max_locks_per_transaction = 256
```

The schema is 618 tables and is applied in a single transaction, which takes one lock per object.
At PostgreSQL's default of 64 the migration fails with `53200: out of shared memory`. The shipped
compose file sets this; an existing server may not have it.

---

## 6. Licensing

The installation checks its licence with the authority at `License__ServerUrl` if one is
configured. Two heartbeats can fail without consequence; past the grace period the installation
becomes **read-only** — it keeps serving every screen and refuses writes with an explanation, rather
than locking anybody out.

For a network that cannot reach the authority, ask the vendor for an **offline licence key**. It is
a signed, time-boxed key verified on the machine with no network call at all:

1. Open **Licence** in the portal (`/admin/license`).
2. Paste the key and activate it.
3. The page shows what the key covers, the institution it is bound to, and the date it expires.

Set `License__OfflineMode=true` to stop the heartbeat attempts entirely.

---

## 7. Backing it up

Three volumes matter, and all three are needed together:

| Volume | What is in it | If you lose it |
|---|---|---|
| `postgres` | Every record. | Everything. |
| `config` | The tenant id, the encryption keys, the signing keys, the key-ring certificate. | Encrypted data cannot be read back, and every issued token is invalid. |
| `storage` | Uploaded documents, unless you use S3. | Degree certificates, transcript scans, subject-access exports. |

```bash
docker compose -f docker-compose.quickstart.yml exec postgres \
  pg_dump -U aegiscore -Fc aegiscore > aegiscore-$(date +%F).dump

docker run --rm -v aegiscore_config:/config -v "$PWD:/out" alpine \
  tar czf /out/aegiscore-config-$(date +%F).tar.gz -C /config .
```

Keep the config archive as carefully as the database dump; together they are the institution.

---

## 8. Upgrading

```bash
AEGISCORE_RELEASE_IMAGE=registry.example/aegiscore:1.5.0 \
docker compose -f docker-compose.quickstart.yml up -d app
```

The new container applies pending migrations on start and says which. Take a database dump first —
a migration is not reversible in general. If you would rather apply the schema change yourself:

```bash
docker compose -f docker-compose.quickstart.yml run --rm app dotnet AegisCore.API.dll migrate
```

It prints `initialised`, `migrated` or `verified` with the detail, and exits non-zero if it refused.

---

## 9. What "installed" means

After §3 and §4, all of the following are true, and each was verified on a fresh installation:

- A real person signs in with a password they chose, and receives an RS256 token carrying a key id.
- There is no demo data, no seeded account and no bypass. Demo mode refuses to start in Production.
- The schema matches the code exactly: 618 tables, 6,048 columns, no pending migrations.
- An uploaded document is stored, read back byte-for-byte, and withheld if it is ever altered.
- A fee recorded as paid reaches the general ledger, and the cash position changes by that amount.
- Fifty students racing for ten seats end as ten registered and forty on the waiting list.

Next: **[ADMINISTRATION.md](ADMINISTRATION.md)** for running it, and
**[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** when something is wrong.
