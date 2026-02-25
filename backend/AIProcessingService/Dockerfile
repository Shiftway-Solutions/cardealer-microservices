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
COPY ["AIProcessingService/AIProcessingService.Api/AIProcessingService.Api.csproj", "AIProcessingService/AIProcessingService.Api/"]
COPY ["AIProcessingService/AIProcessingService.Application/AIProcessingService.Application.csproj", "AIProcessingService/AIProcessingService.Application/"]
COPY ["AIProcessingService/AIProcessingService.Infrastructure/AIProcessingService.Infrastructure.csproj", "AIProcessingService/AIProcessingService.Infrastructure/"]
COPY ["AIProcessingService/AIProcessingService.Domain/AIProcessingService.Domain.csproj", "AIProcessingService/AIProcessingService.Domain/"]

RUN dotnet restore "AIProcessingService/AIProcessingService.Api/AIProcessingService.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/AIProcessingService/AIProcessingService.Api"
RUN dotnet build "AIProcessingService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AIProcessingService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

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

ENTRYPOINT ["dotnet", "AIProcessingService.Api.dll"]
