FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy csproj files and restore
COPY ["VehicleIntelligenceService/VehicleIntelligenceService.Api/VehicleIntelligenceService.Api.csproj", "VehicleIntelligenceService/VehicleIntelligenceService.Api/"]
COPY ["VehicleIntelligenceService/VehicleIntelligenceService.Application/VehicleIntelligenceService.Application.csproj", "VehicleIntelligenceService/VehicleIntelligenceService.Application/"]
COPY ["VehicleIntelligenceService/VehicleIntelligenceService.Domain/VehicleIntelligenceService.Domain.csproj", "VehicleIntelligenceService/VehicleIntelligenceService.Domain/"]
COPY ["VehicleIntelligenceService/VehicleIntelligenceService.Infrastructure/VehicleIntelligenceService.Infrastructure.csproj", "VehicleIntelligenceService/VehicleIntelligenceService.Infrastructure/"]

RUN dotnet restore "VehicleIntelligenceService/VehicleIntelligenceService.Api/VehicleIntelligenceService.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/VehicleIntelligenceService/VehicleIntelligenceService.Api"
RUN dotnet build "VehicleIntelligenceService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VehicleIntelligenceService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VehicleIntelligenceService.Api.dll"]
