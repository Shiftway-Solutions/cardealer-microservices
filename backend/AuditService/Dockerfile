# syntax=docker/dockerfile:1
# Build stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy shared projects first
COPY ["_Shared/CarDealer.Shared/CarDealer.Shared.csproj", "_Shared/CarDealer.Shared/"]
COPY ["_Shared/CarDealer.Contracts/CarDealer.Contracts.csproj", "_Shared/CarDealer.Contracts/"]

# Copy ServiceDiscovery projects
COPY ["ServiceDiscovery/ServiceDiscovery.Application/ServiceDiscovery.Application.csproj", "ServiceDiscovery/ServiceDiscovery.Application/"]
COPY ["ServiceDiscovery/ServiceDiscovery.Domain/ServiceDiscovery.Domain.csproj", "ServiceDiscovery/ServiceDiscovery.Domain/"]
COPY ["ServiceDiscovery/ServiceDiscovery.Infrastructure/ServiceDiscovery.Infrastructure.csproj", "ServiceDiscovery/ServiceDiscovery.Infrastructure/"]

# Copy csproj files and restore
COPY ["AuditService/AuditService.Api/AuditService.Api.csproj", "AuditService/AuditService.Api/"]
COPY ["AuditService/AuditService.Application/AuditService.Application.csproj", "AuditService/AuditService.Application/"]
COPY ["AuditService/AuditService.Domain/AuditService.Domain.csproj", "AuditService/AuditService.Domain/"]
COPY ["AuditService/AuditService.Infrastructure/AuditService.Infrastructure.csproj", "AuditService/AuditService.Infrastructure/"]
COPY ["AuditService/AuditService.Shared/AuditService.Shared.csproj", "AuditService/AuditService.Shared/"]

RUN dotnet restore "AuditService/AuditService.Api/AuditService.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/AuditService/AuditService.Api"
RUN dotnet build "AuditService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AuditService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080

# Install wget for health checks
RUN apk add --no-cache wget

# Create secrets directory for Docker secrets
RUN mkdir -p /run/secrets

# Create non-root user
RUN adduser -D -u 1000 appuser && chown -R appuser:appuser /app /run/secrets
USER appuser

# Set environment for Docker
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

COPY --from=publish --chown=appuser:appuser /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "AuditService.Api.dll"]
