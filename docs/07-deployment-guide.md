# Document 7 — Deployment Guide
**Standard:** ISO/IEC/IEEE 26514

| Field | Value |
|-------|-------|
| Document ID | AEGIS-DEP-001 |
| Version | 1.0.0 |
| Date | 2025-01-15 |
| Author | Abdul Nafay Sarmad, Founder & CEO |
| Status | Approved |

## Revision History

| Version | Date | Author | Description |
|---------|------|--------|-------------|
| 1.0.0 | 2025-01-15 | Abdul Nafay Sarmad | Initial release |

## 1. Introduction

### 1.1 Purpose

This Deployment Guide provides complete, step-by-step instructions for deploying AegisCore across all target environments: Hetzner EPYC (Helsinki primary, Nuremberg disaster recovery), and Azure App Service (staging and CI/CD). It is intended for system administrators and DevOps engineers responsible for initial installation, configuration, and ongoing maintenance of the AegisCore platform.

### 1.2 Scope

This document covers:
- Fresh installation on all target environments
- Database initialisation and migration
- Redis configuration
- Secrets injection via Infisical
- SSL/TLS certificate provisioning
- Health check verification
- Rollback procedures
- Environment-specific configuration

### 1.3 References

1. AegisCore Software Architecture Document (AEGIS-SAD-001)
2. AegisCore Security Architecture Document (AEGIS-SEC-001)
3. Docker Documentation — https://docs.docker.com
4. PostgreSQL 16 Documentation
5. Redis 7 Documentation

## 2. Deployment Architecture Overview

AegisCore is deployed across three infrastructure nodes:

- **Hetzner Helsinki (Primary):** Production API servers, PostgreSQL primary, Redis primary, nginx reverse proxy
- **Hetzner Nuremberg (Secondary/DR):** Standby API servers, PostgreSQL standby replica, Redis replica
- **Azure App Service:** Staging environment, CI/CD pipeline execution, auxiliary services

## 3. Prerequisites Checklist

### 3.1 Hardware Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| CPU | 16 cores (AMD EPYC or equivalent) | 32 cores |
| RAM | 64 GB | 128 GB |
| Storage | 500 GB NVMe SSD | 1 TB NVMe SSD (RAID 1) |
| Network | 1 Gbps | 10 Gbps |

### 3.2 Software Requirements

| Software | Version | Purpose |
|----------|---------|---------|
| Ubuntu Server | 22.04 LTS | Operating system |
| Docker | 24.0+ | Container runtime |
| Docker Compose | 2.20+ | Container orchestration |
| .NET Runtime | 10.0 | Application runtime (if not using Docker) |
| PostgreSQL | 16+ | Database server |
| pgvector | 0.5+ | Vector similarity search extension |
| Redis | 7.2+ | Cache and session store |
| nginx | 1.24+ | Reverse proxy |
| Infisical CLI | Latest | Secrets injection |
| curl | 8.0+ | Health check verification |
| git | 2.40+ | Source code management |

### 3.3 Network Requirements

| Port | Protocol | Source | Destination | Purpose |
|------|----------|--------|-------------|---------|
| 443 | TCP | Internet | nginx | HTTPS traffic |
| 80 | TCP | Internet | nginx | HTTP (redirect to HTTPS) |
| 8080 | TCP | nginx | API containers | Internal API traffic |
| 5432 | TCP | API containers | PostgreSQL | Database connections |
| 6379 | TCP | API containers | Redis | Cache connections |
| 22 | TCP | VPN CIDR | All servers | SSH administration |

### 3.4 External Services

| Service | Purpose | Credentials Required |
|---------|---------|---------------------|
| Infisical | Secrets management | Machine identity token |
| Let's Encrypt | TLS certificates | Domain ownership verification |
| GitHub | Source code and CI/CD | Deploy key with read access |
| Docker Hub / GHCR | Container registry | Pull credentials |

## 4. Step-by-Step Deployment Procedures

### 4.1 Fresh Installation — Hetzner Helsinki (Primary)

#### Step 1: Server Provisioning

1. Provision a Hetzner dedicated server with Ubuntu 22.04 LTS.
2. Configure hostname: `aegiscore-helsinki-01`
3. Update system packages:
   ```bash
   sudo apt update && sudo apt upgrade -y
   ```
4. Configure firewall (ufw):
   ```bash
   sudo ufw allow 443/tcp
   sudo ufw allow 80/tcp
   sudo ufw allow from <VPN_CIDR> to any port 22
   sudo ufw enable
   ```

#### Step 2: Install Docker

