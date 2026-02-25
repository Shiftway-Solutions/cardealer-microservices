# syntax=docker/dockerfile:1
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

# Copy project files
COPY ["Video360Service.Api/Video360Service.Api.csproj", "Video360Service.Api/"]
COPY ["Video360Service.Application/Video360Service.Application.csproj", "Video360Service.Application/"]
COPY ["Video360Service.Domain/Video360Service.Domain.csproj", "Video360Service.Domain/"]
COPY ["Video360Service.Infrastructure/Video360Service.Infrastructure.csproj", "Video360Service.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Video360Service.Api/Video360Service.Api.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/Video360Service.Api"
RUN dotnet build "Video360Service.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Video360Service.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Video360Service.Api.dll"]
