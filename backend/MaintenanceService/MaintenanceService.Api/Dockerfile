FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false
COPY ["backend/MaintenanceService/MaintenanceService.Api/MaintenanceService.Api.csproj", "backend/MaintenanceService/MaintenanceService.Api/"]
COPY ["backend/MaintenanceService/MaintenanceService.Domain/MaintenanceService.Domain.csproj", "backend/MaintenanceService/MaintenanceService.Domain/"]
COPY ["backend/MaintenanceService/MaintenanceService.Infrastructure/MaintenanceService.Infrastructure.csproj", "backend/MaintenanceService/MaintenanceService.Infrastructure/"]
RUN dotnet restore "backend/MaintenanceService/MaintenanceService.Api/MaintenanceService.Api.csproj"
COPY . .
WORKDIR "/src/backend/MaintenanceService/MaintenanceService.Api"
RUN dotnet build "MaintenanceService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MaintenanceService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MaintenanceService.Api.dll"]
