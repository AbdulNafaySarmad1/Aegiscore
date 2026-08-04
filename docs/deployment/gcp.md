# AegisCore on Google Cloud Platform (GCP)
>
> Step-by-step guide for deploying AegisCore to GCP using Cloud Run, Cloud SQL PostgreSQL, Memorystore Redis, and Secret Manager.

## Prerequisites

- GCP project with billing enabled
- [gcloud CLI](https://cloud.google.com/sdk/docs/install) installed and configured
- APIs enabled: Cloud SQL, Memorystore, Secret Manager, Cloud Run, Artifact Registry, Compute Engine
- Docker images pushed to GHCR (see [release workflow](../../.github/workflows/release.yml))

Enable required APIs:

```bash
gcloud services enable \
  sqladmin.googleapis.com \
  redis.googleapis.com \
  secretmanager.googleapis.com \
  run.googleapis.com \
  artifactregistry.googleapis.com \
  compute.googleapis.com
```

---

## 1. Cloud SQL PostgreSQL

Create a Cloud SQL for PostgreSQL instance with private IP:

```bash
# Reserve a private IP range for VPC peering
gcloud compute addresses create google-managed-services-default \
  --global \
  --purpose VPC_PEERING \
  --prefix-length 20 \
  --network default

gcloud services vpc-peerings connect \
  --service servicenetworking.googleapis.com \
  --ranges google-managed-services-default \
  --network default

gcloud sql instances create aegiscore-db \
  --database-version POSTGRES_17 \
  --tier db-custom-2-4096 \
  --region us-central1 \
  --storage-size 50 \
  --storage-type SSD \
  --backup-start-time 03:00 \
  --availability-type REGIONAL \
  --network default \
  --no-assign-ip \
  --database-flags cloudsql.enable_pgvector=on
```

Set the admin password and create the application database:

```bash
gcloud sql users set-password postgres --instance aegiscore-db --password "$(openssl rand -base64 32)"

gcloud sql databases create aegiscore --instance aegiscore-db
```

Connect via Cloud SQL Auth proxy (or a bastion VM) and create the app role:

```sql
CREATE ROLE aegiscore LOGIN PASSWORD '<secure-password>'
  NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
GRANT ALL ON DATABASE aegiscore TO aegiscore;
GRANT ALL ON SCHEMA public TO aegiscore;
```

### Connection String (via Private IP / Auth Proxy)

```
Host=/cloudsql/<project>:us-central1:aegiscore-db;Port=5432;Database=aegiscore;Username=aegiscore;Password=<password>;SslMode=Require
```

---

## 2. Memorystore Redis

```bash
gcloud redis instances create aegiscore-redis \
  --size 1 \
  --region us-central1 \
  --zone us-central1-a \
  --redis-version redis_7_2 \
  --network default \
  --connect-mode PRIVATE_SERVICE_ACCESS \
  --redis-config maxmemory-policy=allkeys-lru \
  --tier BASIC \
  --auth-mode AUTH_ONLY_ENABLED

# Retrieve the auth string
gcloud redis instances get-auth-string aegiscore-redis --region us-central1
```

### Connection String

```
10.x.x.x:6379,password=<auth-string>,ssl=True
```

---

## 3. Secret Manager

```bash
gcloud secrets create aegiscore-db-conn --replication-policy automatic
gcloud secrets create aegiscore-redis-conn --replication-policy automatic
gcloud secrets create aegiscore-jwt-secret --replication-policy automatic
gcloud secrets create aegiscore-encryption-key --replication-policy automatic
gcloud secrets create aegiscore-pqc-key --replication-policy automatic
gcloud secrets create license-hmac-secret --replication-policy automatic
gcloud secrets create governance-feedback-key --replication-policy automatic
gcloud secrets create tenant-id --replication-policy automatic

# Add versions
printf "<connection-string>" | gcloud secrets versions add aegiscore-db-conn --data-file=-
printf "<redis-string>" | gcloud secrets versions add aegiscore-redis-conn --data-file=-
openssl rand -base64 64 | gcloud secrets versions add aegiscore-jwt-secret --data-file=-
openssl rand -base64 32 | gcloud secrets versions add aegiscore-encryption-key --data-file=-
openssl rand -base64 32 | gcloud secrets versions add aegiscore-pqc-key --data-file=-
openssl rand -base64 64 | gcloud secrets versions add license-hmac-secret --data-file=-
openssl rand -base64 32 | gcloud secrets versions add governance-feedback-key --data-file=-
uuidgen | gcloud secrets versions add tenant-id --data-file=-
```

Grant the Cloud Run service account access to secrets:

```bash
SERVICE_ACCOUNT="aegiscore-sa@${GOOGLE_CLOUD_PROJECT}.iam.gserviceaccount.com"

gcloud secrets add-iam-policy-binding aegiscore-db-conn \
  --member "serviceAccount:$SERVICE_ACCOUNT" \
  --role roles/secretmanager.secretAccessor

gcloud secrets add-iam-policy-binding aegiscore-redis-conn \
  --member "serviceAccount:$SERVICE_ACCOUNT" \
  --role roles/secretmanager.secretAccessor

# Repeat for remaining secrets...
```

---

## 4. Deploy to Cloud Run

### API Service

```bash
gcloud run deploy aegiscore-api \
  --image "ghcr.io/<owner>/aegiscore-api:latest" \
  --region us-central1 \
  --platform managed \
  --port 8080 \
  --cpu 1 --memory 2Gi \
  --min-instances 1 --max-instances 5 \
  --concurrency 80 \
  --vpc-connector projects/<project>/locations/us-central1/connectors/aegiscore-vpc \
  --service-account aegiscore-sa@<project>.iam.gserviceaccount.com \
  --set-cloudsql-instances <project>:us-central1:aegiscore-db \
  --set-env-vars "\
ASPNETCORE_ENVIRONMENT=Production,\
AEGISCORE_CLOUD_PROVIDER=GCP,\
AEGISCORE_SECRET_PROVIDER=GCP_SECRET_MANAGER,\
AEGISCORE_STORAGE_PROVIDER=s3,\
GCP_PROJECT_ID=<project>,\
AEGISCORE_DB_CONNECTION_SECRET=aegiscore-db-conn,\
AEGISCORE_REDIS_CONNECTION_SECRET=aegiscore-redis-conn,\
AEGISCORE_JWT_SECRET_NAME=aegiscore-jwt-secret,\
AEGISCORE_ENCRYPTION_KEY_NAME=aegiscore-encryption-key,\
AEGISCORE_PQC_KEY_NAME=aegiscore-pqc-key,\
LICENSE_HMAC_SECRET_NAME=license-hmac-secret,\
GOVERNANCE_FEEDBACK_KEY_NAME=governance-feedback-key,\
TENANT_ID_SECRET_NAME=tenant-id"
```

### Portal Service

```bash
gcloud run deploy aegiscore-portal \
  --image "ghcr.io/<owner>/aegiscore-portal:latest" \
  --region us-central1 \
  --platform managed \
  --port 8080 \
  --cpu 1 --memory 1Gi \
  --min-instances 1 --max-instances 5 \
  --concurrency 80 \
  --set-env-vars "\
NODE_ENV=production,\
PORT=8080,\
HOSTNAME=0.0.0.0,\
AEGISCORE_API_INTERNAL_URL=https://aegiscore-api-xxxxxx-uc.a.run.app"
```

---

## 5. Alternative: GKE (Google Kubernetes Engine)

For advanced deployments requiring custom scheduling or sidecar containers:

```bash
gcloud container clusters create aegiscore-cluster \
  --region us-central1 \
  --num-nodes 3 \
  --machine-type e2-standard-2 \
  --enable-private-nodes \
  --enable-ip-alias

kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/ --recursive
```

---

## 6. Required Environment Variables

| Variable                                  | Required | Description                                      | Example                                      |
|-------------------------------------------|----------|--------------------------------------------------|----------------------------------------------|
| `ASPNETCORE_ENVIRONMENT`                  | Yes      | Runtime environment                              | `Production`                                 |
| `AEGISCORE_CLOUD_PROVIDER`                | Yes      | Cloud provider identifier                        | `GCP`                                        |
| `AEGISCORE_SECRET_PROVIDER`               | Yes      | Secret backend selection                         | `GCP_SECRET_MANAGER`                         |
| `AEGISCORE_DB_CONNECTION`                 | Yes      | PostgreSQL connection string                     | `Host=/cloudsql/...;SslMode=Require`         |
| `AEGISCORE_REDIS_CONNECTION`              | Yes      | Redis connection string                          | `host:6379,password=...,ssl=True`            |
| `AEGISCORE_JWT_SECRET`                    | Yes      | JWT signing key                                  | 64-char base64                               |
| `AEGISCORE_ENCRYPTION_KEY`                | Yes      | AES encryption key                               | 32-byte base64                               |
| `AEGISCORE_PQC_KEY_BASE64`                | Yes      | Post-quantum crypto key                          | 32-byte base64                               |
| `LICENSE_HMAC_SECRET`                     | Yes      | License HMAC signing key                         | 64-char base64                               |
| `GOVERNANCE_FEEDBACK_KEY_BASE64`          | Yes      | Governance feedback encryption key               | 32-byte base64                               |
| `TENANT_ID`                               | Yes      | Tenant identifier                                | UUID                                         |
| `GCP_PROJECT_ID`                          | No*      | GCP project ID (*if using GCP Secret Manager)    | `my-project`                                 |
| `AEGISCORE_STORAGE_PROVIDER`              | No       | Storage backend                                  | `s3` / `local` / `gcs`                       |
| `Database__AutoMigrate`                   | No       | Run EF migrations on startup                     | `true` / `false`                             |

---

## 7. Troubleshooting

### Cloud SQL Connection Refused

- Verify the Cloud Run service has the `--set-cloudsql-instances` flag set correctly.
- Ensure the Cloud SQL instance is in the same region as Cloud Run.
- Check the service account has `roles/cloudsql.client` IAM role.
- For VPC-native (private IP) access, configure a Serverless VPC Access connector and pass `--vpc-connector`.

### Private IP Connectivity

- Cloud SQL instances with `--no-assign-ip` require private services access and a VPC connector.
- The Serverless VPC Access connector must be in the same region as the Cloud Run service.
- Memorystore Redis `PRIVATE_SERVICE_ACCESS` mode requires the same VPC configuration.

### Secret Manager Access Denied

- Verify the Cloud Run service account has `roles/secretmanager.secretAccessor` on each secret.
- Check secret names in env vars match the actual secret names (case-sensitive).
- Ensure the secret exists and has at least one active version.

### Cold Start Latency

- Set `--min-instances 1` to keep at least one instance warm (incurs cost).
- Increase `--cpu` and `--memory` for faster startup.
- Use `--concurrency 80` and tune based on load testing.

### Image Pull Errors

- Cloud Run pulls from GHCR just like any registry. Ensure the image is public or configure GHCR credentials.
- Use `gcloud run deploy --image ghcr.io/...` directly without Artifact Registry if pulling from GHCR.
