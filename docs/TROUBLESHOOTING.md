# Troubleshooting AegisCore

Every failure mode this system is known to have, with the exact message it produces, what causes it,
and what to do. Ordered by when you meet it: starting, signing in, using it, and the quiet kinds
that show wrong numbers instead of an error.

If you have a trace reference from a screen, start there:

```bash
docker compose logs app | grep '<the reference>'
```

---

## 1. It will not start

### "STARTUP BLOCKED: The database did not answer within the startup wait"

The database is not running, not reachable from this container, or the connection details are wrong.
The message names what it tried.

```bash
docker compose ps
docker compose exec app sh -c 'getent hosts postgres'
```

`Database__StartupWaitSeconds` lengthens the wait if your database is simply slow to come up.

### "The database ran out of lock space while the schema was being applied (PostgreSQL 53200)"

The schema is 618 tables and is applied in one transaction, taking a lock per object. PostgreSQL
grants 64 locks per transaction by default.

Start the database server with `max_locks_per_transaction=256` and start the container again.
Nothing was half-applied: the migration transaction rolled back. The shipped compose file sets this
already; a database you brought yourself may not.

### "The database is N migration(s) behind this version (...) and Database__AutoMigrate is false"

Deliberate. Take a backup, then either start once with `Database__AutoMigrate=true` or run the
migration yourself:

```bash
docker compose run --rm app dotnet AegisCore.API.dll migrate
```

### "The database schema differs from what this version expects, and no migration would fix it"

A column was altered by hand, or a migration was edited after it had run. The message names the
tables and columns. Restore from a backup taken with the matching version. The container refuses to
start rather than serve wrong answers from a schema it does not understand.

### "Migrations were applied but the schema still differs from the code"

The build's migrations do not describe its model — a packaging fault, not yours. Do not serve
requests from it; report the message to the vendor.

### "AegisCore cannot start: Runtime:DemoMode is enabled in a Production environment"

Demo mode carries a seeded identity path. It is refused in Production, always. Set
`AEGISCORE_DEMO_MODE=false`.

### "AEGISCORE_STORAGE_PROVIDER=AZURE is not supported by this build"

Only `local` and `s3` can actually store a file here. The refusal is at startup and on purpose: a
provider that accepts uploads it cannot keep loses documents silently. Use `local` with a mounted
volume, or point `s3` at any S3-compatible endpoint — Cloudflare R2, MinIO, Wasabi, Ceph.

### The Data Protection certificate cannot be written

The `config` volume is not writable by the application user. The message names the path. Check the
volume mount and its ownership; the container runs as a non-root user.

---

## 2. Nobody can sign in

### The first sign-in after installation says the register is not answering

Expected, once. When the wizard's last step lands, the container restarts itself so that it comes up
configured, and for ten to twenty seconds nothing answers. The wizard's final screen holds until the
register is ready again before offering the sign-in link; if you left the page early or typed the
address yourself, the message is "The register is not answering yet. It restarts for a few seconds
after an installation or an upgrade; wait a moment and try again." Do that. If it is still not
answering after two minutes:

```bash
docker compose ps                 # app should be "healthy", with one restart
docker compose logs app | tail    # the last lines say what it is waiting for
```

### "Invalid credentials" for a password you are sure of

Check the account is `Active` (`GET /api/identity/users`). Suspended and deactivated accounts answer
the same way on purpose, so an attacker cannot learn which accounts exist.

### "Too many failed sign-in attempts from this connection"

Twenty failures in a minute from one client address pauses that address for five minutes. Successful
sign-ins are not counted, so a class signing in together is not affected.

If a campus behind one NAT address trips this legitimately, raise
`RateLimit__SignInFailuresPerMinute`. The per-account brute-force protection is separate and stays
strict.

### Everyone is signed out at once, or the portal keeps returning to the sign-in page

Two causes, distinguishable in the log:

