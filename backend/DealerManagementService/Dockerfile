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

# Copy ServiceDiscovery projects
COPY ["ServiceDiscovery/ServiceDiscovery.Application/ServiceDiscovery.Application.csproj", "ServiceDiscovery/ServiceDiscovery.Application/"]
COPY ["ServiceDiscovery/ServiceDiscovery.Domain/ServiceDiscovery.Domain.csproj", "ServiceDiscovery/ServiceDiscovery.Domain/"]
COPY ["ServiceDiscovery/ServiceDiscovery.Infrastructure/ServiceDiscovery.Infrastructure.csproj", "ServiceDiscovery/ServiceDiscovery.Infrastructure/"]

# Copy csproj files and restore
COPY ["DealerManagementService/DealerManagementService.Api/DealerManagementService.Api.csproj", "DealerManagementService/DealerManagementService.Api/"]
COPY ["DealerManagementService/DealerManagementService.Application/DealerManagementService.Application.csproj", "DealerManagementService/DealerManagementService.Application/"]
COPY ["DealerManagementService/DealerManagementService.Domain/DealerManagementService.Domain.csproj", "DealerManagementService/DealerManagementService.Domain/"]
COPY ["DealerManagementService/DealerManagementService.Infrastructure/DealerManagementService.Infrastructure.csproj", "DealerManagementService/DealerManagementService.Infrastructure/"]

RUN dotnet restore "DealerManagementService/DealerManagementService.Api/DealerManagementService.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/DealerManagementService/DealerManagementService.Api"
RUN dotnet build "DealerManagementService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DealerManagementService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080

# Install wget for healthcheck (Alpine uses apk)
RUN apk add --no-cache wget

# Create non-root user and secrets directory (Alpine uses addgroup/adduser)
RUN addgroup -g 1000 appgroup && \
    adduser -D -u 1000 -G appgroup appuser && \
    mkdir -p /run/secrets /app/uploads && \
    chown -R appuser:appgroup /app /run/secrets

# Set environment for Docker
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=publish /app/publish .
RUN chown -R appuser:appgroup /app

USER appuser

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "DealerManagementService.Api.dll"]
