############################################
# PRODUCTION BUILD - BillingService Multi-stage Dockerfile
############################################

# Stage 1: Build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy shared projects first
COPY _Shared/ _Shared/

# Copy service projects
COPY BillingService/BillingService.Domain/ BillingService/BillingService.Domain/
COPY BillingService/BillingService.Application/ BillingService/BillingService.Application/
COPY BillingService/BillingService.Infrastructure/ BillingService/BillingService.Infrastructure/
COPY BillingService/BillingService.Api/ BillingService/BillingService.Api/

# Restore dependencies
WORKDIR /src/BillingService/BillingService.Api
RUN dotnet restore

# Build
RUN dotnet build -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Security: run as non-root
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Copy published app
COPY --from=publish /app/publish .

# Environment
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:80

EXPOSE 80

ENTRYPOINT ["dotnet", "BillingService.Api.dll"]
