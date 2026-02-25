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
COPY ["BillingService/BillingService.Api/BillingService.Api.csproj", "BillingService/BillingService.Api/"]
COPY ["BillingService/BillingService.Application/BillingService.Application.csproj", "BillingService/BillingService.Application/"]
COPY ["BillingService/BillingService.Domain/BillingService.Domain.csproj", "BillingService/BillingService.Domain/"]
COPY ["BillingService/BillingService.Infrastructure/BillingService.Infrastructure.csproj", "BillingService/BillingService.Infrastructure/"]
COPY ["BillingService/BillingService.Shared/BillingService.Shared.csproj", "BillingService/BillingService.Shared/"]
COPY ["_Shared/CarDealer.Shared/CarDealer.Shared.csproj", "_Shared/CarDealer.Shared/"]
COPY ["_Shared/CarDealer.Contracts/CarDealer.Contracts.csproj", "_Shared/CarDealer.Contracts/"]
COPY ["_Shared/CarDealer.Shared.Idempotency/CarDealer.Shared.Idempotency.csproj", "_Shared/CarDealer.Shared.Idempotency/"]
COPY ["_Shared/CarDealer.Shared.Logging/CarDealer.Shared.Logging.csproj", "_Shared/CarDealer.Shared.Logging/"]
COPY ["_Shared/CarDealer.Shared.ErrorHandling/CarDealer.Shared.ErrorHandling.csproj", "_Shared/CarDealer.Shared.ErrorHandling/"]
COPY ["_Shared/CarDealer.Shared.Observability/CarDealer.Shared.Observability.csproj", "_Shared/CarDealer.Shared.Observability/"]
COPY ["_Shared/CarDealer.Shared.Audit/CarDealer.Shared.Audit.csproj", "_Shared/CarDealer.Shared.Audit/"]

RUN dotnet restore "BillingService/BillingService.Api/BillingService.Api.csproj"

# Copy everything else
COPY . .
WORKDIR "/src/BillingService/BillingService.Api"

# Publish stage (dotnet publish implicitly builds)
FROM build AS publish
RUN dotnet publish "BillingService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Install wget for health checks and create secrets directory
RUN apk add --no-cache wget \
    && mkdir -p /run/secrets

# Create non-root user (Alpine syntax)
RUN addgroup -g 1000 appgroup && \
    adduser -D -u 1000 -G appgroup appuser && \
    chown -R appuser:appgroup /app /run/secrets

USER appuser

COPY --from=publish --chown=appuser:appgroup /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080

# Expose port
EXPOSE 8080

# Health check using wget
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "BillingService.Api.dll"]
