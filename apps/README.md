# Applications

This directory is the destination for AegisCore executable hosts:

- `api`: ASP.NET Core composition root
- `portal`: Next.js 16 web application
- `worker`: background processing host
- `setup`: installation tooling

Hosts compose modules. They do not own university business rules.

Existing hosts move here only in CI-green commits that update every build,
deployment, and documentation path atomically.
