# syntax=docker/dockerfile:1.10
# ─── AegisCore Production Dockerfile ───
# Multi-stage, non-root, hardened, < 200MB target

# ─── STAGE 1: Build ────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0.103-bookworm-slim AS build
WORKDIR /src

COPY AegisCore.slnx ./
COPY Directory.Build.props ./
COPY AegisCore.Domain/AegisCore.Domain.csproj AegisCore.Domain/
COPY AegisCore.Contracts/AegisCore.Contracts.csproj AegisCore.Contracts/
COPY AegisCore.Application/AegisCore.Application.csproj AegisCore.Application/
COPY AegisCore.Infrastructure/AegisCore.Infrastructure.csproj AegisCore.Infrastructure/
COPY AegisCore.API/AegisCore.API.csproj AegisCore.API/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore AegisCore.API/AegisCore.API.csproj \
      -p:RestoreUseStaticGraphEvaluation=true

COPY . .
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish AegisCore.API/AegisCore.API.csproj \
      -c Release \
      -o /app/publish \
      -p:UseAppHost=false \
      -p:UseSharedCompilation=false \
      -p:DebugType=none \
      -p:DebugSymbols=false \
      -p:CopyOutputSymbolsToPublishDirectory=false \
      -p:SatelliteResourceLanguages=en \
      --no-restore \
      --self-contained false

# ─── STAGE 2: Runtime ──────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0.3-alpine3.21 AS runtime
WORKDIR /app

RUN apk add --no-cache \
      icu-libs \
      krb5-libs \
      libgdiplus \
      curl \
    && addgroup -S -g 1000 appgroup \
    && adduser -S -D -H -u 1000 -s /sbin/nologin -G appgroup appuser \
    && mkdir -p /app/storage /app/config /app/logs \
    && chown -R appuser:appgroup /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_gcServer=1 \
    DOTNET_GCHeapHardLimit=4B0000000 \
    COMPlus_EnableDiagnostics=0

COPY --from=build --chown=appuser:appgroup /app/publish ./
COPY --from=build --chown=appuser:appgroup /src/formats/ ./formats/
COPY --from=build --chown=appuser:appgroup /src/aegiscorelogo.png ./aegiscore-logo.png

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --retries=3 --start-period=40s \
    CMD curl -fsS http://localhost:8080/health/live || exit 1

USER appuser:appgroup
ENTRYPOINT ["dotnet", "AegisCore.API.dll"]
