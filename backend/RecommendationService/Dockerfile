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
COPY _Shared/CarDealer.Shared.Logging/CarDealer.Shared.Logging.csproj _Shared/CarDealer.Shared.Logging/
COPY _Shared/CarDealer.Shared.Audit/CarDealer.Shared.Audit.csproj _Shared/CarDealer.Shared.Audit/
COPY _Shared/CarDealer.Shared.ErrorHandling/CarDealer.Shared.ErrorHandling.csproj _Shared/CarDealer.Shared.ErrorHandling/
# Copy service projects
COPY RecommendationService/RecommendationService.Domain/RecommendationService.Domain.csproj RecommendationService/RecommendationService.Domain/
COPY RecommendationService/RecommendationService.Application/RecommendationService.Application.csproj RecommendationService/RecommendationService.Application/
COPY RecommendationService/RecommendationService.Infrastructure/RecommendationService.Infrastructure.csproj RecommendationService/RecommendationService.Infrastructure/
COPY RecommendationService/RecommendationService.Api/RecommendationService.Api.csproj RecommendationService/RecommendationService.Api/

# Restore dependencies
WORKDIR /src/RecommendationService/RecommendationService.Api
RUN dotnet restore

# Copy all source code
WORKDIR /src
COPY _Shared/CarDealer.Shared/ _Shared/CarDealer.Shared/
COPY _Shared/CarDealer.Contracts/ _Shared/CarDealer.Contracts/
COPY _Shared/CarDealer.Shared.Logging/ _Shared/CarDealer.Shared.Logging/
COPY _Shared/CarDealer.Shared.Audit/ _Shared/CarDealer.Shared.Audit/
COPY _Shared/CarDealer.Shared.ErrorHandling/ _Shared/CarDealer.Shared.ErrorHandling/
COPY RecommendationService/ RecommendationService/

# Build the application
WORKDIR /src/RecommendationService/RecommendationService.Api
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

# Change to non-root user
USER appuser

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "RecommendationService.Api.dll"]
