# AegisCore on Amazon Web Services (AWS)
>
> Step-by-step guide for deploying AegisCore to AWS using ECS Fargate, RDS PostgreSQL, ElastiCache Redis, and AWS Secrets Manager.

## Prerequisites

- AWS account with IAM permissions for ECS, RDS, ElastiCache, Secrets Manager, EC2
- [AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html) configured
- Docker images pushed to GHCR (see [release workflow](../../.github/workflows/release.yml))

---

## 1. Networking — VPC and Security Groups

```bash
VPC_ID=$(aws ec2 create-vpc --cidr-block 10.0.0.0/16 --query Vpc.VpcId --output text)
aws ec2 create-tags --resources $VPC_ID --tags Key=Name,Value=aegiscore-vpc

# Public subnets (for load balancer)
PUBLIC_SUBNET_1=$(aws ec2 create-subnet --vpc-id $VPC_ID --cidr-block 10.0.1.0/24 --availability-zone us-east-1a --query Subnet.SubnetId --output text)
PUBLIC_SUBNET_2=$(aws ec2 create-subnet --vpc-id $VPC_ID --cidr-block 10.0.2.0/24 --availability-zone us-east-1b --query Subnet.SubnetId --output text)

# Private subnets (for RDS, ElastiCache, ECS tasks)
PRIVATE_SUBNET_1=$(aws ec2 create-subnet --vpc-id $VPC_ID --cidr-block 10.0.10.0/24 --availability-zone us-east-1a --query Subnet.SubnetId --output text)
PRIVATE_SUBNET_2=$(aws ec2 create-subnet --vpc-id $VPC_ID --cidr-block 10.0.11.0/24 --availability-zone us-east-1b --query Subnet.SubnetId --output text)

# Internet Gateway
IGW_ID=$(aws ec2 create-internet-gateway --query InternetGateway.InternetGatewayId --output text)
aws ec2 attach-internet-gateway --internet-gateway-id $IGW_ID --vpc-id $VPC_ID
```

### Security Groups

```bash
# RDS security group
DB_SG=$(aws ec2 create-security-group \
  --group-name aegiscore-db-sg \
  --description "PostgreSQL access" \
  --vpc-id $VPC_ID \
  --query GroupId --output text)

aws ec2 authorize-security-group-ingress \
  --group-id $DB_SG \
  --protocol tcp --port 5432 \
  --source-group aegiscore-ecs-sg

# Redis security group
REDIS_SG=$(aws ec2 create-security-group \
  --group-name aegiscore-redis-sg \
  --description "Redis access" \
  --vpc-id $VPC_ID \
  --query GroupId --output text)

aws ec2 authorize-security-group-ingress \
  --group-id $REDIS_SG \
  --protocol tcp --port 6379 \
  --source-group aegiscore-ecs-sg
```

---

## 2. RDS PostgreSQL

```bash
aws rds create-db-subnet-group \
  --db-subnet-group-name aegiscore-db-subnet \
  --db-subnet-group-description "AegisCore DB subnets" \
  --subnet-ids "$PRIVATE_SUBNET_1" "$PRIVATE_SUBNET_2"

aws rds create-db-instance \
  --db-instance-identifier aegiscore-db \
  --db-instance-class db.t3.medium \
  --engine postgres \
  --engine-version 17.2 \
  --allocated-storage 50 \
  --storage-type gp3 \
  --master-username aegiscore_admin \
  --master-user-password "$(openssl rand -base64 32)" \
  --db-name aegiscore \
  --vpc-security-group-ids "$DB_SG" \
  --db-subnet-group-name aegiscore-db-subnet \
  --publicly-accessible false \
  --backup-retention-period 14 \
  --multi-az true \
  --no-enable-iam-db-authentication
```

After creation, connect via a bastion and enable pgvector:

```sql
CREATE EXTENSION IF NOT EXISTS vector;
```

Create the application role:

```sql
CREATE ROLE aegiscore LOGIN PASSWORD '<secure-password>'
  NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
GRANT ALL ON DATABASE aegiscore TO aegiscore;
GRANT ALL ON SCHEMA public TO aegiscore;
```

---

## 3. ElastiCache Redis

