# Administering AegisCore

For the person who keeps this running after the installation. It covers the jobs that recur, the
decisions only an administrator can make, and the things that look alarming but are not.

Read **[INSTALL.md](INSTALL.md)** first if the system is not yet running, and
**[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** when something is wrong.

---

## People and access

### How an account becomes a student's account

A student account is created by admission, not by hand. When an applicant is moved to **Enrolled**
on the admissions board, the system creates their student record and their user account **with the
same identifier**, and that identity is what makes the student screens theirs: their sign-in token
carries `student_id`, and the transcript, fee status, timetable and check-in screens read it.

An account created by hand through **Identity → Users** is a staff account. It has no student
record behind it, and the student screens will tell its owner so.

### Giving somebody access

1. **Identity → Users → new user** — name, username, email. No password is set.
2. **Set their password** — `POST /api/identity/users/{id}/password`, or hand the job to them with
   an activation link. A password is 16 to 128 characters; nothing else is required of it, because
   length is what matters and rules that demand a symbol produce `Passw0rd!`.
3. **Assign a role** — `POST /api/identity/users/assign-role`. Permissions come from roles, never
   from the person.

Every password an administrator sets is recorded in the audit log against that administrator's
name, with the account it was set on.

### Signing in with Entra, Google Workspace or Okta

The setup wizard's third step chooses how people sign in: email and password, Microsoft Entra ID,
Google Workspace, Okta, or several of these at once. The login screen offers exactly what was
chosen; a person who signs in through a provider is matched by (tenant, issuer, subject) and given
roles from the provider's group mapping. The full account — setup, what happens at sign-in, and
the checklist for proving it with a real directory account — is in [SSO.md](SSO.md).

### Roles and permissions

A permission is a pair: a module and an action (`Attendance` + `Read`, `System` + `Write`). A role
carries a set of them, and a person carries roles. The portal shows each person only the modules
their roles allow, and the API refuses the rest with `403` and the permission it wanted:

```json
{ "error": "You do not have Read permission on Students." }
```

That is the sentence to search for when somebody says a screen is empty.

---

## The licence

**Licence** (`/admin/license`) shows what the installation is licensed to do, when the last
heartbeat succeeded, and what will happen next.

- **Active** — nothing to do.
- **Warning** — heartbeats are failing. The page says when the grace period ends. The installation
  keeps working; writes are unaffected until then.
- **Read-only** — the grace period passed. Every screen still opens and every report still runs;
  writes are refused with `403 license_read_only` and a sentence saying what to do.

To restore writing: fix the network path to the authority and press **Check now**, or enter an
**offline licence key**, which is verified on the machine with no network call. Ask the vendor for
one if this installation has no route to the internet by design.

---

## Settings

**Settings → University settings** holds what the institution calls itself and the conventions it
writes in: the name printed on transcripts, the short name in the navigation, the locale that
decides how dates and numbers are written, the currency, the default payment gateway, the
accrediting body, and what the national identity document is called where you are.

Each of these can also be set as an environment variable before the first start, so a new container
comes up already carrying the institution's name.

Two related screens:

- **Settings → Institutional email** — where a student's university address comes from when they are
  admitted: by hand, from Google Workspace, or from Microsoft 365. Credentials are encrypted before
  they are stored and are never sent back to a browser, which is why the box is empty even when
  credentials are held.
- **Campus boundaries** — the ground a student must be standing on for a geofenced attendance
  check-in to count. The editor plots on a plain grid and contacts no map service; if you want a
  basemap behind it, set `NEXT_PUBLIC_MAP_TILE_URL` to a tile server you trust.

---

## Documents and the vault

**Document vault** holds records under a retention lock: 50 years for degrees, certifications and
financial records; 20 for fee challans, grade sheets and notices; 5 for everything else. The
category chosen at filing decides the tier, and the term cannot be shortened afterwards.

Each document is signed over its content hash with this installation's registered post-quantum key
(ML-DSA-65, or the stateless archival algorithm for the fifty-year tier). **Check** on a document
re-reads the stored bytes, hashes them again and verifies that signature. It answers one question —
*is this still the document that was filed?* — and a document that fails is withheld from download
until somebody restores it from a backup.

It is not a check that a document is genuine or correct. Only that nothing has altered it.

---

## Money

### How a fee reaches the books

A fee demand is a bill; it posts nothing. When a payment settles — by hand or through a gateway —
the system posts one receipt in the same transaction: **debit 1010 Bank, credit 4000 Student fee
revenue**. Income is recognised when the money is received.

If your institution recognises fee income when the demand is raised instead, that needs a posting at
demand time as well, and a decision about deferred revenue. This system does not make that choice
for you and does not pretend to have made it.

### Reading the finance screens

- **Finance dashboard** — revenue, expenses, cash, and the fee collection funnel. The funnel counts
  demands; revenue and cash count the ledger. They differ by whatever has been demanded and not yet
  paid, which is the point of showing both.
- **Reports** — receivables ageing, cash position, statement of financial position. The last one
  needs an accounting standard configured for the institution; until then it says so and names the
  setting rather than failing.
- **Audit and compliance** — the ledger's own integrity: whether every entry verifies against the
  chain, when the last seal was taken, and which key signed it.

### The auditor's door

`/auditor` is for an external auditor who has been given a time-limited token. It is deliberately
outside the staff shell: an auditor has no account here, and should not need one. Issue the token
from **Audit and compliance**, and it expires on its own.

---

## Keys

### Token-signing keys

Access tokens are signed with an RSA key pair generated on first start into the `config` volume.
Every token carries a key id, so a rotation does not invalidate the tokens already issued.

```bash
docker compose exec app dotnet AegisCore.API.dll rotate-jwt-keys
docker compose restart app
```

The previous public key is kept as `jwt-signing.previous.pub` and still validates tokens signed
before the rotation. Once those have expired — `SESSION_LIFETIME`, 15 minutes by default — the file
can go.

### Data Protection

The key ring is encrypted with a PKCS#12 certificate on the `config` volume, generated on first
start with its password stored beside it. If you supply your own, set `DataProtection__CertificatePath`
and `DATA_PROTECTION_CERTIFICATE_PASSWORD`. Production refuses to start without one, because
without it every protected value would be readable by anyone holding the volume.

### The master key, and the columns it encrypts

`AEGISCORE_PQC_KEY_BASE64` — generated into the `config` volume on first start — encrypts the local
secret vault, the IP addresses in the access audit, and, since this release, the columns listed in
`AegisCore.Infrastructure/Security/FieldEncryption/FieldEncryptionModel.cs`: CNICs, passport and
visa numbers, applicant and guardian contact details, counselling and disciplinary text,
mental-health tickets, and where people signed in from (IP, city, coordinates). A backup of the
database without that key is unreadable for those columns — the same rule as the Data Protection
certificate above.

An installation upgraded from an earlier release keeps reading its existing rows; run the
rewrite once so that no plaintext remains:

```bash
docker compose exec app dotnet AegisCore.API.dll encrypt-fields
```

It is safe to run again. Rotating this key is not yet supported (it would mean re-encrypting the
vault, the audit log and every column above in one pass); it is recorded as an open item in
`docs/compliance/04-nist-csf-gap-assessment.md`, control AC-23.

### Signing keys for the ledger and the vault

Post-quantum signing keys are per tenant and per purpose, registered in the database with their key
ids and held in the secret store. Rotation is a key ceremony from the finance module; the old key
stays valid for verifying what it already signed.

---

## Watching it run

| Endpoint | Answers |
|---|---|
| `/health` | Is the process alive. |
| `/health/live` | The same, for a liveness probe. |
| `/health/ready` | Is it ready to serve — every startup check, by name. |
| `/health/modules` | Per module: is its code wired, are its tables there, do its dependencies answer. Infrastructure is **proved**, not assumed: storage is written and read back, Redis is pinged, the schema is compared against the code. |
| `/health/schema` | What the live database lacks relative to the code, by name. |
| `/admin/health` | The two reports above as a screen, for the administrator. **System health** on the dashboard. |

`/health/modules` is cached for ten seconds, so a monitor may poll it as often as it likes. It
answers `503` when anything it depends on is unavailable, with a sentence naming the setting that
would fix it.

### What the logs look like

Production logs are JSON, one object per line, with a `TraceId` on every request. When a person
reports an error, they will have been given a reference — search for it:

```bash
docker compose logs app | grep '0HNOLJ4RHR62J'
```

Every `5xx` answer the portal shows carries that reference, and every `4xx` answer carries a
sentence the person can act on instead.

---

## Capacity

Measured on the pilot stack — one application container, one PostgreSQL, one Redis, on a laptop:

| | |
|---|---|
| Sustained throughput | 662 requests a second over 45 seconds, 16 concurrent callers |
| Latency | median 15 ms, p95 34 ms, p99 41 ms |
| Registration spike | 50 students racing for 10 seats settle as 10 and 40, slowest request 421 ms |
| Sign-in | about 1.5 s under a 30-way burst — deliberate: passwords are hashed with Argon2, and that cost is the defence |

Sign-in is the only CPU-bound path. If a cohort of thousands signs in at one moment, add
application replicas rather than lowering the hashing cost.

Raise `RateLimit__PerAddressPerMinute` before a campus of any size goes behind a single NAT address:
the default of 600 a minute is generous for one person and small for four thousand.

---

## Backups, and testing them

The three volumes in the install guide — `postgres`, `config`, `storage` — are a set. Take them
together and restore them together.

Test a restore before you need one:

```bash
docker compose -f docker-compose.quickstart.yml down
docker volume rm aegiscore_postgres aegiscore_config aegiscore_storage
# restore all three from the same night, then:
docker compose -f docker-compose.quickstart.yml up -d
curl -s -b jar.txt http://localhost:8080/health/modules | jq '.status'   # jar.txt from a sign-in, see INSTALL.md §4
```

A restore that brings back the database without the `config` volume leaves every encrypted value
unreadable. That is worth finding out in a drill rather than in an incident.

For the database alone, `deploy/scripts/backup.sh` writes an encrypted `pg_dump`
(`BACKUP_ENCRYPTION_KEY` is required; the script refuses to write a plaintext dump) and
`deploy/scripts/restore.sh` brings it back into an empty database. `deploy/scripts/backup-restore-drill.sh`
runs both against your live server into a throwaway database and checks the result — run it
quarterly and file the output under `docs/compliance/exercises/`; the same drill runs on every CI
build.
