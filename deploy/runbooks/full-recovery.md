# Disaster Recovery Runbook — Full Server Failure

**Scenario**: Complete server loss (hardware failure, provider outage, catastrophic event).
**Severity**: 🔴 Critical
**RTO**: 25 minutes
**RPO**: 5 minutes (data loss acceptable)

## Pre-Requisites
- ✅ S3 backup bucket with latest database dump
- ✅ Docker images in GHCR
- ✅ Terraform/Ansible config or DigitalOcean snapshot
- ✅ DNS managed in Cloudflare (quick A-record update)
- ✅ SSL certificates auto-provisioned via Caddy (Let's Encrypt)

## Recovery

### Step 1 — Provision new server (5 minutes)

**DigitalOcean**:
```bash
doctl compute droplet create aegiscore-prod \
    --image ubuntu-24-04-x64 \
    --size s-4vcpu-8gb \
    --region blr1 \
    --ssh-keys "your-fingerprint" \
    --tag-name aegiscore
```

**Azure**:
```bash
az vm create \
    --resource-group aegiscore-prod \
    --name aegiscore-prod \
    --image Ubuntu2204 \
    --size Standard_D4s_v5 \
    --admin-username deploy \
    --ssh-key-values ~/.ssh/aegiscore.pub
```

### Step 2 — Install dependencies (3 minutes)
```bash
# SSH to new server
ssh deploy@<new-ip>

# Install Docker
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker deploy

# Login to GHCR
echo "$GITHUB_TOKEN" | docker login ghcr.io -u aegiscore-bot --password-stdin

# Clone or rsync deploy directory
mkdir -p /opt/aegiscore
cd /opt/aegiscore
git clone https://github.com/nocturnesystems/aegiscore.git .
```

### Step 3 — Restore database (5 minutes)
```bash
# Start only PostgreSQL
cd /opt/aegiscore/deploy
docker compose -f docker-compose.prod.yml up -d postgres
sleep 15

# Download latest backup from S3
LATEST_BACKUP=$(aws s3 ls s3://aegiscore-backups/ --recursive | sort | tail -1 | awk '{print $4}')
aws s3 cp "s3://aegiscore-backups/$LATEST_BACKUP" /tmp/restore.sql.gz

# Restore
gunzip -c /tmp/restore.sql.gz | docker exec -i aegiscore-postgres psql -U aegiscore -d aegiscore
```

### Step 4 — Start application stack (5 minutes)
```bash
cd /opt/aegiscore/deploy

# Set required environment variables
export POSTGRES_PASSWORD="<from-secrets-manager>"
export REDIS_PASSWORD="<from-secrets-manager>"
export AEGISCORE_JWT_SECRET="<from-secrets-manager>"
export LICENSE_HMAC_SECRET="<from-secrets-manager>"
export TENANT_ID="<from-secrets-manager>"
# ... (all other required env vars)

# Start everything
docker compose -f docker-compose.prod.yml up -d --wait
```

### Step 5 — Update DNS (2 minutes)
```bash
# Point domain to new server IP
curl -X PUT "https://api.cloudflare.com/client/v4/zones/$CF_ZONE_ID/dns_records/$DNS_RECORD_ID" \
    -H "Authorization: Bearer $CF_API_TOKEN" \
    -H "Content-Type: application/json" \
    -d "{\"type\":\"A\",\"name\":\"aegiscore.pk\",\"content\":\"$NEW_SERVER_IP\",\"ttl\":120,\"proxied\":true}"
```

### Step 6 — Verify (5 minutes)
```bash
# Wait for DNS propagation
sleep 60

# Verify health
curl -fsS https://aegiscore.pk/health/ready
curl -fsS https://aegiscore.pk/api/wizard/demo-status

# Verify data
docker exec aegiscore-postgres psql -U aegiscore -d aegiscore -c "SELECT count(*) FROM \"Users\";"
docker exec aegiscore-postgres psql -U aegiscore -d aegiscore -c "SELECT count(*) FROM \"StudentProfiles\";"

# Verify SSO
curl -fsS https://aegiscore.pk/api/auth/login -H "Content-Type: application/json" -d '{"email":"demo-admin@aegiscore.local","password":"test123"}'
```

### Rollback If Failure
- Keep old server running for 24 hours
- Point DNS back to old server IP
- Investigate failure, fix, repeat recovery
