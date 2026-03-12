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
COPY ["_Shared/CarDealer.Shared.Caching/CarDealer.Shared.Caching.csproj", "_Shared/CarDealer.Shared.Caching/"]
COPY ["_Shared/CarDealer.Shared.ErrorHandling/CarDealer.Shared.ErrorHandling.csproj", "_Shared/CarDealer.Shared.ErrorHandling/"]
COPY ["_Shared/CarDealer.Shared.LlmGateway/CarDealer.Shared.LlmGateway.csproj", "_Shared/CarDealer.Shared.LlmGateway/"]
COPY ["_Shared/CarDealer.Shared.Logging/CarDealer.Shared.Logging.csproj", "_Shared/CarDealer.Shared.Logging/"]
COPY ["_Shared/CarDealer.Shared.Observability/CarDealer.Shared.Observability.csproj", "_Shared/CarDealer.Shared.Observability/"]

# Copy csproj files and restore
COPY ["ListingAgent/ListingAgent.Api/ListingAgent.Api.csproj", "ListingAgent/ListingAgent.Api/"]
COPY ["ListingAgent/ListingAgent.Application/ListingAgent.Application.csproj", "ListingAgent/ListingAgent.Application/"]
COPY ["ListingAgent/ListingAgent.Domain/ListingAgent.Domain.csproj", "ListingAgent/ListingAgent.Domain/"]
COPY ["ListingAgent/ListingAgent.Infrastructure/ListingAgent.Infrastructure.csproj", "ListingAgent/ListingAgent.Infrastructure/"]

RUN dotnet restore "ListingAgent/ListingAgent.Api/ListingAgent.Api.csproj"

# Copy everything else
COPY . .
WORKDIR "/src/ListingAgent/ListingAgent.Api"

# Publish stage
FROM build AS publish
RUN dotnet publish "ListingAgent.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080

# Install wget for health checks
RUN apk add --no-cache wget

# Create non-root user and secrets directory
RUN getent group appgroup || addgroup -g 1000 appgroup && \
    getent passwd appuser || adduser -D -u 1000 -G appgroup appuser && \
    mkdir -p /run/secrets && \
    chown -R appuser:appgroup /app /run/secrets
USER appuser

# Set Docker environment
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=publish --chown=appuser:appgroup /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ListingAgent.Api.dll"]
