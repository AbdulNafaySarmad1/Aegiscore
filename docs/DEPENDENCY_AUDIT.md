# Dependency Vulnerability Audit — AegisCore

**Audit date**: 2026-08-05  
**Audit type**: Full dependency scan (NuGet + npm)  
**Scope**: All project files under `AegisCore/` (excluding backup `aegiscore-backend/`)

---

## 1. NuGet Packages — AegisCore.Infrastructure

| Package | Version | Status | Notes |
|---|---|---|---|
| **NEST** | 7.17.5 | **CRITICAL** | EOL since 2023. Replaced by `Elastic.Clients.Elasticsearch` (v8.x). No further security patches. Requires code migration. |
| **Elasticsearch.Net** (transitive) | 7.17.5 | **CRITICAL** | Bundled with NEST. Same EOL risk. |
| AWSSDK.S3 | 3.7.511.8 | OK | **Recently updated** from 3.7.400. Latest 3.7.x. v4.0+ exists (major bump). |
| AWSSDK.SecretsManager | 3.7.504.43 | OK | **Recently updated** from 3.7.400. Latest 3.7.x. |
| AWSSDK.Core (transitive) | 3.7.501.1 | OK | Bundled with AWSSDK packages. |
| Azure.Identity | 1.21.0 | OK | **Recently updated** from 1.12.0 (CVE-2024-35255 mitigated in 1.12.0+). |
| Azure.Security.KeyVault.Secrets | 4.11.0 | OK | **Recently updated** from 4.6.0. |
| Azure.ResourceManager.PostgreSql | 1.2.0 | OK | Current. 1.4.2 available (minor). |
| Google.Cloud.SecretManager.V1 | 2.8.0 | OK | **Recently updated** from 1.7.0 (multi-year-old version). |
| Casbin.NET | 2.21.2 | OK | **Recently updated** from 2.12.0. |
| Cronos | 0.13.0 | OK | **Recently updated** from 0.8.4. |
| Serilog | 4.4.0 | OK | **Recently updated** from 4.2.0. |
| System.Security.Cryptography.Xml | 10.0.10 | OK | **Recently updated** from 9.0.18 (now matches .NET 10 target). |
| BouncyCastle.Cryptography | 2.7.0 | OK | Latest 2.x. No 3.x exists on NuGet. |
| SixLabors.ImageSharp | 3.1.12 | **WATCH** | Earlier versions had CVE-2024-32035, CVE-2024-41132. 3.1.12 is latest 3.x. 4.0.0 available (major). |
| Microsoft.Graph | 6.2.0 | OK | Latest version per NuGet (confirmed 2026-08-05). |
| StackExchange.Redis | 2.8.16 | OK | v3.1.3 available (major bump). 2.8.x line is stable. Consider upgrading for .NET 10. |
| Quartz / Quartz.Extensions.Hosting | 3.13.0 | OK | v3.19.1 available (minor updates). |
| Konscious.Security.Cryptography.Argon2 | 1.3.1 | OK | Password hashing. Current. |
| Lextm.SharpSnmpLib | 12.5.0 | OK | Current. |
| Microsoft.IdentityModel.Tokens | 8.18.0 | OK | Pinned above CVE-2024-21319 range. 8.22.0 available. |
| Microsoft.EntityFrameworkCore | 10.0.0 | OK | .NET 10 RTM. 10.0.10 available (servicing update). |

---

## 2. NuGet Packages — AegisCore.API

| Package | Version | Status | Notes |
|---|---|---|---|
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.0 | OK | .NET 10 RTM. |
| Serilog.AspNetCore | 9.0.0 | OK | v10.0.0 available for .NET 10. |
| FluentValidation.AspNetCore | 11.3.0 | OK | v11.3.1 available. Application layer uses 11.11.0 (standalone). |
| Swashbuckle.AspNetCore | 6.7.3 | OK | v10.2.3 available but brings breaking changes. |
| AspNetCoreRateLimit | 5.0.0 | OK | Current. |
| Hellang.Middleware.ProblemDetails | 6.5.1 | OK | Current. |
| Serilog.Sinks.PostgreSQL | 2.3.0 | OK | Current. |
| AspNetCore.HealthChecks.NpgSql | 9.0.0 | OK | Should align with .NET 10 when available. |
| AspNetCore.HealthChecks.Redis | 9.0.0 | OK | Same as above. |
| System.IdentityModel.Tokens.Jwt | 8.18.0 | OK | 8.22.0 available. |
| Microsoft.IdentityModel.JsonWebTokens | 8.18.0 | OK | 8.22.0 available. |

