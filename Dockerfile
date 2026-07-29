FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Nocturne.slnx ./
COPY Directory.Build.props ./
COPY AegisCore.Domain/AegisCore.Domain.csproj AegisCore.Domain/
COPY AegisCore.Contracts/AegisCore.Contracts.csproj AegisCore.Contracts/
COPY AegisCore.Application/AegisCore.Application.csproj AegisCore.Application/
COPY AegisCore.Infrastructure/AegisCore.Infrastructure.csproj AegisCore.Infrastructure/
COPY AegisCore.API/AegisCore.API.csproj AegisCore.API/

RUN dotnet restore AegisCore.API/AegisCore.API.csproj

COPY . .
RUN dotnet publish AegisCore.API/AegisCore.API.csproj -c Release -o /app/publish \
    -p:UseAppHost=false \
    -p:UseSharedCompilation=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN apk add --no-cache krb5-libs \
    && addgroup -S appgroup \
    && adduser -S -D -H -s /sbin/nologin -G appgroup appuser \
    && mkdir -p /app/storage /app/config \
    && chown -R appuser /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=build --chown=appuser /app/publish .

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD wget -q -O - http://localhost:8080/health >/dev/null || exit 1

USER appuser
ENTRYPOINT ["dotnet", "AegisCore.API.dll"]
