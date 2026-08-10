# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AegisCore.slnx ./
COPY Directory.Build.props ./
COPY AegisCore.Domain/AegisCore.Domain.csproj AegisCore.Domain/
COPY AegisCore.Contracts/AegisCore.Contracts.csproj AegisCore.Contracts/
COPY AegisCore.Application/AegisCore.Application.csproj AegisCore.Application/
COPY AegisCore.Infrastructure/AegisCore.Infrastructure.csproj AegisCore.Infrastructure/
COPY AegisCore.API/AegisCore.API.csproj AegisCore.API/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore AegisCore.API/AegisCore.API.csproj

COPY . .
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish AegisCore.API/AegisCore.API.csproj -c Release -o /app/publish \
    -p:UseAppHost=false \
    -p:UseSharedCompilation=false \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# PdfSharpCore resolves installed system fonts at runtime. The minimal Alpine
# ASP.NET image has none by default, so install fontconfig + DejaVu explicitly
# for challan/transcript PDF generation instead of failing with
# "No Fonts installed on this device!".
RUN apk add --no-cache icu-libs krb5-libs fontconfig ttf-dejavu \
    && fc-cache -f \
    && addgroup -S appgroup \
    && adduser -S -D -H -s /sbin/nologin -G appgroup appuser \
    && mkdir -p /app/storage /app/config \
    && chown -R appuser /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_EnableDiagnostics=0

COPY --from=build --chown=appuser /app/publish .

COPY --chown=appuser formats/ /app/formats/
COPY --chown=appuser aegiscorelogo.png /app/aegiscore-logo.png

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD wget -q -O - http://localhost:8080/health >/dev/null || exit 1

USER appuser
ENTRYPOINT ["dotnet", "AegisCore.API.dll"]
