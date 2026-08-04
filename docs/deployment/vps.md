# AegisCore on a Bare Linux VPS
>
> Step-by-step guide for deploying AegisCore on a commodity VPS (Hetzner, DigitalOcean, Linode, Vultr, etc.) running Ubuntu 24 LTS.

## Prerequisites

- Ubuntu 24 LTS server with at least 4 vCPUs, 8 GB RAM, 50 GB SSD
- A domain name pointing to the server's public IP (e.g. `aegiscore.university.edu.pk`)
- SSH access with sudo privileges

---

## 1. Initial Server Setup

```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Set timezone
sudo timedatectl set-timezone Asia/Karachi

# Create application directory
sudo mkdir -p /opt/aegiscore
sudo chown $USER:$USER /opt/aegiscore
```

---

## 2. Install Docker & Docker Compose

```bash
# Docker
curl -fsSL https://get.docker.com -o /tmp/get-docker.sh
sudo sh /tmp/get-docker.sh
sudo usermod -aG docker $USER
newgrp docker

# Docker Compose (v2, bundled with Docker)
docker compose version

# Verify
docker run --rm hello-world
```

---

## 3. Configure UFW Firewall

```bash
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 22/tcp    comment "SSH"
sudo ufw allow 80/tcp    comment "HTTP"
sudo ufw allow 443/tcp   comment "HTTPS"
sudo ufw enable
sudo ufw status verbose
```

---

## 4. Clone and Configure AegisCore

```bash
cd /opt/aegiscore
git clone https://github.com/<owner>/Aegiscore.git .

# Generate secrets
./scripts/bootstrap-env.sh

# Edit .env with production values
vim .env
```

### Key .env Values for Production

```ini
ASPNETCORE_ENVIRONMENT=Production
AEGISCORE_DEMO_MODE=false
AEGISCORE_SECRET_PROVIDER=FILE
AEGISCORE_INSTALLATION_COMPLETED=true
AEGISCORE_CLOUD_PROVIDER=ONPREMISE
AEGISCORE_DB_CONNECTION=Host=postgres;Port=5432;Database=aegiscore;Username=aegiscore;Password=<password>;SslMode=Disable
AEGISCORE_REDIS_CONNECTION=redis:6379,password=<password>
AEGISCORE_STORAGE_PROVIDER=local
AEGISCORE_STORAGE_LOCAL_PATH=/app/storage
TENANT_ID=<uuid>
AEGISCORE_JWT_SECRET=<64-char base64>
AEGISCORE_ENCRYPTION_KEY=<32-byte base64>
AEGISCORE_PQC_KEY_BASE64=<32-byte base64>
LICENSE_HMAC_SECRET=<64-char base64>
GOVERNANCE_FEEDBACK_KEY_BASE64=<32-byte base64>
POSTGRES_PASSWORD=<strong-password>
REDIS_PASSWORD=<strong-password>
AEGISCORE_PORT=8080
```

---

## 5. Nginx Reverse Proxy

Create `/etc/nginx/sites-available/aegiscore`:

```nginx
server {
    listen 80;
    server_name aegiscore.university.edu.pk;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name aegiscore.university.edu.pk;

    ssl_certificate     /etc/letsencrypt/live/aegiscore.university.edu.pk/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/aegiscore.university.edu.pk/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    client_max_body_size 100M;

    # Portal (Next.js)
    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 86400;
    }

    # API
    location /api {
        rewrite ^/api/(.*) /$1 break;
        proxy_pass http://127.0.0.1:8081;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # SignalR / WebSocket (real-time notifications)
    location /hubs {
        proxy_pass http://127.0.0.1:8081;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Enable the site:

```bash
sudo ln -s /etc/nginx/sites-available/aegiscore /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx
```

---

## 6. SSL via Certbot (Let's Encrypt)

```bash
sudo apt install -y certbot python3-certbot-nginx

