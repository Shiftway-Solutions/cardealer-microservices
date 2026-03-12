# syntax=docker/dockerfile:1
# Build stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy csproj files and restore
COPY ["AdminService/AdminService.Api/AdminService.Api.csproj", "AdminService/AdminService.Api/"]
COPY ["AdminService/AdminService.Application/AdminService.Application.csproj", "AdminService/AdminService.Application/"]
COPY ["AdminService/AdminService.Domain/AdminService.Domain.csproj", "AdminService/AdminService.Domain/"]
COPY ["AdminService/AdminService.Infrastructure/AdminService.Infrastructure.csproj", "AdminService/AdminService.Infrastructure/"]
COPY ["AdminService/AdminService.Shared/AdminService.Shared.csproj", "AdminService/AdminService.Shared/"]
COPY ["_Shared/CarDealer.Shared/CarDealer.Shared.csproj", "_Shared/CarDealer.Shared/"]
COPY ["_Shared/CarDealer.Contracts/CarDealer.Contracts.csproj", "_Shared/CarDealer.Contracts/"]
COPY ["_Shared/CarDealer.Shared.Audit/CarDealer.Shared.Audit.csproj", "_Shared/CarDealer.Shared.Audit/"]
COPY ["_Shared/CarDealer.Shared.ErrorHandling/CarDealer.Shared.ErrorHandling.csproj", "_Shared/CarDealer.Shared.ErrorHandling/"]
COPY ["_Shared/CarDealer.Shared.Logging/CarDealer.Shared.Logging.csproj", "_Shared/CarDealer.Shared.Logging/"]
COPY ["_Shared/CarDealer.Shared.Observability/CarDealer.Shared.Observability.csproj", "_Shared/CarDealer.Shared.Observability/"]
COPY ["_Shared/CarDealer.Shared.Resilience/CarDealer.Shared.Resilience.csproj", "_Shared/CarDealer.Shared.Resilience/"]
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Application/ServiceDiscovery.Application.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Application/"]
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Domain/ServiceDiscovery.Domain.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Domain/"]
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Infrastructure/ServiceDiscovery.Infrastructure.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Infrastructure/"]

RUN dotnet restore "AdminService/AdminService.Api/AdminService.Api.csproj"

# Copy everything else
COPY . .
WORKDIR "/src/AdminService/AdminService.Api"

# Publish stage (dotnet publish implicitly builds)
FROM build AS publish
RUN dotnet publish "AdminService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080

# Create directories for secrets
RUN mkdir -p /run/secrets

# Install wget for health checks (not included in alpine by default)
RUN apk add --no-cache wget

# Create non-root user (Alpine syntax)
RUN addgroup -g 1000 appuser && \
    adduser -D -u 1000 -G appuser appuser && \
    chown -R appuser:appuser /app /run/secrets

USER appuser

COPY --from=publish --chown=appuser:appuser /app/publish .

# Environment variables for secrets (can be overridden)
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080

# Health check using wget (lighter than curl on Alpine)
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 http://localhost:8080/health -qO- > /dev/null || exit 1

ENTRYPOINT ["dotnet", "AdminService.Api.dll"]
