############################################
# PRODUCTION Dockerfile - Based on Dockerfile.dev
############################################

# Stage 1: Build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false
WORKDIR /app

# Copy project files for restore (from Dockerfile.dev)
COPY ErrorService/ErrorService.Api/ErrorService.Api.csproj ./ErrorService/ErrorService.Api/
COPY ErrorService/ErrorService.Application/ErrorService.Application.csproj ./ErrorService/ErrorService.Application/
COPY ErrorService/ErrorService.Domain/ErrorService.Domain.csproj ./ErrorService/ErrorService.Domain/
COPY ErrorService/ErrorService.Infrastructure/ErrorService.Infrastructure.csproj ./ErrorService/ErrorService.Infrastructure/
COPY ErrorService/ErrorService.Shared/ErrorService.Shared.csproj ./ErrorService/ErrorService.Shared/

# Copy shared project files
COPY _Shared/CarDealer.Contracts/*.csproj ./_Shared/CarDealer.Contracts/
COPY _Shared/CarDealer.Shared/*.csproj ./_Shared/CarDealer.Shared/

# Copy ServiceDiscovery project files
COPY ServiceDiscovery/ServiceDiscovery.Application/*.csproj ./ServiceDiscovery/ServiceDiscovery.Application/
COPY ServiceDiscovery/ServiceDiscovery.Infrastructure/*.csproj ./ServiceDiscovery/ServiceDiscovery.Infrastructure/
COPY ServiceDiscovery/ServiceDiscovery.Domain/*.csproj ./ServiceDiscovery/ServiceDiscovery.Domain/

# Restore packages
RUN dotnet restore "ErrorService/ErrorService.Api/ErrorService.Api.csproj"

# Copy all source code
COPY . .

# Build in Release mode
RUN dotnet build "ErrorService/ErrorService.Api/ErrorService.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "ErrorService/ErrorService.Api/ErrorService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Stage 3: Runtime (slim image)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Security: run as non-root
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Copy published app
COPY --from=publish /app/publish .

# Production environment
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:80

EXPOSE 80

ENTRYPOINT ["dotnet", "ErrorService.Api.dll"]
