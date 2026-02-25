############################################
# PRODUCTION BUILD - VehiclesSaleService Multi-stage Dockerfile
############################################

# Stage 1: Build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy shared projects first
COPY _Shared/ _Shared/

# Copy service projects
COPY VehiclesSaleService/VehiclesSaleService.Domain/ VehiclesSaleService/VehiclesSaleService.Domain/
COPY VehiclesSaleService/VehiclesSaleService.Application/ VehiclesSaleService/VehiclesSaleService.Application/
COPY VehiclesSaleService/VehiclesSaleService.Infrastructure/ VehiclesSaleService/VehiclesSaleService.Infrastructure/
COPY VehiclesSaleService/VehiclesSaleService.Api/ VehiclesSaleService/VehiclesSaleService.Api/

# Restore dependencies
WORKDIR /src/VehiclesSaleService/VehiclesSaleService.Api
RUN dotnet restore

# Build
RUN dotnet build -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
EXPOSE 8080

# Install wget for healthcheck
RUN apk add --no-cache wget

# Security: run as non-root
RUN addgroup -g 1000 appuser && \
    adduser -D -u 1000 -G appuser appuser && \
    mkdir -p /run/secrets && \
    chown -R appuser:appuser /app /run/secrets

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

USER appuser

# Copy published app
COPY --from=publish --chown=appuser:appuser /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "VehiclesSaleService.Api.dll"]
