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
COPY _Shared/CarDealer.Shared/CarDealer.Shared.csproj _Shared/CarDealer.Shared/
COPY _Shared/CarDealer.Contracts/CarDealer.Contracts.csproj _Shared/CarDealer.Contracts/
COPY _Shared/CarDealer.Shared.Idempotency/CarDealer.Shared.Idempotency.csproj _Shared/CarDealer.Shared.Idempotency/
COPY _Shared/CarDealer.Shared.Logging/CarDealer.Shared.Logging.csproj _Shared/CarDealer.Shared.Logging/
COPY _Shared/CarDealer.Shared.ErrorHandling/CarDealer.Shared.ErrorHandling.csproj _Shared/CarDealer.Shared.ErrorHandling/
COPY _Shared/CarDealer.Shared.Observability/CarDealer.Shared.Observability.csproj _Shared/CarDealer.Shared.Observability/
COPY _Shared/CarDealer.Shared.Audit/CarDealer.Shared.Audit.csproj _Shared/CarDealer.Shared.Audit/

# Copy service projects
COPY PaymentService/PaymentService.Domain/PaymentService.Domain.csproj PaymentService/PaymentService.Domain/
COPY PaymentService/PaymentService.Application/PaymentService.Application.csproj PaymentService/PaymentService.Application/
COPY PaymentService/PaymentService.Infrastructure/PaymentService.Infrastructure.csproj PaymentService/PaymentService.Infrastructure/
COPY PaymentService/PaymentService.Api/PaymentService.Api.csproj PaymentService/PaymentService.Api/

# Restore dependencies
WORKDIR /src/PaymentService/PaymentService.Api
RUN dotnet restore

# Copy all source code
WORKDIR /src
COPY _Shared/CarDealer.Shared/ _Shared/CarDealer.Shared/
COPY _Shared/CarDealer.Contracts/ _Shared/CarDealer.Contracts/
COPY _Shared/CarDealer.Shared.Idempotency/ _Shared/CarDealer.Shared.Idempotency/
COPY _Shared/CarDealer.Shared.Logging/ _Shared/CarDealer.Shared.Logging/
COPY _Shared/CarDealer.Shared.ErrorHandling/ _Shared/CarDealer.Shared.ErrorHandling/
COPY _Shared/CarDealer.Shared.Observability/ _Shared/CarDealer.Shared.Observability/
COPY _Shared/CarDealer.Shared.Audit/ _Shared/CarDealer.Shared.Audit/
COPY PaymentService/ PaymentService/

# Build the application
WORKDIR /src/PaymentService/PaymentService.Api
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Install wget for health checks and create secrets directory
RUN apk add --no-cache wget \
    && mkdir -p /run/secrets

# Create non-root user for security
RUN addgroup -S appgroup && adduser -S -G appgroup appuser

# Copy published files
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

# Change ownership and switch to non-root user
RUN chown -R appuser:appgroup /app /run/secrets
USER appuser

# Expose port
EXPOSE 8080

# Health check using wget
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "PaymentService.Api.dll"]
