# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app

# Install wget for healthcheck (Alpine uses apk)
RUN apk add --no-cache wget

# Create secrets directory
RUN mkdir -p /run/secrets

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
COPY ["ServiceDiscovery/ServiceDiscovery.Domain/ServiceDiscovery.Domain.csproj", "ServiceDiscovery/ServiceDiscovery.Domain/"]
COPY ["ServiceDiscovery/ServiceDiscovery.Application/ServiceDiscovery.Application.csproj", "ServiceDiscovery/ServiceDiscovery.Application/"]
COPY ["ServiceDiscovery/ServiceDiscovery.Infrastructure/ServiceDiscovery.Infrastructure.csproj", "ServiceDiscovery/ServiceDiscovery.Infrastructure/"]

# Copy csproj files and restore dependencies
COPY ["CacheService/CacheService.Api/CacheService.Api.csproj", "CacheService/CacheService.Api/"]
COPY ["CacheService/CacheService.Application/CacheService.Application.csproj", "CacheService/CacheService.Application/"]
COPY ["CacheService/CacheService.Domain/CacheService.Domain.csproj", "CacheService/CacheService.Domain/"]
COPY ["CacheService/CacheService.Infrastructure/CacheService.Infrastructure.csproj", "CacheService/CacheService.Infrastructure/"]

RUN dotnet restore "CacheService/CacheService.Api/CacheService.Api.csproj"

# Copy all source files
COPY _Shared/ ./_Shared/
COPY ServiceDiscovery/ ./ServiceDiscovery/
COPY CacheService/ ./CacheService/

# Build the application
WORKDIR "/src/CacheService/CacheService.Api"
RUN dotnet build "CacheService.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "CacheService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

# Set Docker environment
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080

RUN addgroup -g 1000 appuser && adduser -D -u 1000 -G appuser appuser
USER appuser
COPY --from=publish /app/publish .

# Health check (port must match ASPNETCORE_URLS)
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CacheService.Api.dll"]
