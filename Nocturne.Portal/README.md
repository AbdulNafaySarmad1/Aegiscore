# AegisCore Portal

Next.js 16 + React 19 portal. The .NET API remains the authoritative business,
security, licensing, workflow, and persistence layer.

```bash
npm ci
npm run dev
npm run build
```

The production image uses Next.js static export behind Nginx. Nginx serves
portal routes (including `/setup`) and proxies `/api`, `/hubs`, `/swagger`, and
`/health` to the AegisCore API container.
