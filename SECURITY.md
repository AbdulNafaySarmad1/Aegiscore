# Security policy

## Reporting a vulnerability

Email **security@aegiscore.systems** (until that mailbox exists: the Data Protection Contact named in `docs/compliance/05-governance.md`). Please include the affected version or commit, steps to reproduce, and impact. Do not open a public issue for a security problem.

You will receive an acknowledgement within **2 business days** and a triage decision (severity, fix timeline) within **7 days**. Critical issues in a shipped release are fixed within 7 days, high within 30, per `docs/compliance/12-vulnerability-register.md`. We credit reporters in the release notes unless they prefer otherwise, and we do not pursue good-faith researchers who respect the scope below.

## Scope

- The `AegisCore.*` projects, the Portal, the Mobile app and the `aegiscore-clip` sidecar in this repository.
- `license.aegiscore.systems` (licence heartbeat endpoint).

Out of scope: universities' own installations and infrastructure (report those to the university), denial-of-service by volume, findings that require a compromised administrator account.

## Supported versions

The current release and the previous minor release receive security fixes. Installations receive fixed images through the release channel in `docs/CLIENT_DEPLOYMENT_OPERATIONS.md`.

## Disclosure

We publish an advisory in `docs/16-release-notes-changelog.md` with the fixed release, naming affected versions, and notify licensed universities through their registered contact.
