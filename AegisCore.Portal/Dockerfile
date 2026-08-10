# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS build
WORKDIR /src
ARG NEXT_PUBLIC_DEMO_MODE=false
ARG NEXT_PUBLIC_DEMO_OFFLINE_PREVIEW=false
ENV NEXT_PUBLIC_DEMO_MODE=$NEXT_PUBLIC_DEMO_MODE \
    NEXT_PUBLIC_DEMO_OFFLINE_PREVIEW=$NEXT_PUBLIC_DEMO_OFFLINE_PREVIEW

COPY package.json package-lock.json ./
RUN --mount=type=cache,id=npm,target=/root/.npm npm ci --prefer-offline --no-audit --no-fund

COPY . .
RUN npm run build

FROM node:22-alpine AS runtime
LABEL org.opencontainers.image.title="AegisCore Portal" \
      org.opencontainers.image.description="AegisCore University ERP portal"
ARG NEXT_PUBLIC_DEMO_MODE=false
ARG NEXT_PUBLIC_DEMO_OFFLINE_PREVIEW=false
ENV NODE_ENV=production \
    NEXT_PUBLIC_DEMO_MODE=$NEXT_PUBLIC_DEMO_MODE \
    NEXT_PUBLIC_DEMO_OFFLINE_PREVIEW=$NEXT_PUBLIC_DEMO_OFFLINE_PREVIEW \
    PORT=8080 \
    HOSTNAME=0.0.0.0
WORKDIR /app
RUN addgroup -S aegiscore && adduser -S -G aegiscore aegiscore
COPY --from=build --chown=aegiscore:aegiscore /src/.next/standalone ./
COPY --from=build --chown=aegiscore:aegiscore /src/.next/static ./.next/static
COPY --from=build --chown=aegiscore:aegiscore /src/public ./public
USER aegiscore

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD wget -q -O - http://127.0.0.1:8080/portal-health >/dev/null || exit 1
CMD ["node", "server.js"]
