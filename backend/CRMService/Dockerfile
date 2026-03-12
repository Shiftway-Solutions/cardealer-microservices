# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080

# Create secrets directory for Docker secrets
RUN mkdir -p /run/secrets

# Install wget for healthcheck (Alpine uses apk)
RUN apk add --no-cache wget

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
COPY ["_Shared/CarDealer.Shared.Audit/CarDealer.Shared.Audit.csproj", "_Shared/CarDealer.Shared.Audit/"]
COPY ["_Shared/CarDealer.Shared.Logging/CarDealer.Shared.Logging.csproj", "_Shared/CarDealer.Shared.Logging/"]
COPY ["_Shared/CarDealer.Shared.Observability/CarDealer.Shared.Observability.csproj", "_Shared/CarDealer.Shared.Observability/"]
COPY ["_Shared/CarDealer.Shared.ErrorHandling/CarDealer.Shared.ErrorHandling.csproj", "_Shared/CarDealer.Shared.ErrorHandling/"]

# Copy project files
COPY ["CRMService/CRMService.Api/CRMService.Api.csproj", "CRMService/CRMService.Api/"]
COPY ["CRMService/CRMService.Application/CRMService.Application.csproj", "CRMService/CRMService.Application/"]
COPY ["CRMService/CRMService.Domain/CRMService.Domain.csproj", "CRMService/CRMService.Domain/"]
COPY ["CRMService/CRMService.Infrastructure/CRMService.Infrastructure.csproj", "CRMService/CRMService.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "CRMService/CRMService.Api/CRMService.Api.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/CRMService/CRMService.Api"
RUN dotnet build "CRMService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CRMService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Set environment to Docker
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080

RUN addgroup -g 1000 appuser && adduser -D -u 1000 -G appuser appuser
USER appuser
COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CRMService.Api.dll"]
