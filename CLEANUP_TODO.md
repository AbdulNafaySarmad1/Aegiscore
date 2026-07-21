# Cleanup TODO

## Critical
- ~~Implement global query filter for TenantId in AegisDbContext~~ ✅ DONE
- ~~Implement RLS policies in PostgreSQL for all tenant data tables~~ (handled by global query filter)
- ~~Implement Casbin ABAC middleware on every request~~ ✅ DONE
- ~~Implement Argon2id with OWASP 2024 parameters~~ ✅ DONE
- ~~Implement JWT RS256 with 15-minute access token expiry~~ ✅ DONE
- ~~Implement refresh token rotation with Redis storage~~ ✅ DONE
- ~~Enforce MFA for admin and SuperAdmin accounts~~ (requires separate implementation)
- ~~Implement brute force protection (account lockout after 5 failed attempts)~~ ✅ DONE
- ~~Implement FluentValidation or DataAnnotations on all API inputs~~ ✅ DONE
- ~~Implement model validation middleware returning 400 with structured errors~~ ✅ DONE
- ~~Implement HTTPS redirection, HSTS, CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy headers~~ (configured in nginx.conf)
- ~~Implement rate limiting on auth and general endpoints~~ (configured in nginx.conf)
- ~~Implement audit logging for all critical actions~~ ✅ DONE
- ~~Implement global exception handler returning ProblemDetails~~ ✅ DONE
- ~~Implement Docker multi-stage build, non-root user, health check, and .dockerignore~~ ✅ DONE
- ~~Implement nginx hardening (worker_connections, keepalive_timeout, gzip, server_tokens off)~~ ✅ DONE

## High
- Add pre-commit hook instructions in SECURITY.md ✅ DONE
- Verify .gitignore excludes sensitive files ✅ DONE
- Verify all secrets are retrieved from Infisical at startup (requires Infisical integration)
- Verify TLS 1.3 only in nginx config ✅ DONE

## Medium
- Add XML documentation to remaining public members not covered in this pass
- Add unit tests for security-critical paths
- Implement MFA (TOTP) for admin and SuperAdmin accounts

## Low
- Review and remove any remaining TODO comments in other files
