# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080

# Create secrets directory and set environment
RUN mkdir -p /run/secrets
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

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

COPY ["IntegrationService/IntegrationService.Api/IntegrationService.Api.csproj", "IntegrationService/IntegrationService.Api/"]
COPY ["IntegrationService/IntegrationService.Application/IntegrationService.Application.csproj", "IntegrationService/IntegrationService.Application/"]
COPY ["IntegrationService/IntegrationService.Domain/IntegrationService.Domain.csproj", "IntegrationService/IntegrationService.Domain/"]
COPY ["IntegrationService/IntegrationService.Infrastructure/IntegrationService.Infrastructure.csproj", "IntegrationService/IntegrationService.Infrastructure/"]
RUN dotnet restore "IntegrationService/IntegrationService.Api/IntegrationService.Api.csproj"
COPY . .
WORKDIR "/src/IntegrationService/IntegrationService.Api"
RUN dotnet build "IntegrationService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IntegrationService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN addgroup -g 1000 appuser && adduser -D -u 1000 -G appuser appuser
USER appuser
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "IntegrationService.Api.dll"]
