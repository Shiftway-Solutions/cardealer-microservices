FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy ConfigurationService projects
COPY ["ConfigurationService/ConfigurationService.Api/ConfigurationService.Api.csproj", "ConfigurationService/ConfigurationService.Api/"]
COPY ["ConfigurationService/ConfigurationService.Application/ConfigurationService.Application.csproj", "ConfigurationService/ConfigurationService.Application/"]
COPY ["ConfigurationService/ConfigurationService.Domain/ConfigurationService.Domain.csproj", "ConfigurationService/ConfigurationService.Domain/"]
COPY ["ConfigurationService/ConfigurationService.Infrastructure/ConfigurationService.Infrastructure.csproj", "ConfigurationService/ConfigurationService.Infrastructure/"]

# Copy ServiceDiscovery projects (required dependency)
COPY ["ServiceDiscovery/ServiceDiscovery.Domain/ServiceDiscovery.Domain.csproj", "ServiceDiscovery/ServiceDiscovery.Domain/"]
COPY ["ServiceDiscovery/ServiceDiscovery.Application/ServiceDiscovery.Application.csproj", "ServiceDiscovery/ServiceDiscovery.Application/"]
COPY ["ServiceDiscovery/ServiceDiscovery.Infrastructure/ServiceDiscovery.Infrastructure.csproj", "ServiceDiscovery/ServiceDiscovery.Infrastructure/"]

# Restore packages
RUN dotnet restore "ConfigurationService/ConfigurationService.Api/ConfigurationService.Api.csproj"

# Copy all source code
COPY ConfigurationService/ ConfigurationService/
COPY ServiceDiscovery/ ServiceDiscovery/

# Build
WORKDIR "/src/ConfigurationService/ConfigurationService.Api"
RUN dotnet build "ConfigurationService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConfigurationService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r appgroup && useradd -r -g appgroup -m appuser && \
    chown -R appuser:appuser /app

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080

USER appuser

EXPOSE 8080
COPY --from=publish --chown=appuser:appuser /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ConfigurationService.Api.dll"]
