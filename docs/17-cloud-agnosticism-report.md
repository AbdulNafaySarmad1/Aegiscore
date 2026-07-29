# Nocturne Cloud-Agnosticism Report

Date: 2026-07-26

## Scope

This audit covers runtime storage, secret, and encryption dependencies for the Nocturne backend.

## Findings

Storage is routed through `IStorageProvider`. The configured providers `local`, `azureblob`, `s3`, and `sftp` now resolve through the same provider contract and pass the same upload/download/list/existence smoke test.

Secrets are routed through `ISecretService` with environment, Azure Key Vault, AWS Secrets Manager, and GCP Secret Manager implementations already present. Deployment configs use environment variables and do not bake secrets into the image.

Encryption key material is no longer hardcoded in the PQC service. `NOCTURNE_PQC_KEY_BASE64` must be a 32-byte base64 value supplied by the selected secret provider or deployment environment.

## Verification

`dotnet test AegisCore.Tests/AegisCore.Tests.csproj -m:1 -p:BuildInParallel=false`

Result: 43 passed, 0 failed.

## Sign-Off Checklist

- [x] Local disk backend selected by config
- [x] Azure Blob backend selected by config
- [x] S3 backend selected by config
- [x] SFTP backend selected by config
- [x] Storage path traversal blocked
- [x] PQC encryption requires external key material
- [x] Docker env template documents required secrets
- [x] No cloud CLI is installed in the API image

## Limits

Cloud credential integration was not executed in this offline workspace. The verified contract is provider selection and storage behavior through `IStorageProvider`; live Azure/S3/SFTP credentials should be smoke-tested in the target university environment.
