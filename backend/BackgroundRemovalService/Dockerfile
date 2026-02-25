# syntax=docker/dockerfile:1
# Build stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy solution and project files
COPY ["backend/BackgroundRemovalService/BackgroundRemovalService.Api/BackgroundRemovalService.Api.csproj", "BackgroundRemovalService/BackgroundRemovalService.Api/"]
COPY ["backend/BackgroundRemovalService/BackgroundRemovalService.Application/BackgroundRemovalService.Application.csproj", "BackgroundRemovalService/BackgroundRemovalService.Application/"]
COPY ["backend/BackgroundRemovalService/BackgroundRemovalService.Domain/BackgroundRemovalService.Domain.csproj", "BackgroundRemovalService/BackgroundRemovalService.Domain/"]
COPY ["backend/BackgroundRemovalService/BackgroundRemovalService.Infrastructure/BackgroundRemovalService.Infrastructure.csproj", "BackgroundRemovalService/BackgroundRemovalService.Infrastructure/"]
COPY ["backend/_Shared/CarDealer.Contracts/CarDealer.Contracts.csproj", "_Shared/CarDealer.Contracts/"]
COPY ["backend/_Shared/CarDealer.Shared/CarDealer.Shared.csproj", "_Shared/CarDealer.Shared/"]
COPY ["backend/_Shared/CarDealer.Shared.Logging/CarDealer.Shared.Logging.csproj", "_Shared/CarDealer.Shared.Logging/"]
COPY ["backend/_Shared/CarDealer.Shared.ErrorHandling/CarDealer.Shared.ErrorHandling.csproj", "_Shared/CarDealer.Shared.ErrorHandling/"]

# Restore dependencies
RUN dotnet restore "BackgroundRemovalService/BackgroundRemovalService.Api/BackgroundRemovalService.Api.csproj"

# Copy source code
COPY backend/BackgroundRemovalService/ BackgroundRemovalService/
COPY backend/_Shared/ _Shared/

# Build and publish
WORKDIR /src/BackgroundRemovalService/BackgroundRemovalService.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser

# Copy published app
COPY --from=build /app/publish .

# Create media directory
RUN mkdir -p /app/media-files && chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "BackgroundRemovalService.Api.dll"]