# Dry run first
sudo certbot certonly --nginx -d aegiscore.university.edu.pk --dry-run

# Obtain certificate
sudo certbot --nginx -d aegiscore.university.edu.pk --non-interactive --agree-tos -m admin@university.edu.pk

# Auto-renewal is enabled by default. Verify:
sudo systemctl status certbot.timer
```

---

## 7. Systemd Service for Auto-Restart

Create `/etc/systemd/system/aegiscore.service`:

```ini
[Unit]
Description=AegisCore Docker Compose Stack
Requires=docker.service
After=docker.service network-online.target

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=/opt/aegiscore
ExecStart=/usr/bin/docker compose --env-file .env up -d --wait
ExecStop=/usr/bin/docker compose --env-file .env down
ExecReload=/usr/bin/docker compose --env-file .env restart
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable aegiscore
sudo systemctl start aegiscore
sudo systemctl status aegiscore
```

---

## 8. Deploy Application

```bash
cd /opt/aegiscore

# Pull prebuilt images (recommended for production)
docker compose --env-file .env pull

# Or build from source
docker compose --env-file .env up -d --build --wait

# Verify
curl -s http://localhost:8080/health/ready | python3 -m json.tool
docker compose --env-file .env ps
```

---

## 9. Monitoring (Prometheus + Grafana)

Add to `docker-compose.yml` or create `docker-compose.monitoring.yml`:

```yaml
services:
  prometheus:
    image: prom/prometheus:v3.2.1
    volumes:
      - ./config/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus_data:/prometheus
    command:
      - "--config.file=/etc/prometheus/prometheus.yml"
      - "--storage.tsdb.path=/prometheus"
      - "--storage.tsdb.retention.time=30d"
    ports:
      - "127.0.0.1:9090:9090"
    restart: unless-stopped

  grafana:
    image: grafana/grafana:11.5.1
    volumes:
      - grafana_data:/var/lib/grafana
    environment:
      GF_SECURITY_ADMIN_PASSWORD: ${GRAFANA_PASSWORD:-admin}
      GF_INSTALL_PLUGINS: grafana-clock-panel
    ports:
      - "127.0.0.1:3000:3000"
    restart: unless-stopped

volumes:
  prometheus_data:
  grafana_data:
```

Basic `config/prometheus.yml`:

```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: aegiscore-api
    metrics_path: /metrics
    static_configs:
      - targets: ["api:8080"]
  - job_name: postgres
    static_configs:
      - targets: ["postgres:9187"]
  - job_name: redis
    static_configs:
      - targets: ["redis-exporter:9121"]
```

---

## 10. Maintenance Commands

```bash
# View logs
journalctl -u aegiscore -f
docker compose --env-file .env logs -f --tail 100

# Update to latest images
docker compose --env-file .env pull
docker compose --env-file .env up -d --wait

# Restart stack
sudo systemctl restart aegiscore

# Database backup
docker exec aegiscore-postgres-1 pg_dump -U aegiscore_admin aegiscore > backup_$(date +%Y%m%d).sql
```

---

## 11. Troubleshooting

| Issue                          | Check                                                                 |
|--------------------------------|-----------------------------------------------------------------------|
| Containers won't start         | `docker compose --env-file .env logs api` and check env var placeholders |
| Nginx 502 Bad Gateway          | Verify portal and API containers are healthy: `docker compose ps`     |
| Database connection refused    | Check `postgres` container is running and password matches `.env`     |
| SSL certificate expired        | Run `sudo certbot renew --force-renewal`                              |
| Port 8080 already in use       | Change `AEGISCORE_PORT` in `.env`                                     |
| Disk full                      | Prune unused Docker resources: `docker system prune -a --volumes`     |
| Permission denied on volumes   | Check directory ownership matches container user (UID 1000)           |
| Out of memory                  | Increase swap: `sudo fallocate -l 4G /swapfile && sudo mkswap /swapfile && sudo swapon /swapfile` |
