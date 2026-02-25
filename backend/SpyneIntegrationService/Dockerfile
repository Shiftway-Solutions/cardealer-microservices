# syntax=docker/dockerfile:1
# SpyneIntegrationService
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy project files
COPY ["SpyneIntegrationService.Api/SpyneIntegrationService.Api.csproj", "SpyneIntegrationService.Api/"]
COPY ["SpyneIntegrationService.Application/SpyneIntegrationService.Application.csproj", "SpyneIntegrationService.Application/"]
COPY ["SpyneIntegrationService.Domain/SpyneIntegrationService.Domain.csproj", "SpyneIntegrationService.Domain/"]
COPY ["SpyneIntegrationService.Infrastructure/SpyneIntegrationService.Infrastructure.csproj", "SpyneIntegrationService.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "SpyneIntegrationService.Api/SpyneIntegrationService.Api.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/SpyneIntegrationService.Api"
RUN dotnet build "SpyneIntegrationService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SpyneIntegrationService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN addgroup -g 1000 appuser && adduser -D -u 1000 -G appuser appuser
USER appuser
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "SpyneIntegrationService.Api.dll"]
