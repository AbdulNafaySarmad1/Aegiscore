# AegisCore on Microsoft Azure
>
> Step-by-step guide for deploying AegisCore to Azure using Container Apps, Azure Database for PostgreSQL Flexible Server, Azure Cache for Redis, and Azure Key Vault.

## Prerequisites

- Azure subscription with Contributor role
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) installed locally
- Docker images pushed to GHCR (see [release workflow](../../.github/workflows/release.yml))

---

## 1. Resource Group

```bash
az group create \
  --name aegiscore-prod \
  --location westeurope
```

---

## 2. Azure Database for PostgreSQL Flexible Server

```bash
az postgres flexible-server create \
  --resource-group aegiscore-prod \
  --name aegiscore-db \
  --admin-user aegiscore_admin \
  --admin-password "$(openssl rand -base64 32)" \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 64 \
  --version 17 \
  --database-name aegiscore \
  --high-availability Disabled \
  --public-access 0.0.0.0
```

Enable the **pgvector** extension:

```bash
az postgres flexible-server parameter set \
  --resource-group aegiscore-prod \
  --server-name aegiscore-db \
  --name azure.extensions \
  --value pgvector
```

Create the application database user (least privilege):

```sql
CREATE ROLE aegiscore LOGIN PASSWORD '<secure-password>'
  NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
GRANT ALL ON DATABASE aegiscore TO aegiscore;
GRANT ALL ON SCHEMA public TO aegiscore;
```

### Connection String

```
Host=aegiscore-db.postgres.database.azure.com;Port=5432;Database=aegiscore;Username=aegiscore;Password=<password>;SslMode=Require
```

---

## 3. Azure Cache for Redis

```bash
az redis create \
  --resource-group aegiscore-prod \
  --name aegiscore-redis \
  --location westeurope \
  --sku Basic \
  --vm-size C0 \
  --enable-non-ssl-port false \
  --minimum-tls-version 1.2
```

Retrieve the access key:

```bash
az redis list-keys --resource-group aegiscore-prod --name aegiscore-redis --query primaryKey -o tsv
```

### Connection String

```
aegiscore-redis.redis.cache.windows.net:6380,password=<key>,ssl=True
```

---

## 4. Azure Key Vault

```bash
az keyvault create \
  --resource-group aegiscore-prod \
  --name aegiscore-kv \
  --location westeurope \
  --enable-rbac-authorization true
```

Add secrets:

```bash
az keyvault secret set --vault-name aegiscore-kv --name "AegiscoreDbConnection"   --value "<connection-string>"
az keyvault secret set --vault-name aegiscore-kv --name "AegiscoreRedisConnection" --value "<redis-connection-string>"
az keyvault secret set --vault-name aegiscore-kv --name "AegiscoreJwtSecret"       --value "$(openssl rand -base64 64)"
az keyvault secret set --vault-name aegiscore-kv --name "AegiscoreEncryptionKey"   --value "$(openssl rand -base64 32)"
az keyvault secret set --vault-name aegiscore-kv --name "PqcKeyBase64"             --value "$(openssl rand -base64 32)"
az keyvault secret set --vault-name aegiscore-kv --name "LicenseHmacSecret"        --value "$(openssl rand -base64 64)"
az keyvault secret set --vault-name aegiscore-kv --name "GovernanceFeedbackKey"    --value "$(openssl rand -base64 32)"
```

Grant the Container App's managed identity access (RBAC):

```bash
IDENTITY_PRINCIPAL_ID=$(az identity show \
  --resource-group aegiscore-prod \
  --name aegiscore-identity \
  --query principalId -o tsv)

az role assignment create \
  --assignee "$IDENTITY_PRINCIPAL_ID" \
  --role "Key Vault Secrets User" \
  --scope "$(az keyvault show --name aegiscore-kv --query id -o tsv)"
```

---

## 5. Azure Container Apps Environment

```bash
az containerapp env create \
  --resource-group aegiscore-prod \
  --name aegiscore-env \
  --location westeurope
```

Create a User-Assigned Managed Identity for the Container App:

```bash
az identity create \
  --resource-group aegiscore-prod \
  --name aegiscore-identity
```

---

## 6. Deploy Container Apps

### API Container

