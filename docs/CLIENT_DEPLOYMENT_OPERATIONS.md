# Nocturne implementation and release operations

Clients receive the compose file, secret configuration, and private runtime image.
The final image contains compiled .NET assemblies and the Next.js standalone runtime;
no repository checkout, .NET SDK, build context, or source mount is delivered.
Browser source maps and .NET debug symbols are excluded from the published artifacts.
JavaScript is necessarily delivered to browsers and .NET assemblies can be decompiled;
a container is a distribution boundary, not a guarantee against reverse engineering.

Provision a client-owned supported database and Redis, apply the reviewed migration
chain once with the existing deployment tooling, and prepare the tenant/keys/license.
The client runtime disables automatic migrations: two replicas must not race schema
changes. Use expand/contract migrations compatible with both running versions before
publishing `latest`. Breaking schema changes require a planned maintenance deployment.
Configure university TLS ingress to proxy to localhost:8080, preserve Host and the
public Origin, and supply the matching HTTPS `APP_URL`. Runtime database credentials
must use the established least-privilege/RLS setup; do not give clients DBA credentials.

Two application containers share client-owned storage/config and external database/
Redis. The second replica suppresses scheduled business workers; signing queues and
license validation remain enabled. Its job is HTTP redundancy, not automatic worker
failover. The primary resumes scheduled work after restart. Configure backup/restore,
monitoring, and external database/Redis availability during implementation.

Watchtower polls every 21,600 seconds, uses the client's dedicated registry config,
and updates only labeled application replicas. Ingress is deliberately excluded so
the sole local listener is not restarted mid-update. Ingress security updates require
Nocturne maintenance or redundant university ingress; this compose is not host-level HA.
Pre-update checks the peer, returns EX_TEMPFAIL (75) to skip if unavailable, and drains
the current replica. Post-update waits for readiness with its timeout disabled, keeping
the other replica serving if a release fails. Investigate a stalled updater; restore a
known-good image through Nocturne's release process before resuming. In-flight long
requests/WebSockets can reconnect during restart; test workload-specific draining.
Do not claim unconditional zero downtime from `--rolling-restart` alone.

License validation runs at API startup and then every 24 hours. HTTPS authority
responses accept `status: valid|invalid|expired|revoked`, `message`, and
`tier: Basic|Standard|Premium`. Missing tier defaults to Basic for older authorities;
unknown tiers lock the installation. Valid responses restore active status. Existing
offline signed validation and audited temporary overrides remain available.
Basic and Standard retain core ERP access; Premium additionally enables `/api/ai`,
`/api/proctoring`, and `/api/plagiarism`. This initial module mapping must match the
commercial entitlement contract before issuing client licenses.

Invalid licenses block business mutations while authenticated reads remain available.
Authentication, license recovery, audit records, and pre-installation bootstrap are
maintenance exceptions. The EF SaveChanges interceptor also blocks scheduled business
writes; future raw-SQL/bulk writers must enforce the same policy before execution.
The billing model (implementation fee, annual license, monthly tier) remains an
authority-side commercial concern; the ERP consumes validated status and tier.

Release acceptance requires a successful image build, tests, actual private pull,
fresh-client setup, and an observed rolling update with continuous HTTP probes. None
of these external checks should be inferred from static compose validation.
