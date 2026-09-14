# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS portal-build
WORKDIR /portal
COPY AegisCore.Portal/package.json AegisCore.Portal/package-lock.json ./
RUN --mount=type=cache,id=npm,target=/root/.npm npm ci --no-audit --no-fund
COPY AegisCore.Portal/ ./
ENV NEXT_PUBLIC_DEMO_MODE=false AEGISCORE_API_INTERNAL_URL=http://127.0.0.1:8081 NEXT_TELEMETRY_DISABLED=1
RUN npm run build
RUN find /portal/.next/standalone /portal/.next/static -type f -name '*.map' -delete

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AegisCore.slnx ./
COPY Directory.Build.props ./
COPY AegisCore.Domain/AegisCore.Domain.csproj AegisCore.Domain/
COPY AegisCore.Contracts/AegisCore.Contracts.csproj AegisCore.Contracts/
COPY AegisCore.Application/AegisCore.Application.csproj AegisCore.Application/
COPY AegisCore.Infrastructure/AegisCore.Infrastructure.csproj AegisCore.Infrastructure/
COPY AegisCore.API/AegisCore.API.csproj AegisCore.API/
COPY AegisCore.Migrations.MySql/AegisCore.Migrations.MySql.csproj AegisCore.Migrations.MySql/
COPY AegisCore.Migrations.MsSql/AegisCore.Migrations.MsSql.csproj AegisCore.Migrations.MsSql/
COPY AegisCore.Migrations.Oracle/AegisCore.Migrations.Oracle.csproj AegisCore.Migrations.Oracle/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore AegisCore.API/AegisCore.API.csproj

COPY . .
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish AegisCore.API/AegisCore.API.csproj -c Release -o /app/publish \
    -p:UseAppHost=false \
    -p:UseSharedCompilation=false \
    -p:DebugType=None -p:DebugSymbols=false \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN apk add --no-cache icu-libs krb5-libs nodejs haproxy \
    && addgroup -S appgroup \
    && adduser -S -D -H -s /sbin/nologin -G appgroup appuser \
    && mkdir -p /app/storage /app/config \
    && chown -R appuser /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_EnableDiagnostics=0

COPY --from=build --chown=appuser /app/publish .
COPY --from=portal-build --chown=appuser /portal/.next/standalone /app/portal/
COPY --from=portal-build --chown=appuser /portal/.next/static /app/portal/.next/static/
COPY --from=portal-build --chown=appuser /portal/public /app/portal/public/
COPY --chown=appuser docker/client/ /app/client/
RUN sed -i 's/\r$//' /app/client/*.sh

COPY --chown=appuser formats/ /app/formats/
COPY --chown=appuser demo-scenarios/ /app/demo-scenarios/
COPY --chown=appuser aegiscorelogo.png /app/aegiscore-logo.png

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD wget -q -O - http://localhost:8080/health >/dev/null || exit 1

USER appuser
ENTRYPOINT ["sh", "/app/client/entrypoint.sh"]
