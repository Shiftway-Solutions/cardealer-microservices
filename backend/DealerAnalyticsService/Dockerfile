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
COPY ["_Shared/CarDealer.Shared.Audit/CarDealer.Shared.Audit.csproj", "_Shared/CarDealer.Shared.Audit/"]
COPY ["_Shared/CarDealer.Shared.ErrorHandling/CarDealer.Shared.ErrorHandling.csproj", "_Shared/CarDealer.Shared.ErrorHandling/"]
COPY ["_Shared/CarDealer.Shared.Logging/CarDealer.Shared.Logging.csproj", "_Shared/CarDealer.Shared.Logging/"]
COPY ["_Shared/CarDealer.Shared.Observability/CarDealer.Shared.Observability.csproj", "_Shared/CarDealer.Shared.Observability/"]

# Copy ServiceDiscovery projects
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Application/ServiceDiscovery.Application.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Application/"]
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Domain/ServiceDiscovery.Domain.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Domain/"]
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Infrastructure/ServiceDiscovery.Infrastructure.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Infrastructure/"]

# Copy csproj files and restore
COPY ["DealerAnalyticsService/DealerAnalyticsService.Api/DealerAnalyticsService.Api.csproj", "DealerAnalyticsService/DealerAnalyticsService.Api/"]
COPY ["DealerAnalyticsService/DealerAnalyticsService.Application/DealerAnalyticsService.Application.csproj", "DealerAnalyticsService/DealerAnalyticsService.Application/"]
COPY ["DealerAnalyticsService/DealerAnalyticsService.Domain/DealerAnalyticsService.Domain.csproj", "DealerAnalyticsService/DealerAnalyticsService.Domain/"]
COPY ["DealerAnalyticsService/DealerAnalyticsService.Infrastructure/DealerAnalyticsService.Infrastructure.csproj", "DealerAnalyticsService/DealerAnalyticsService.Infrastructure/"]

RUN dotnet restore "DealerAnalyticsService/DealerAnalyticsService.Api/DealerAnalyticsService.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/DealerAnalyticsService/DealerAnalyticsService.Api"
RUN dotnet build "DealerAnalyticsService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DealerAnalyticsService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080

# Install wget for healthcheck
RUN apk add --no-cache wget

# Create non-root user and secrets directory
RUN adduser -D -u 1000 appuser && \
    mkdir -p /run/secrets && \
    chown -R appuser:appuser /app /run/secrets

# Set environment for Docker
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

USER appuser

COPY --from=publish --chown=appuser:appuser /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "DealerAnalyticsService.Api.dll"]
