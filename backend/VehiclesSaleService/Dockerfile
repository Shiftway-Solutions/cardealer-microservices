# syntax=docker/dockerfile:1
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared projects first
COPY ["_Shared/CarDealer.Shared/CarDealer.Shared.csproj", "_Shared/CarDealer.Shared/"]
COPY ["_Shared/CarDealer.Contracts/CarDealer.Contracts.csproj", "_Shared/CarDealer.Contracts/"]

# Copy csproj files and restore
COPY ["VehiclesSaleService/VehiclesSaleService.Api/VehiclesSaleService.Api.csproj", "VehiclesSaleService/VehiclesSaleService.Api/"]
COPY ["VehiclesSaleService/VehiclesSaleService.Application/VehiclesSaleService.Application.csproj", "VehiclesSaleService/VehiclesSaleService.Application/"]
COPY ["VehiclesSaleService/VehiclesSaleService.Domain/VehiclesSaleService.Domain.csproj", "VehiclesSaleService/VehiclesSaleService.Domain/"]
COPY ["VehiclesSaleService/VehiclesSaleService.Infrastructure/VehiclesSaleService.Infrastructure.csproj", "VehiclesSaleService/VehiclesSaleService.Infrastructure/"]
COPY ["VehiclesSaleService/VehiclesSaleService.Shared/VehiclesSaleService.Shared.csproj", "VehiclesSaleService/VehiclesSaleService.Shared/"]

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages dotnet restore "VehiclesSaleService/VehiclesSaleService.Api/VehiclesSaleService.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/VehiclesSaleService/VehiclesSaleService.Api"
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages dotnet build "VehiclesSaleService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages dotnet publish "VehiclesSaleService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080

# Create non-root user and secrets directory (Alpine syntax)
RUN addgroup -g 1000 appuser && \
    adduser -D -u 1000 -G appuser appuser && \
    mkdir -p /run/secrets && \
    chown -R appuser:appuser /app /run/secrets

# Install wget for healthcheck
RUN apk add --no-cache wget

# Set Docker environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

USER appuser

COPY --from=publish --chown=appuser:appuser /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "VehiclesSaleService.Api.dll"]


# Wed Feb 25 09:48:42 AST 2026
# trigger 1772028282
