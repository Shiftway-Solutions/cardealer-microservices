# syntax=docker/dockerfile:1
# Build stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy shared projects
COPY ["_Shared/CarDealer.Shared/CarDealer.Shared.csproj", "_Shared/CarDealer.Shared/"]
COPY ["_Shared/CarDealer.Contracts/CarDealer.Contracts.csproj", "_Shared/CarDealer.Contracts/"]

# Copy ServiceDiscovery projects
COPY ["ServiceDiscovery/ServiceDiscovery.Domain/ServiceDiscovery.Domain.csproj", "ServiceDiscovery/ServiceDiscovery.Domain/"]
COPY ["ServiceDiscovery/ServiceDiscovery.Application/ServiceDiscovery.Application.csproj", "ServiceDiscovery/ServiceDiscovery.Application/"]
COPY ["ServiceDiscovery/ServiceDiscovery.Infrastructure/ServiceDiscovery.Infrastructure.csproj", "ServiceDiscovery/ServiceDiscovery.Infrastructure/"]

# Copy csproj files and restore
COPY ["ConfigurationService/ConfigurationService.Api/ConfigurationService.Api.csproj", "ConfigurationService/ConfigurationService.Api/"]
COPY ["ConfigurationService/ConfigurationService.Application/ConfigurationService.Application.csproj", "ConfigurationService/ConfigurationService.Application/"]
COPY ["ConfigurationService/ConfigurationService.Domain/ConfigurationService.Domain.csproj", "ConfigurationService/ConfigurationService.Domain/"]
COPY ["ConfigurationService/ConfigurationService.Infrastructure/ConfigurationService.Infrastructure.csproj", "ConfigurationService/ConfigurationService.Infrastructure/"]

RUN dotnet restore "ConfigurationService/ConfigurationService.Api/ConfigurationService.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/ConfigurationService/ConfigurationService.Api"
RUN dotnet build "ConfigurationService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ConfigurationService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Runtime stage - Alpine para menor superficie de ataque
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080

# Install wget for healthcheck and create secrets directory
RUN apk add --no-cache wget && \
    mkdir -p /run/secrets

# Set environment to Docker
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

# Create non-root user (Alpine uses addgroup/adduser)
RUN addgroup -g 1000 appuser && \
    adduser -D -u 1000 -G appuser appuser && \
    chown -R appuser:appuser /app

USER appuser

COPY --from=publish --chown=appuser:appuser /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ConfigurationService.Api.dll"]