# syntax=docker/dockerfile:1
# ComplianceService - Servicio de Cumplimiento Regulatorio
# Multi-stage build para optimización

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copiar archivos de proyecto para restaurar dependencias
COPY ["ComplianceService.Api/ComplianceService.Api.csproj", "ComplianceService.Api/"]
COPY ["ComplianceService.Application/ComplianceService.Application.csproj", "ComplianceService.Application/"]
COPY ["ComplianceService.Domain/ComplianceService.Domain.csproj", "ComplianceService.Domain/"]
COPY ["ComplianceService.Infrastructure/ComplianceService.Infrastructure.csproj", "ComplianceService.Infrastructure/"]

RUN dotnet restore "ComplianceService.Api/ComplianceService.Api.csproj"

# Copiar todo el código fuente
COPY . .

# Build de la aplicación
WORKDIR "/src/ComplianceService.Api"
RUN dotnet build "ComplianceService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ComplianceService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN addgroup -g 1000 appuser && adduser -D -u 1000 -G appuser appuser
USER appuser
COPY --from=publish /app/publish .

# Metadata
LABEL maintainer="OKLA Compliance Team <compliance@okla.com.do>"
LABEL version="1.0.0"
LABEL description="Servicio de Cumplimiento Regulatorio - Ley 155-17, Ley 172-13, Ley 126-02"

ENTRYPOINT ["dotnet", "ComplianceService.Api.dll"]
