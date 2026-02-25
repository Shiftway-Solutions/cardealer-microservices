# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
# Install curl for health checks
RUN apk add --no-cache curl

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy shared project files first (dependencies)
COPY ["_Shared/CarDealer.Contracts/CarDealer.Contracts.csproj", "_Shared/CarDealer.Contracts/"]
COPY ["_Shared/CarDealer.Shared/CarDealer.Shared.csproj", "_Shared/CarDealer.Shared/"]
COPY ["ErrorService/ErrorService.Domain/ErrorService.Domain.csproj", "ErrorService/ErrorService.Domain/"]
COPY ["ErrorService/ErrorService.Shared/ErrorService.Shared.csproj", "ErrorService/ErrorService.Shared/"]

# Copy ChatbotService project files
COPY ["ChatbotService/ChatbotService.Api/ChatbotService.Api.csproj", "ChatbotService/ChatbotService.Api/"]
COPY ["ChatbotService/ChatbotService.Application/ChatbotService.Application.csproj", "ChatbotService/ChatbotService.Application/"]
COPY ["ChatbotService/ChatbotService.Domain/ChatbotService.Domain.csproj", "ChatbotService/ChatbotService.Domain/"]
COPY ["ChatbotService/ChatbotService.Infrastructure/ChatbotService.Infrastructure.csproj", "ChatbotService/ChatbotService.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "ChatbotService/ChatbotService.Api/ChatbotService.Api.csproj"

# Copy all source code
COPY _Shared/ _Shared/
COPY ErrorService/ErrorService.Domain/ ErrorService/ErrorService.Domain/
COPY ErrorService/ErrorService.Shared/ ErrorService/ErrorService.Shared/
COPY ChatbotService/ ChatbotService/

# Build
WORKDIR "/src/ChatbotService/ChatbotService.Api"
RUN dotnet build "ChatbotService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChatbotService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN addgroup -g 1000 appuser && adduser -D -u 1000 -G appuser appuser
COPY --from=publish /app/publish .
RUN mkdir -p /app/logs && chown -R appuser:appuser /app/logs
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ChatbotService.Api.dll"]