---

## 3. NuGet Packages — AegisCore.Application

| Package | Version | Status | Notes |
|---|---|---|---|
| FluentValidation | 11.11.0 | OK | v12.1.1 available (major). |
| MediatR | 12.4.1 | OK | v14.2.0 available (major). |
| AutoMapper | 16.2.0 | OK | Current. |
| PdfSharpCore | 1.3.67 | OK | Current. |
| QRCoder | 1.6.0 | OK | v1.8.0 available. |
| Npgsql | 10.0.0 | OK | .NET 10 RTM. 10.0.3 available. |

---

## 4. NuGet Packages — AegisCore.LedgerVerifier

| Package | Version | Status | Notes |
|---|---|---|---|
| Newtonsoft.Json | 13.0.4 | OK | Latest. Required by Google.Api.Gax transitive dep. |
| BouncyCastle.Cryptography | 2.7.0 | OK | Same as Infrastructure. |

---

## 5. NuGet Packages — AegisCore.Tests

| Package | Version | Status | Notes |
|---|---|---|---|
| xunit | 2.7.0 | OK | v2.9.3 available. |
| Moq | 4.20.70 | OK | v4.20.72 available. |
| coverlet.collector | 6.0.0 | OK | v10.0.1 available (major). |
| Microsoft.NET.Test.Sdk | 17.9.0 | OK | v18.8.1 available. |
| System.Security.Cryptography.Xml | 10.0.0 | OK | Already on .NET 10 version. |

---

## 6. npm Packages — AegisCore.Portal

**npm audit result**: 0 vulnerabilities (info: 0, low: 0, moderate: 0, high: 0, critical: 0)

| Package | Version | Status | Notes |
|---|---|---|---|
| next | 16.2.12 | OK | Latest stable 16.x. |
| react / react-dom | 19.2.3 | OK | Current React 19.x. |
| @microsoft/signalr | 10.0.0 | OK | v10.0.11 available. No CVEs reported. |
| lucide-react | 0.577.0 | OK | Current. v1.28.0 is a major rewrite (breaking). |
| recharts | 3.10.1 | OK | Current. |
| next-intl | 4.13.4 | OK | v4.13.5 available. |
| framer-motion | 12.43.0 | OK | Current. |
| @noble/post-quantum | 0.6.1 | OK | Post-quantum crypto library. Current. |
| eslint | 9.39.5 | OK | v10.8.0 available (major). |
| typescript | 5.9.3 | OK | v7.0.2 available (major — requires migration). |
| sharp (override) | 0.35.3 | OK | Latest. Previous versions had CVE-2023-4863 (libwebp). |
| postcss (override) | 8.5.25 | OK | Latest. Previous versions (<8.4.31) had CVE-2023-44270. |

**Overrides applied**:
- `brace-expansion` → ^5.0.9
- `minimatch` → ^10.2.6
- `postcss` → ^8.5.25 (mitigates CVE-2023-44270)
- `sharp` → ^0.35.3 (mitigates libwebp CVE)

---

## 7. Transitive Dependency Audit

### Identity & Auth Pipeline
| Package | Version | Status |
|---|---|---|
| Microsoft.Identity.Client | 4.83.1 | OK |
| Microsoft.Identity.Client.Extensions.Msal | 4.83.1 | OK |
| Microsoft.IdentityModel.* | 8.18.0 | OK (8.22.0 available) |
| Microsoft.IdentityModel.Protocols.OpenIdConnect | 8.18.0 | OK |

### Network & Protocol
| Package | Version | Status |
|---|---|---|
| Grpc.Core.Api / Grpc.Net.Client / Grpc.Net.Common | 2.71.0 | OK |
| Google.Protobuf | 3.31.1 | OK |
| Pipelines.Sockets.Unofficial | 2.2.8 | OK (Redis dependency) |
| Newtonsoft.Json | 13.0.4 | OK (Google.Api.Gax dep, latest) |