```bash
az containerapp create \
  --resource-group aegiscore-prod \
  --environment aegiscore-env \
  --name aegiscore-api \
  --image "ghcr.io/<owner>/aegiscore-api:latest" \
  --target-port 8080 \
  --ingress internal \
  --cpu 1.0 --memory 2.0Gi \
  --min-replicas 1 --max-replicas 3 \
  --user-assigned aegiscore-identity \
  --secrets \
    "db-conn=keyvaultref:https://aegiscore-kv.vault.azure.net/secrets/AegiscoreDbConnection,identityref:$(az identity show -g aegiscore-prod -n aegiscore-identity --query id -o tsv)" \
    "redis-conn=keyvaultref:https://aegiscore-kv.vault.azure.net/secrets/AegiscoreRedisConnection,identityref:$(az identity show -g aegiscore-prod -n aegiscore-identity --query id -o tsv)" \
  --env-vars \
    "ASPNETCORE_URLS=http://+:8080" \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "AEGISCORE_CLOUD_PROVIDER=AZURE" \
    "AEGISCORE_SECRET_PROVIDER=AZURE_KEY_VAULT" \
    "AEGISCORE_STORAGE_PROVIDER=azureblob" \
    "AEGISCORE_DB_CONNECTION=secretref:db-conn" \
    "AEGISCORE_REDIS_CONNECTION=secretref:redis-conn"
```

### Portal Container

```bash
az containerapp create \
  --resource-group aegiscore-prod \
  --environment aegiscore-env \
  --name aegiscore-portal \
  --image "ghcr.io/<owner>/aegiscore-portal:latest" \
  --target-port 8080 \
  --ingress external \
  --cpu 0.5 --memory 1.0Gi \
  --min-replicas 1 --max-replicas 3 \
  --env-vars \
    "NODE_ENV=production" \
    "PORT=8080" \
    "HOSTNAME=0.0.0.0" \
    "AEGISCORE_API_INTERNAL_URL=http://aegiscore-api:8080"
```

---

## 7. Required Environment Variables

| Variable                                  | Required | Description                                      | Example                                      |
|-------------------------------------------|----------|--------------------------------------------------|----------------------------------------------|
| `ASPNETCORE_ENVIRONMENT`                  | Yes      | Runtime environment                              | `Production`                                 |
| `AEGISCORE_CLOUD_PROVIDER`                | Yes      | Cloud provider identifier                        | `AZURE`                                      |
| `AEGISCORE_SECRET_PROVIDER`               | Yes      | Secret backend selection                         | `AZURE_KEY_VAULT`                            |
| `AEGISCORE_DB_CONNECTION`                 | Yes      | PostgreSQL connection string                     | `Host=...;Port=5432;Database=...;SslMode=Require` |
| `AEGISCORE_REDIS_CONNECTION`              | Yes      | Redis connection string                          | `host:6380,password=...,ssl=True`            |
| `AEGISCORE_JWT_SECRET`                    | Yes      | JWT signing key                                  | 64-char base64                               |
| `AEGISCORE_ENCRYPTION_KEY`                | Yes      | AES encryption key                               | 32-byte base64                               |
| `AEGISCORE_PQC_KEY_BASE64`                | Yes      | Post-quantum crypto key                          | 32-byte base64                               |
| `LICENSE_HMAC_SECRET`                     | Yes      | License HMAC signing key                         | 64-char base64                               |
| `GOVERNANCE_FEEDBACK_KEY_BASE64`          | Yes      | Governance feedback encryption key               | 32-byte base64                               |
| `TENANT_ID`                               | Yes      | Tenant identifier                                | UUID                                         |
| `AEGISCORE_STORAGE_PROVIDER`              | Yes      | Storage backend                                  | `azureblob`                                  |
| `AEGISCORE_STORAGE_LOCAL_PATH`            | No       | Local storage path (if provider=local)           | `/app/storage`                               |
| `Database__AutoMigrate`                   | No       | Run EF migrations on startup                     | `true` / `false`                             |
| `AEGISCORE_API_INTERNAL_URL`              | Yes*     | Portal → API internal URL (*portal container)    | `http://aegiscore-api:8080`                  |
| `AEGISCORE_DEMO_MODE`                     | No       | Demo mode (must be false in production)          | `false`                                      |

---

## 8. Troubleshooting

### Connection Refused to PostgreSQL

- Verify the PostgreSQL Flexible Server firewall rules allow your Container App's outbound IP.
- Add the Container App environment's static outbound IP to the PostgreSQL firewall.
- Ensure `SslMode=Require` is set in the connection string.

### SSL/TLS Issues

- Azure Database for PostgreSQL Flexible Server enforces TLS 1.2+.
- Set `SslMode=Require` (not `VerifyFull` unless you provide a CA cert).
- For Redis, use port `6380` with `ssl=True`.

### Firewall / Networking

- Container App ingress can be `internal` (VNet only) or `external` (internet-facing).
- PostgreSQL must be reachable from the Container App environment's subnet.
- Use VNet integration if you require private network connectivity.

### Key Vault Access Denied

- Confirm the managed identity has the `Key Vault Secrets User` role.
- Check the Key Vault networking firewall allows Azure services.
- Verify secret names match what the application expects (case-sensitive).
