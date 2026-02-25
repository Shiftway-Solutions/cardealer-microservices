# Stage 1: Build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy csproj files y restore dependencies
COPY ["EventTrackingService.Api/EventTrackingService.Api.csproj", "EventTrackingService.Api/"]
COPY ["EventTrackingService.Application/EventTrackingService.Application.csproj", "EventTrackingService.Application/"]
COPY ["EventTrackingService.Domain/EventTrackingService.Domain.csproj", "EventTrackingService.Domain/"]
COPY ["EventTrackingService.Infrastructure/EventTrackingService.Infrastructure.csproj", "EventTrackingService.Infrastructure/"]

RUN dotnet restore "EventTrackingService.Api/EventTrackingService.Api.csproj"

# Copy source code
COPY . .

WORKDIR "/src/EventTrackingService.Api"
RUN dotnet build "EventTrackingService.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "EventTrackingService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Expose port 8080 (Kubernetes standard)
EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "EventTrackingService.Api.dll"]
