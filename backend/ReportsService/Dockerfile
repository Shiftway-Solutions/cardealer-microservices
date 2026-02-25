# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080

# Install wget for healthcheck (Alpine uses apk)
RUN apk add --no-cache wget

# Create secrets directory
RUN mkdir -p /run/secrets

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

# Copy service projects
COPY ["ReportsService/ReportsService.Api/ReportsService.Api.csproj", "ReportsService/ReportsService.Api/"]
COPY ["ReportsService/ReportsService.Application/ReportsService.Application.csproj", "ReportsService/ReportsService.Application/"]
COPY ["ReportsService/ReportsService.Domain/ReportsService.Domain.csproj", "ReportsService/ReportsService.Domain/"]
COPY ["ReportsService/ReportsService.Infrastructure/ReportsService.Infrastructure.csproj", "ReportsService/ReportsService.Infrastructure/"]

RUN dotnet restore "ReportsService/ReportsService.Api/ReportsService.Api.csproj"

# Copy shared source code
COPY ["_Shared/", "_Shared/"]

# Copy service source code
COPY ["ReportsService/", "ReportsService/"]

WORKDIR "/src/ReportsService/ReportsService.Api"
RUN dotnet build "ReportsService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ReportsService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080

RUN addgroup -g 1000 appuser && adduser -D -u 1000 -G appuser appuser
USER appuser
COPY --from=publish /app/publish .

# Healthcheck
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ReportsService.Api.dll"]
