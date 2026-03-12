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
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Application/ServiceDiscovery.Application.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Application/"]
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Infrastructure/ServiceDiscovery.Infrastructure.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Infrastructure/"]
COPY ["_Shared/ServiceDiscovery/ServiceDiscovery.Domain/ServiceDiscovery.Domain.csproj", "_Shared/ServiceDiscovery/ServiceDiscovery.Domain/"]

# Copy csproj files and restore
COPY ["MediaService/MediaService.Api/MediaService.Api.csproj", "MediaService/MediaService.Api/"]
COPY ["MediaService/MediaService.Application/MediaService.Application.csproj", "MediaService/MediaService.Application/"]
COPY ["MediaService/MediaService.Domain/MediaService.Domain.csproj", "MediaService/MediaService.Domain/"]
COPY ["MediaService/MediaService.Infrastructure/MediaService.Infrastructure.csproj", "MediaService/MediaService.Infrastructure/"]
COPY ["MediaService/MediaService.Shared/MediaService.Shared.csproj", "MediaService/MediaService.Shared/"]

RUN dotnet restore "MediaService/MediaService.Api/MediaService.Api.csproj"

# Copy everything else
COPY . .
WORKDIR "/src/MediaService/MediaService.Api"

# Publish stage (dotnet publish implicitly builds)
FROM build AS publish
RUN dotnet publish "MediaService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080

# Install wget for health checks (Alpine uses apk)
RUN apk add --no-cache wget

# Create non-root user, uploads directory and secrets directory
RUN getent group appgroup || addgroup -g 1000 appgroup && \
    getent passwd appuser || adduser -D -u 1000 -G appgroup appuser && \
    mkdir -p /app/uploads && \
    mkdir -p /run/secrets && \
    chown -R appuser:appgroup /app /run/secrets
USER appuser

# Set Docker environment
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=publish --chown=appuser:appgroup /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "MediaService.Api.dll"]