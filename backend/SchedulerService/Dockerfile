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

# Copy csproj files
COPY ["SchedulerService/SchedulerService.Api/SchedulerService.Api.csproj", "SchedulerService/SchedulerService.Api/"]
COPY ["SchedulerService/SchedulerService.Infrastructure/SchedulerService.Infrastructure.csproj", "SchedulerService/SchedulerService.Infrastructure/"]
COPY ["SchedulerService/SchedulerService.Application/SchedulerService.Application.csproj", "SchedulerService/SchedulerService.Application/"]
COPY ["SchedulerService/SchedulerService.Domain/SchedulerService.Domain.csproj", "SchedulerService/SchedulerService.Domain/"]

# Restore dependencies
RUN dotnet restore "SchedulerService/SchedulerService.Api/SchedulerService.Api.csproj"

# Copy everything else
COPY _Shared/ ./_Shared/
COPY SchedulerService/ ./SchedulerService/

# Build
WORKDIR "/src/SchedulerService/SchedulerService.Api"
RUN dotnet build "SchedulerService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SchedulerService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080

# Install wget for healthcheck (Alpine uses apk)
RUN apk add --no-cache wget

# Create secrets directory and non-root user (Alpine uses addgroup/adduser)
RUN addgroup -g 1000 appuser && adduser -D -u 1000 -G appuser appuser && \
    mkdir -p /run/secrets && \
    chown -R appuser:appuser /app /run/secrets

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080

USER appuser
COPY --from=publish --chown=appuser:appuser /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "SchedulerService.Api.dll"]
