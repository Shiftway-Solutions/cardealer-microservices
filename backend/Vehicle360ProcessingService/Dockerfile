# syntax=docker/dockerfile:1
# Stage 1: Build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy shared libraries first
COPY ["_Shared/CarDealer.Shared/CarDealer.Shared.csproj", "_Shared/CarDealer.Shared/"]
COPY ["_Shared/CarDealer.Contracts/CarDealer.Contracts.csproj", "_Shared/CarDealer.Contracts/"]

# Copy project files
COPY ["Vehicle360ProcessingService/Vehicle360ProcessingService.Api/Vehicle360ProcessingService.Api.csproj", "Vehicle360ProcessingService/Vehicle360ProcessingService.Api/"]
COPY ["Vehicle360ProcessingService/Vehicle360ProcessingService.Application/Vehicle360ProcessingService.Application.csproj", "Vehicle360ProcessingService/Vehicle360ProcessingService.Application/"]
COPY ["Vehicle360ProcessingService/Vehicle360ProcessingService.Infrastructure/Vehicle360ProcessingService.Infrastructure.csproj", "Vehicle360ProcessingService/Vehicle360ProcessingService.Infrastructure/"]
COPY ["Vehicle360ProcessingService/Vehicle360ProcessingService.Domain/Vehicle360ProcessingService.Domain.csproj", "Vehicle360ProcessingService/Vehicle360ProcessingService.Domain/"]

# Restore packages
RUN dotnet restore "Vehicle360ProcessingService/Vehicle360ProcessingService.Api/Vehicle360ProcessingService.Api.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/Vehicle360ProcessingService/Vehicle360ProcessingService.Api"
RUN dotnet build "Vehicle360ProcessingService.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "Vehicle360ProcessingService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy published files
RUN addgroup -g 1000 appuser && adduser -D -u 1000 -G appuser appuser
USER appuser
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "Vehicle360ProcessingService.Api.dll"]