```bash
aws elasticache create-cache-subnet-group \
  --cache-subnet-group-name aegiscore-redis-subnet \
  --cache-subnet-group-description "Redis subnets" \
  --subnet-ids "$PRIVATE_SUBNET_1" "$PRIVATE_SUBNET_2"

aws elasticache create-replication-group \
  --replication-group-id aegiscore-redis \
  --replication-group-description "AegisCore cache" \
  --engine redis \
  --engine-version 7.2 \
  --cache-node-type cache.t3.micro \
  --num-cache-clusters 1 \
  --automatic-failover-enabled false \
  --cache-subnet-group-name aegiscore-redis-subnet \
  --security-group-ids "$REDIS_SG" \
  --at-rest-encryption-enabled true \
  --transit-encryption-enabled true \
  --auth-token "$(openssl rand -base64 32)"
```

### Connection String

```
aegiscore-redis.xxxxxx.0001.use1.cache.amazonaws.com:6379,password=<auth-token>,ssl=True
```

---

## 4. AWS Secrets Manager

Store all secrets in a single JSON secret:

```bash
aws secretsmanager create-secret \
  --name aegiscore/production \
  --description "AegisCore production secrets" \
  --secret-string "$(cat <<EOF | jq -c .
{
  "AEGISCORE_DB_CONNECTION": "Host=aegiscore-db.xxxxxx.us-east-1.rds.amazonaws.com;Port=5432;Database=aegiscore;Username=aegiscore;Password=<password>;SslMode=Require",
  "AEGISCORE_REDIS_CONNECTION": "aegiscore-redis.xxxxxx.0001.use1.cache.amazonaws.com:6379,password=<auth-token>,ssl=True",
  "AEGISCORE_JWT_SECRET": "$(openssl rand -base64 64)",
  "AEGISCORE_ENCRYPTION_KEY": "$(openssl rand -base64 32)",
  "AEGISCORE_PQC_KEY_BASE64": "$(openssl rand -base64 32)",
  "LICENSE_HMAC_SECRET": "$(openssl rand -base64 64)",
  "GOVERNANCE_FEEDBACK_KEY_BASE64": "$(openssl rand -base64 32)",
  "TENANT_ID": "$(uuidgen)"
}
EOF
)"
```

Create an IAM role for ECS tasks to read secrets:

```bash
aws iam create-role \
  --role-name aegiscore-ecs-task-role \
  --assume-role-policy-document '{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"Service":"ecs-tasks.amazonaws.com"},"Action":"sts:AssumeRole"}]}'

aws iam attach-role-policy \
  --role-name aegiscore-ecs-task-role \
  --policy-arn arn:aws:iam::aws:policy/SecretsManagerReadWrite

# Or a tighter inline policy:
aws iam put-role-policy \
  --role-name aegiscore-ecs-task-role \
  --policy-name aegiscore-secrets \
  --policy-document "$(cat <<EOF
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Action": ["secretsmanager:GetSecretValue"],
    "Resource": "arn:aws:secretsmanager:*:*:secret:aegiscore/production-*"
  }]
}
EOF
)"
```

---

## 5. ECS Fargate

### ECS Cluster

```bash
aws ecs create-cluster --cluster-name aegiscore-cluster
```

### Task Definition — API

Create `api-task-def.json`:

```json
{
  "family": "aegiscore-api",
  "networkMode": "awsvpc",
  "executionRoleArn": "arn:aws:iam::<account-id>:role/ecsTaskExecutionRole",
  "taskRoleArn": "arn:aws:iam::<account-id>:role/aegiscore-ecs-task-role",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "1024",
  "memory": "2048",
  "containerDefinitions": [{
    "name": "api",
    "image": "ghcr.io/<owner>/aegiscore-api:latest",
    "portMappings": [{"containerPort": 8080}],
    "environment": [
      {"name": "ASPNETCORE_ENVIRONMENT", "value": "Production"},
      {"name": "AEGISCORE_CLOUD_PROVIDER", "value": "AWS"},
      {"name": "AEGISCORE_SECRET_PROVIDER", "value": "AWS_SECRETS_MANAGER"},
      {"name": "AEGISCORE_STORAGE_PROVIDER", "value": "s3"},
      {"name": "AWS_SECRET_ARN", "value": "arn:aws:secretsmanager:us-east-1:<account-id>:secret:aegiscore/production"}
    ],
    "healthCheck": {
      "command": ["CMD-SHELL", "curl -f http://localhost:8080/health/live || exit 1"],
      "interval": 30,
      "timeout": 5,
      "retries": 3,
      "startPeriod": 60
    },
    "logConfiguration": {
      "logDriver": "awslogs",
      "options": {
        "awslogs-group": "/ecs/aegiscore-api",
        "awslogs-region": "us-east-1",
        "awslogs-stream-prefix": "api"
      }
    }
  }]
}
```

