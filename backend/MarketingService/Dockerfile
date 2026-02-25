# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080

# Install wget for healthcheck
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
COPY ["MarketingService/MarketingService.Api/MarketingService.Api.csproj", "MarketingService/MarketingService.Api/"]
COPY ["MarketingService/MarketingService.Application/MarketingService.Application.csproj", "MarketingService/MarketingService.Application/"]
COPY ["MarketingService/MarketingService.Domain/MarketingService.Domain.csproj", "MarketingService/MarketingService.Domain/"]
COPY ["MarketingService/MarketingService.Infrastructure/MarketingService.Infrastructure.csproj", "MarketingService/MarketingService.Infrastructure/"]
COPY ["_Shared/CarDealer.Shared/CarDealer.Shared.csproj", "_Shared/CarDealer.Shared/"]
COPY ["_Shared/CarDealer.Contracts/CarDealer.Contracts.csproj", "_Shared/CarDealer.Contracts/"]
RUN dotnet restore "MarketingService/MarketingService.Api/MarketingService.Api.csproj"
COPY . .
WORKDIR "/src/MarketingService/MarketingService.Api"
RUN dotnet build "MarketingService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MarketingService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Set Docker environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

RUN addgroup -g 1000 appuser && adduser -D -u 1000 -G appuser appuser
USER appuser
COPY --from=publish /app/publish .

# Healthcheck
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "MarketingService.Api.dll"]
