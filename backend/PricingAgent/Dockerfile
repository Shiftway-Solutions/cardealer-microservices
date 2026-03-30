# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy shared libraries
COPY ["_Shared/CarDealer.Shared/CarDealer.Shared.csproj", "_Shared/CarDealer.Shared/"]
COPY ["_Shared/CarDealer.Contracts/CarDealer.Contracts.csproj", "_Shared/CarDealer.Contracts/"]
COPY ["_Shared/CarDealer.Shared.Logging/CarDealer.Shared.Logging.csproj", "_Shared/CarDealer.Shared.Logging/"]
COPY ["_Shared/CarDealer.Shared.ErrorHandling/CarDealer.Shared.ErrorHandling.csproj", "_Shared/CarDealer.Shared.ErrorHandling/"]
COPY ["_Shared/CarDealer.Shared.Observability/CarDealer.Shared.Observability.csproj", "_Shared/CarDealer.Shared.Observability/"]
COPY ["_Shared/CarDealer.Shared.Audit/CarDealer.Shared.Audit.csproj", "_Shared/CarDealer.Shared.Audit/"]
COPY ["_Shared/CarDealer.Shared.Resilience/CarDealer.Shared.Resilience.csproj", "_Shared/CarDealer.Shared.Resilience/"]

# Copy service discovery
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Application/ServiceDiscovery.Application.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Application/"]
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Infrastructure/ServiceDiscovery.Infrastructure.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Infrastructure/"]
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Domain/ServiceDiscovery.Domain.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Domain/"]

# Copy PricingAgent projects
COPY ["PricingAgent/PricingAgent.Api/PricingAgent.Api.csproj", "PricingAgent/PricingAgent.Api/"]
COPY ["PricingAgent/PricingAgent.Application/PricingAgent.Application.csproj", "PricingAgent/PricingAgent.Application/"]
COPY ["PricingAgent/PricingAgent.Domain/PricingAgent.Domain.csproj", "PricingAgent/PricingAgent.Domain/"]
COPY ["PricingAgent/PricingAgent.Infrastructure/PricingAgent.Infrastructure.csproj", "PricingAgent/PricingAgent.Infrastructure/"]

RUN dotnet restore "PricingAgent/PricingAgent.Api/PricingAgent.Api.csproj"

COPY . .
WORKDIR "/src/PricingAgent/PricingAgent.Api"

FROM build AS publish
RUN dotnet publish "PricingAgent.Api.csproj" -c Release -o /app/publish --self-contained false /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080

RUN apk add --no-cache wget
RUN mkdir -p /run/secrets

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

RUN adduser -D -u 1000 appuser && chown -R appuser:appuser /app /run/secrets
USER appuser

COPY --from=publish --chown=appuser:appuser /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/api/pricing-agent/health || exit 1

