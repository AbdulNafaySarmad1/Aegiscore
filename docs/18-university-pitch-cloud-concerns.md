# AegisCore Cloud Concerns Pitch

## Position

AegisCore does not force a university into one cloud. The same backend can run on local servers, Kubernetes, Azure, AWS-compatible S3/MinIO, or a mixed setup.

## Architecture

```
Portal / Mobile Apps
        |
      HTTPS
        |
AegisCore API on Docker/Kubernetes
        |
 +------+---------+----------+
 |                |          |
PostgreSQL      Redis    IStorageProvider
                         |
       local disk / Azure Blob / S3 / SFTP
```

## Operational Assurances

- Data residency can stay on campus with PostgreSQL, Redis, and local or MinIO storage.
- Secrets are supplied at runtime through environment variables or a vault provider.
- Storage is behind a provider contract, so universities can move from on-prem to cloud without changing academic modules.
- The Docker image contains application runtime only, not cloud-specific deployment tooling.

## Buyer Questions

Can we run without Azure?

Yes. Use Docker Compose or on-prem Kubernetes with PostgreSQL, Redis, and local or MinIO-compatible storage.

Can we migrate later?

Yes. Keep database backups and object storage keys stable, then switch provider configuration during a maintenance window.

Are secrets in the image?

No. The image expects runtime environment variables or vault-backed configuration.
