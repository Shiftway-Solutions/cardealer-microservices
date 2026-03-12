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
COPY ["_Shared/CarDealer.Shared.Logging/CarDealer.Shared.Logging.csproj", "_Shared/CarDealer.Shared.Logging/"]
COPY ["_Shared/CarDealer.Shared.ErrorHandling/CarDealer.Shared.ErrorHandling.csproj", "_Shared/CarDealer.Shared.ErrorHandling/"]
COPY ["_Shared/CarDealer.Shared.Observability/CarDealer.Shared.Observability.csproj", "_Shared/CarDealer.Shared.Observability/"]
COPY ["_Shared/CarDealer.Shared.Audit/CarDealer.Shared.Audit.csproj", "_Shared/CarDealer.Shared.Audit/"]
COPY ["_Shared/CarDealer.Shared.Resilience/CarDealer.Shared.Resilience.csproj", "_Shared/CarDealer.Shared.Resilience/"]
COPY ["_Shared/CarDealer.Shared.LlmGateway/CarDealer.Shared.LlmGateway.csproj", "_Shared/CarDealer.Shared.LlmGateway/"]
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Application/ServiceDiscovery.Application.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Application/"]
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Infrastructure/ServiceDiscovery.Infrastructure.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Infrastructure/"]
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Domain/ServiceDiscovery.Domain.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Domain/"]

# Copy csproj files and restore
COPY ["ContactService/ContactService.Api/ContactService.Api.csproj", "ContactService/ContactService.Api/"]
COPY ["ContactService/ContactService.Application/ContactService.Application.csproj", "ContactService/ContactService.Application/"]
COPY ["ContactService/ContactService.Domain/ContactService.Domain.csproj", "ContactService/ContactService.Domain/"]
COPY ["ContactService/ContactService.Infrastructure/ContactService.Infrastructure.csproj", "ContactService/ContactService.Infrastructure/"]

RUN dotnet restore "ContactService/ContactService.Api/ContactService.Api.csproj"

# Copy everything else
COPY . .
WORKDIR "/src/ContactService/ContactService.Api"

# Publish stage (dotnet publish implicitly builds)
FROM build AS publish
RUN dotnet publish "ContactService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080

# Install wget for healthcheck
RUN apk add --no-cache wget

# Create secrets directory for Docker secrets
RUN mkdir -p /run/secrets

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

# Create non-root user
RUN adduser -D -u 1000 appuser && chown -R appuser:appuser /app /run/secrets
USER appuser

COPY --from=publish --chown=appuser:appuser /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ContactService.Api.dll"]