### System.* Packages
| Package | Version | Status |
|---|---|---|
| System.Security.Cryptography.Pkcs | 10.0.10 | OK (aligned with .NET 10) |
| System.Security.Cryptography.ProtectedData | 9.0.0 | OK (framework dep) |
| System.Configuration.ConfigurationManager | 9.0.0 | OK |
| System.Management | 7.0.2 | OK (framework dep) |
| System.Formats.Nrbf | 9.0.0 | OK |
| SharpZipLib | 1.4.2 | OK (latest, CVE-2022-40127 fixed in 1.4.0+) |

---

## 8. Verified Absent / Removed

| Package | Status |
|---|---|
| Microsoft.AspNetCore.Http.Abstractions (v2.2.0) | **VERIFIED REMOVED** — only in backup `aegiscore-backend/`, not in main project |
| Microsoft.AspNetCore.SignalR.Core (v1.1.0) | Only in backup `aegiscore-backend/` |
| System.Text.Encodings.Web (pre-CVE-2024-43485) | Not present in any main project csproj |
| Newtonsoft.Json (pre-13.0.3) | Only in LedgerVerifier at 13.0.4 (latest) |
| Microsoft.AspNetCore v2.x packages | **VERIFIED ABSENT** — only FrameworkReference used |

---

## 9. Recommended Upgrade Paths

### Immediate (critical)
| Package | Current | Target | Effort |
|---|---|---|---|
| **NEST** → Elastic.Clients.Elasticsearch | 7.17.5 | 8.x | High — full client replacement. NEST API is completely different. |

### Near-term (within 3 months)
| Package | Current | Target | Effort |
|---|---|---|---|
| SixLabors.ImageSharp | 3.1.12 | 4.0.0 | Medium — major API changes in image processing |
| StackExchange.Redis | 2.8.16 | 3.1.3 | Low — minor API changes, major version bump |
| Serilog.AspNetCore | 9.0.0 | 10.0.0 | Low — .NET 10 alignment |
| Quartz | 3.13.0 | 3.19.1 | Low — within same major |
| Microsoft.IdentityModel.* | 8.18.0 | 8.22.0 | Low — patch updates |

### Future consideration
| Package | Current | Target | Effort |
|---|---|---|---|
| Swashbuckle.AspNetCore | 6.7.3 | 10.2.3 | Medium — breaking changes in Swagger gen |
| FluentValidation (standalone) | 11.11.0 | 12.1.1 | Low — feature additions |
| MediatR | 12.4.1 | 14.2.0 | Low — feature additions |
| AWSSDK.* | 3.7.x | 4.x | Low — AWS SDKs maintain backward compat |

### npm
| Package | Current | Target | Effort |
|---|---|---|---|
| @microsoft/signalr | 10.0.0 | 10.0.11 | Low — patch |
| next-intl | 4.13.4 | 4.13.5 | Low — patch |
| lucide-react | 0.577.0 | 1.x | High — complete icon redesign |

---

## 10. Summary

| Category | Count |
|---|---|
| Total NuGet top-level packages | 48 (across all projects) |
| Total npm top-level dependencies | 10 (production) |
| npm vulnerabilities | **0** |
| **Updated in this audit** | **9 packages** |
| Critical issues found | **1** (NEST 7.17.5 EOL) |
| Packages flagged for monitoring | **1** (ImageSharp 3.1.12) |
| Obsolete v2.2.0 packages | **0** (confirmed removed) |

### Fixed in This Audit
1. Azure.Identity: 1.12.0 → 1.21.0
2. AWSSDK.S3: 3.7.400 → 3.7.511.8
3. AWSSDK.SecretsManager: 3.7.400 → 3.7.504.43
4. Azure.Security.KeyVault.Secrets: 4.6.0 → 4.11.0
5. Google.Cloud.SecretManager.V1: 1.7.0 → 2.8.0
6. Casbin.NET: 2.12.0 → 2.21.2
7. Cronos: 0.8.4 → 0.13.0
8. Serilog: 4.2.0 → 4.4.0
9. System.Security.Cryptography.Xml: 9.0.18 → 10.0.10
10. Newtonsoft.Json (LedgerVerifier): 13.0.3 → 13.0.4

### Next Action
The NEST → Elastic.Clients.Elasticsearch migration should be planned as a dedicated work item. NEST 7.17.5 receives no further security patches and the Elasticsearch 7.x line is end-of-life. All search-related code that uses `IElasticClient` or `Nest.*` types will need updating.