- **429 answers.** The address is over `RateLimit__PerAddressPerMinute` (600 a minute by default).
  Behind a proxy that does not forward the client address, every user shares one bucket — make sure
  your ingress sets `X-Forwarded-For`.
- **The token-signing keys changed.** Restoring a `config` volume from a different installation, or
  losing it, invalidates every issued token. Everyone signs in again; nothing else is lost.

A slow or unavailable API no longer signs anyone out: the portal says the register did not answer
and offers to try again.

### A student is told "The signed-in account is not linked to a student record"

That account is a staff account. A student's account is created by admission with the same
identifier as their student record; an account created by hand in **Identity → Users** is not a
student's, whatever it is named. Admit the applicant properly, or use the student's own account.

---

## 3. A screen is empty, or refuses

### "You do not have `<Action>` permission on `<Module>`"

Exactly what it says. Assign a role carrying that permission; permissions never attach to a person
directly.

### "Request failed (404)" on a screen that should list something

Report it with the screen's name. Every route the portal calls is exercised by the test suite, so
this means a route was removed or renamed in a build — not a configuration problem.

### A module screen says its records could not be read

`/health/modules` will name the cause. The usual ones: the database is unreachable, Redis is
unreachable, or a table is missing because a migration did not run.

### "This course has no assessment scheme yet"

Marks are entered against components — the quizzes, assignments and papers that make up a course.
Until somebody sets them out, there is nothing to enter a mark against. Set them on the course.

### "No faculty member is assigned to `<course>`, so there is nobody to record this roll against"

A roll is recorded against whoever teaches the course. Assign the course to a faculty member, or
name the one marking it.

### "This roll was already recorded"

Somebody — possibly you, twice — already marked this roll for today. Attendance is once per student
per day per course, enforced by the database.

---

## 4. Money looks wrong

### The dashboard shows revenue of zero while fees have been collected

If this appears on a version before the fee-receipt posting was added, collected fees never reached
the general ledger. Upgrade, and post the historical receipts as an opening entry with your
accountant; the system will not invent them.

On a current version, revenue counts the ledger and the fee funnel counts demands — they differ by
what has been demanded and not yet paid, which is normal.

### "Payment exceeds the outstanding demand."

The demand is already settled, in full or by a scholarship or waiver. Check the demand before
re-entering the payment; a retry with the same idempotency key returns the original payment rather
than taking the money twice.

### "This institution has no finance configuration yet, so no accounting standard applies"

The statement of financial position needs to know which accounting standard the institution follows.
Set it in the finance settings. Everything else in finance works without it.

### A voucher will not post

The database refuses a voucher marked posted whose lines do not balance, and refuses to change a
voucher that is already posted. Both are enforced at the database, not in the application, so
neither can be worked around by a different route.

---

## 5. Documents

### A document is "withheld"

Its stored bytes no longer match the hash recorded when it was filed. It is withheld from download
until someone restores it, and the access log records every attempt.

Restore the file from a backup and press **Check** again. If a check keeps failing after a restore,
the storage itself is at fault — run `/health/modules` and look at the storage line.

### "That document is not in the vault"

It was never stored, it has been deleted, or the stored copy is gone from the storage provider while
its record remains. The access log distinguishes these.

### An upload fails

Check the storage line of `/health/modules` first: it writes an object, reads it back and compares.
If local storage is in use, `AEGISCORE_STORAGE_LOCAL_PATH` must point at a mounted, writable volume
— not a path inside the container's own filesystem, which a `docker pull` will throw away.

---

## 6. Registration and results

### Students cannot register, and the window looks open

