# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

COPY ["AdvertisingService/AdvertisingService.Api/AdvertisingService.Api.csproj", "AdvertisingService/AdvertisingService.Api/"]
COPY ["AdvertisingService/AdvertisingService.Application/AdvertisingService.Application.csproj", "AdvertisingService/AdvertisingService.Application/"]
COPY ["AdvertisingService/AdvertisingService.Domain/AdvertisingService.Domain.csproj", "AdvertisingService/AdvertisingService.Domain/"]
COPY ["AdvertisingService/AdvertisingService.Infrastructure/AdvertisingService.Infrastructure.csproj", "AdvertisingService/AdvertisingService.Infrastructure/"]
COPY ["_Shared/CarDealer.Shared/CarDealer.Shared.csproj", "_Shared/CarDealer.Shared/"]
COPY ["_Shared/CarDealer.Contracts/CarDealer.Contracts.csproj", "_Shared/CarDealer.Contracts/"]
COPY ["_Shared/CarDealer.Shared.Logging/CarDealer.Shared.Logging.csproj", "_Shared/CarDealer.Shared.Logging/"]
COPY ["_Shared/CarDealer.Shared.ErrorHandling/CarDealer.Shared.ErrorHandling.csproj", "_Shared/CarDealer.Shared.ErrorHandling/"]
COPY ["_Shared/CarDealer.Shared.Observability/CarDealer.Shared.Observability.csproj", "_Shared/CarDealer.Shared.Observability/"]
COPY ["_Shared/CarDealer.Shared.Audit/CarDealer.Shared.Audit.csproj", "_Shared/CarDealer.Shared.Audit/"]

RUN dotnet restore "AdvertisingService/AdvertisingService.Api/AdvertisingService.Api.csproj"

COPY . .
WORKDIR "/src/AdvertisingService/AdvertisingService.Api"

FROM build AS publish
RUN dotnet publish "AdvertisingService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080

RUN mkdir -p /run/secrets
RUN addgroup -g 1000 appuser && \
    adduser -D -u 1000 -G appuser appuser && \
    chown -R appuser:appuser /app /run/secrets

USER appuser
COPY --from=publish --chown=appuser:appuser /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "AdvertisingService.Api.dll"]

# CI build trigger 2026-03-03