Register and run:

```bash
aws ecs register-task-definition --cli-input-json file://api-task-def.json

aws ecs create-service \
  --cluster aegiscore-cluster \
  --service-name aegiscore-api \
  --task-definition aegiscore-api \
  --desired-count 2 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[$PRIVATE_SUBNET_1,$PRIVATE_SUBNET_2],securityGroups=[aegiscore-ecs-sg]}"
```

---

## 6. Alternative: EC2 + Docker Compose

For institutions preferring EC2 over Fargate:

```bash
# Launch an EC2 instance (Ubuntu 24 LTS, t3.large minimum)
# SSH in and install Docker:

sudo apt update && sudo apt install -y docker.io docker-compose-v2
sudo usermod -aG docker ubuntu
```

Create `/opt/aegiscore/.env` (see [env vars reference](#7-required-environment-variables)), then:

```bash
cd /opt/aegiscore
docker compose --env-file .env up -d --build --wait
```

---

## 7. Required Environment Variables

| Variable                                  | Required | Description                                      | Example                                      |
|-------------------------------------------|----------|--------------------------------------------------|----------------------------------------------|
| `ASPNETCORE_ENVIRONMENT`                  | Yes      | Runtime environment                              | `Production`                                 |
| `AEGISCORE_CLOUD_PROVIDER`                | Yes      | Cloud provider identifier                        | `AWS`                                        |
| `AEGISCORE_SECRET_PROVIDER`               | Yes      | Secret backend selection                         | `AWS_SECRETS_MANAGER`                        |
| `AEGISCORE_DB_CONNECTION`                 | Yes      | PostgreSQL connection string                     | `Host=...;Port=5432;Database=...;SslMode=Require` |
| `AEGISCORE_REDIS_CONNECTION`              | Yes      | Redis connection string                          | `host:6379,password=...,ssl=True`            |
| `AEGISCORE_JWT_SECRET`                    | Yes      | JWT signing key                                  | 64-char base64                               |
| `AEGISCORE_ENCRYPTION_KEY`                | Yes      | AES encryption key                               | 32-byte base64                               |
| `AEGISCORE_PQC_KEY_BASE64`                | Yes      | Post-quantum crypto key                          | 32-byte base64                               |
| `LICENSE_HMAC_SECRET`                     | Yes      | License HMAC signing key                         | 64-char base64                               |
| `GOVERNANCE_FEEDBACK_KEY_BASE64`          | Yes      | Governance feedback encryption key               | 32-byte base64                               |
| `TENANT_ID`                               | Yes      | Tenant identifier                                | UUID                                         |
| `AEGISCORE_STORAGE_PROVIDER`              | No       | Storage backend                                  | `s3` / `local`                               |
| `AWS_SECRET_ARN`                          | No*      | ARN of Secrets Manager secret (*if using AWS SM) | `arn:aws:secretsmanager:...`                 |
| `Database__AutoMigrate`                   | No       | Run EF migrations on startup                     | `true` / `false`                             |

---

## 8. Troubleshooting

### RDS Connection Refused

- Verify the ECS task security group is in the RDS security group's inbound rules for port 5432.
- Confirm RDS is in the same VPC private subnets as ECS tasks.
- Check `PubliclyAccessible` is `false` for production and a bastion/NAT is configured.

### ElastiCache Timeouts

- ElastiCache is VPC-only. Ensure tasks are in the same VPC.
- Check the security group allows inbound TCP 6379 from the ECS task security group.
- Verify `transit-encryption-enabled: true` and the connection string uses `ssl=True`.

### Secrets Manager Access Denied

- Confirm the ECS task role (`taskRoleArn`) has `secretsmanager:GetSecretValue` for the secret ARN.
- Verify the IAM trust policy allows `ecs-tasks.amazonaws.com` to assume the role.

### ECS Tasks Stuck in PENDING

- Check CloudWatch logs for the task.
- Verify the ECS task execution role has `ecr:GetAuthorizationToken` (for GHCR, this must be configured separately with GHCR credentials).
- For GHCR images, store Docker Hub/GHCR credentials in AWS Secrets Manager and reference them in the task definition.
