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

# Copy csproj files and restore
COPY ["DataProtectionService/DataProtectionService.Api/DataProtectionService.Api.csproj", "DataProtectionService/DataProtectionService.Api/"]
COPY ["DataProtectionService/DataProtectionService.Application/DataProtectionService.Application.csproj", "DataProtectionService/DataProtectionService.Application/"]
COPY ["DataProtectionService/DataProtectionService.Domain/DataProtectionService.Domain.csproj", "DataProtectionService/DataProtectionService.Domain/"]
COPY ["DataProtectionService/DataProtectionService.Infrastructure/DataProtectionService.Infrastructure.csproj", "DataProtectionService/DataProtectionService.Infrastructure/"]

RUN dotnet restore "DataProtectionService/DataProtectionService.Api/DataProtectionService.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/DataProtectionService/DataProtectionService.Api"
RUN dotnet build "DataProtectionService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DataProtectionService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080

RUN addgroup -g 1000 appuser && \
    adduser -D -u 1000 -G appuser appuser && \
    mkdir -p /run/secrets && \
    chown -R appuser:appuser /app /run/secrets

RUN apk add --no-cache wget

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

USER appuser
COPY --from=publish --chown=appuser:appuser /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "DataProtectionService.Api.dll"]
