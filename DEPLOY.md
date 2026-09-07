# Deploy AegisCore

1. Install Docker Engine and Docker Compose. Nocturne prepares your database,
   Redis, TLS address, license, and configuration during implementation.
2. Save the Nocturne-provided `docker-compose.client.yml` in a dedicated directory.
3. Save the securely supplied `.env` beside it; restrict access with `chmod 600 .env`.
4. Log in using the supplied client pull identity and token:

   ```sh
   mkdir -p .registry-auth && chmod 700 .registry-auth
   printf '%s' "$GHCR_PULL_TOKEN" | docker --config .registry-auth login ghcr.io --username "$GHCR_USERNAME" --password-stdin
   chmod 600 .registry-auth/config.json
   export DOCKER_CONFIG="$PWD/.registry-auth"
   ```

5. Start AegisCore:

   ```sh
   docker compose -f docker-compose.client.yml up -d
   ```

6. Open `{APP_URL}/setup`, enter the supplied setup token, and complete the wizard.
   Allow the application to restart after activation, then sign in.
7. Application updates are automatic: checks run every six hours. No routine action
   is needed. Nocturne handles schema and ingress maintenance; do not delete data volumes.
8. Support: **[Nocturne support contact placeholder]**.