The message names the window's dates: `Registration for <window> is not open. It runs from ... to
...`. A window is open only between its dates **and** with status Open.

### A course filled to fewer than its seats

Only on versions before the seat claim was made atomic. Current versions fill exactly the seats that
exist: fifty students racing for ten seats end as ten registered and forty waiting, in join order.

### Two students hold the same place on a waiting list

Only on versions before the list's renumbering was serialised. Fifty simultaneous joins used to
leave 38 or 39 distinct positions for 40 people about one run in three, because each renumbering
read its own snapshot. Current versions queue the renumbering on a database lock per course, and
the spike test runs ten times clean. If you see it on a current version, report it with the course
and window ids.

### "This student is already registered on that course for this window."

The registration already exists. The database enforces one registration per student per course per
window, so a double click cannot produce two.

---

## 7. The quiet kind: right screens, wrong numbers

These are the failures that do not announce themselves. Each was found in this system and fixed;
they are listed so that a similar symptom is recognised rather than trusted.

| Symptom | What it was |
|---|---|
| A dashboard number that never changes | A hard-coded value. `primaryHealthy = true` on the replication screen was a constant, on the one screen an operator opens to find out whether the primary answers. |
| A similarity score on a submission | A random number. There is no corpus to compare against, so the scan is refused instead. |
| "Refer to the Academic Integrity Office" on that score | A case recommendation built on that random number. |
| A collusion ring with two members | Two freshly generated identifiers. |
| A degree verification that always says "Not found" | A condition that read `if (v == null || true)`. The line beneath it would have told an employer that any code belonged to "Verified Student — BS Computer Science — 2025". |
| Convocation eligibility with every clearance ticked | One invented student, with dues, thesis, library and discipline all marked clear. |
| A storage health check that always passed | It wrote and never read back, while the provider underneath discarded every file. |
| "PQC verified ✓" on a vaulted document | An RSA signature with a fresh key per document, whose public key was stored in the same row — which proves nothing — and which verification never checked. |

The general lesson, and the thing to check when a number looks too tidy: ask where it comes from. If
the answer is "the code says so", it is not a measurement.

---

## 8. Diagnostics worth knowing

```bash
# Everything, in one answer
curl -s -b jar.txt http://localhost:8080/health/modules | jq        # jar.txt from a sign-in, see INSTALL.md §4

# What the database lacks relative to the code
curl -s -b jar.txt http://localhost:8080/health/schema | jq

# Is the schema what this build expects?
docker compose run --rm app dotnet AegisCore.API.dll migrate     # prints: initialised | migrated | verified

# Follow one request end to end
docker compose logs app | grep '<trace id from the screen>'

# Errors only
docker compose logs app | jq -c 'select(.Level == "Error" or .Level == "Fatal") | {t: .Timestamp, m: .MessageTemplate}'
```

### Error codes the API answers with

| Code | Meaning |
|---|---|
| `not_found` | No such record. |
| `invalid_request` | The request was malformed; the message says how. |
| `not_allowed` | A rule of the institution refuses it — a payment beyond the demand, a locked mark. |
| `not_configured` | Something has not been set up yet; the message names the setting. |
| `not_available` | This build cannot do it, and will not pretend to. |
| `integrity_check_failed` | Stored bytes no longer match what was recorded. |
| `concurrency_conflict` | Somebody else changed the record first. Reload and try again. |
| `license_read_only` | The licence lapsed; reading works, writing does not. |
| `rate_limit_exceeded` | Too many requests from this connection. |
| `database_unavailable`, `cache_unavailable` | The database or Redis did not answer. |
| `internal_server_error` | A defect. The reference in the message finds it in the log. |

A `5xx` that says "does not support user-initiated transactions" is a known class of defect: a
database transaction started outside the retrying execution strategy. Twenty-one of them were found
and fixed, and a test now fails the build if another is written; if you see it, the build is not the
one that passed that test.

---

## 9. When to ask the vendor

Send the output of `/health/modules`, the application log around the trace reference, and the image
tag. Those three answer most questions without another round trip.

Ask straight away, rather than working around it, if:

- the container refuses to start with "Migrations were applied but the schema still differs";
- `/health/modules` reports a module whose tables are missing after a successful migration;
- a document fails its check immediately after being filed;
- the ledger's chain integrity reports that entries do not verify.

Each of those means the build or the data is not what it should be, and a workaround would only
bury it.
