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
COPY ["ErrorService/ErrorService.Api/ErrorService.Api.csproj", "ErrorService/ErrorService.Api/"]
COPY ["ErrorService/ErrorService.Application/ErrorService.Application.csproj", "ErrorService/ErrorService.Application/"]
COPY ["ErrorService/ErrorService.Domain/ErrorService.Domain.csproj", "ErrorService/ErrorService.Domain/"]
COPY ["ErrorService/ErrorService.Infrastructure/ErrorService.Infrastructure.csproj", "ErrorService/ErrorService.Infrastructure/"]
COPY ["ErrorService/ErrorService.Shared/ErrorService.Shared.csproj", "ErrorService/ErrorService.Shared/"]
COPY ["_Shared/CarDealer.Shared/CarDealer.Shared.csproj", "_Shared/CarDealer.Shared/"]
COPY ["_Shared/CarDealer.Contracts/CarDealer.Contracts.csproj", "_Shared/CarDealer.Contracts/"]
COPY ["_Shared/CarDealer.Shared.Audit/CarDealer.Shared.Audit.csproj", "_Shared/CarDealer.Shared.Audit/"]
COPY ["_Shared/CarDealer.Shared.ErrorHandling/CarDealer.Shared.ErrorHandling.csproj", "_Shared/CarDealer.Shared.ErrorHandling/"]
COPY ["_Shared/CarDealer.Shared.Logging/CarDealer.Shared.Logging.csproj", "_Shared/CarDealer.Shared.Logging/"]
COPY ["_Shared/CarDealer.Shared.Observability/CarDealer.Shared.Observability.csproj", "_Shared/CarDealer.Shared.Observability/"]
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Domain/ServiceDiscovery.Domain.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Domain/"]
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Application/ServiceDiscovery.Application.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Application/"]
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Infrastructure/ServiceDiscovery.Infrastructure.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Infrastructure/"]

RUN dotnet restore "ErrorService/ErrorService.Api/ErrorService.Api.csproj"

# Copy everything else
COPY . .
WORKDIR "/src/ErrorService/ErrorService.Api"

# Publish stage (dotnet publish implicitly builds)
FROM build AS publish
RUN dotnet publish "ErrorService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

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

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080

# Health check using wget
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ErrorService.Api.dll"]
