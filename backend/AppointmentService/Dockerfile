# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080

# Install wget for healthcheck (Alpine uses apk)
RUN apk add --no-cache wget

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false
COPY ["AppointmentService/AppointmentService.Api/AppointmentService.Api.csproj", "AppointmentService/AppointmentService.Api/"]
COPY ["AppointmentService/AppointmentService.Application/AppointmentService.Application.csproj", "AppointmentService/AppointmentService.Application/"]
COPY ["AppointmentService/AppointmentService.Domain/AppointmentService.Domain.csproj", "AppointmentService/AppointmentService.Domain/"]
COPY ["AppointmentService/AppointmentService.Infrastructure/AppointmentService.Infrastructure.csproj", "AppointmentService/AppointmentService.Infrastructure/"]
COPY ["_Shared/CarDealer.Shared/CarDealer.Shared.csproj", "_Shared/CarDealer.Shared/"]
COPY ["_Shared/CarDealer.Contracts/CarDealer.Contracts.csproj", "_Shared/CarDealer.Contracts/"]
RUN dotnet restore "AppointmentService/AppointmentService.Api/AppointmentService.Api.csproj"
COPY . .
WORKDIR "/src/AppointmentService/AppointmentService.Api"
RUN dotnet build "AppointmentService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AppointmentService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Create secrets directory for Docker secrets
RUN mkdir -p /run/secrets

# Set environment to Docker
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080

RUN addgroup -g 1000 appuser && adduser -D -u 1000 -G appuser appuser
USER appuser
COPY --from=publish /app/publish .

# Healthcheck
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "AppointmentService.Api.dll"]
