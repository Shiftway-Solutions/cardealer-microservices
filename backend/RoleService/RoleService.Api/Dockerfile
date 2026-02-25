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
COPY RoleService/RoleService.Api/*.csproj ./RoleService/RoleService.Api/
COPY RoleService/RoleService.Application/*.csproj ./RoleService/RoleService.Application/
COPY RoleService/RoleService.Domain/*.csproj ./RoleService/RoleService.Domain/
COPY RoleService/RoleService.Infrastructure/*.csproj ./RoleService/RoleService.Infrastructure/
COPY RoleService/RoleService.Shared/*.csproj ./RoleService/RoleService.Shared/

# Copy shared project files
COPY _Shared/CarDealer.Contracts/*.csproj ./_Shared/CarDealer.Contracts/
COPY _Shared/CarDealer.Shared/*.csproj ./_Shared/CarDealer.Shared/

# Copy ServiceDiscovery project files
COPY ServiceDiscovery/ServiceDiscovery.Application/*.csproj ./ServiceDiscovery/ServiceDiscovery.Application/
COPY ServiceDiscovery/ServiceDiscovery.Infrastructure/*.csproj ./ServiceDiscovery/ServiceDiscovery.Infrastructure/
COPY ServiceDiscovery/ServiceDiscovery.Domain/*.csproj ./ServiceDiscovery/ServiceDiscovery.Domain/

# Restore packages
RUN dotnet restore "RoleService/RoleService.Api/RoleService.Api.csproj"

# Copy all source code
COPY . .

# Build in Release mode
RUN dotnet build "RoleService/RoleService.Api/RoleService.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "RoleService/RoleService.Api/RoleService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

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

ENTRYPOINT ["dotnet", "RoleService.Api.dll"]
