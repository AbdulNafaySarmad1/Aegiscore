# AegisCore Technical Spec Sheet

This document describes the stack and security primitives used by the AegisCore
system as it exists in this repository.

## System Shape

AegisCore is split into two main application surfaces:

1. `AegisCore.Portal` - a Next.js 16 frontend that renders the user workspace.
2. `AegisCore.API` plus supporting .NET projects - the authoritative backend for
   authentication, business logic, workflows, storage, and security controls.

The portal is presentation and interaction. The .NET backend owns state and
enforcement.

## Frontend Stack

- Framework: Next.js 16.2.12
- Runtime: React 19.2.3
- Language: TypeScript 5
- Internationalization: `next-intl`
- Motion: `framer-motion`
- Charts and visualization: `recharts`, `d3-geo`
- Icons: `lucide-react`
- Realtime client: `@microsoft/signalr`
- Build output: static export behind Nginx in production

Portal scripts:

- `npm run dev`
- `npm run build`
- `npm run start`
- `npm run lint`
- `npm run typecheck`

## Backend Stack

- Runtime: .NET 10
- Web framework: ASP.NET Core
- Data access: Entity Framework Core 10 with Npgsql/PostgreSQL
- Database: PostgreSQL
- Logging: Serilog
- Auth: JWT bearer tokens and related Microsoft identity primitives
- Authorization: Casbin.NET
- Cache / messaging / coordination: Redis via `StackExchange.Redis`
- Validation: FluentValidation
- API docs: Swashbuckle / Swagger
- Health checks: PostgreSQL and Redis health probes
- Rate limiting: `AspNetCoreRateLimit`

## Cryptography and Security

### Password hashing

- Password hashing uses Argon2id.
- The implementation lives in
  [PasswordHashingService.cs](/home/abdul-nafay-sarmad/Music/Aegiscore/Aegiscore/AegisCore.Infrastructure/Security/PasswordHashingService.cs).
- Parameters in code:
  - Parallelism: 4
  - Iterations: 3
  - Memory: 65536 KB
  - Output length: 16 bytes
  - Salt length: 16 bytes
- The stored format is `base64(salt).base64(hash)`.
- Verification uses constant-time comparison.

### JWT signing

- JWTs are signed with RSA in the infrastructure layer.
- Production startup requires an RSA JWT authority or `Jwt:PublicKey`.
- The backend can load or generate RSA key material depending on environment.

### Backup encryption

- Backups are encrypted with AES-256-GCM.
- The implementation lives in
  [BackupEncryptionService.cs](/home/abdul-nafay-sarmad/Music/Aegiscore/Aegiscore/AegisCore.Infrastructure/Backup/BackupEncryptionService.cs).
- Key size: 32 bytes
- Nonce size: 12 bytes
- Tag size: standard GCM tag size
- The service writes an application header, nonce, ciphertext, and tag.
- The source backup file is deleted after encryption completes.

### PQC / post-quantum security

The codebase uses post-quantum-branded services and APIs for signing and
verification. The concrete implementation is a mix of modern PQC libraries and
compatibility wrappers.

- Library: `@noble/post-quantum` on the portal side
- Library: `BouncyCastle.Cryptography` on the backend side
- Signature service:
  - ML-DSA-65 for normal signing
  - SLH-DSA-SHA2-128s for archival signing
- Cryptographic verification is used for finance artifacts, document vault
  objects, audit seals, webhook validation, and other signed payloads.

Important detail:

- The repository does not contain a SHAP implementation.
- If someone says "SHAP-based maths" in this codebase, that is not a documented
  or implemented feature today.
- For explainability or model attribution, there is no concrete code here to
  describe yet.

### Local secret vault / PQC master key

- The PQC cryptography service requires `AEGISCORE_PQC_KEY_BASE64`.
- The configured value must decode to exactly 32 bytes.
- AES-GCM is used with that master key for encrypted secret storage paths.

### Document vault and finance signatures

- Document vault records store SHA-512 hashes and PQC signature metadata.
- Finance artifacts store SHA-256 hashes and PQC signatures.
- Audit and finance flows also use hash chains and signed evidence packages.

## Hashing and Integrity

- Passwords: Argon2id
- Backup integrity and artifact hashing: SHA-256 / SHA-512 depending on the
  subsystem
- Transcript verification: content hash checks
- Profile pictures: SHA-512 for storage integration
- Finance ledger controls: hash chains, audit seals, and signature checks

## Data and Storage

- Primary relational store: PostgreSQL
- Object/document storage: S3-compatible storage, including R2-style document
  vault usage
- Secrets storage: AWS Secrets Manager, Azure Key Vault, or local encrypted
  vault flows depending on deployment path
- Search / indexing support: Elasticsearch client package is present in the
  infrastructure project
- Analytics and reporting data are materialized from backend services rather
  than derived in the portal

## External and Cloud Integrations

- AWS S3
- AWS Secrets Manager
- Azure Document Intelligence
- Azure Identity
- Azure Key Vault Secrets
- Azure Resource Manager for PostgreSQL
- Microsoft Graph
- Google Cloud Secret Manager
- Redis
- SignalR hubs
- SNMP support via SharpSnmpLib

## Domain Services Present

- Finance ledger and payment workflows
- Student attendance and academic progress
- Grade appeals and assessments
- Examinations and transcript verification
- Document vault and retention tiers
- Audit controls and evidence packages
- Transport, cafeteria, and student support workflows
- Elections and governance workflows
- Proctoring, plagiarism detection, and catalog ingestion

## API and Runtime Characteristics

- The API is role-aware and tenant-aware.
- Many endpoints are protected with authorization and policy checks.
- Several flows rely on signed payloads or content hashes for verification.
- The frontend consumes API routes and adapts the workspace by role.

## Deployment Notes

- Portal production is built as a static export and served behind Nginx.
- API and infrastructure services run as .NET services.
- The repository includes Docker and CI-oriented assets, but this document
  only describes the application stack itself.

## Accuracy Notes

This sheet is intentionally strict:

- If a library or algorithm is not present in the repository, it is not listed
  as part of the stack.
- SHAP is not implemented here.
- Where the code uses simulated or compatibility behavior, that is noted above
  instead of being described as a full production implementation.

