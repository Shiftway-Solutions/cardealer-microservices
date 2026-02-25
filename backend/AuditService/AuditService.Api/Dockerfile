# Build stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy csproj files and restore
COPY ["AuditService/AuditService.Api/AuditService.Api.csproj", "AuditService/AuditService.Api/"]
COPY ["AuditService/AuditService.Application/AuditService.Application.csproj", "AuditService/AuditService.Application/"]
COPY ["AuditService/AuditService.Domain/AuditService.Domain.csproj", "AuditService/AuditService.Domain/"]
COPY ["AuditService/AuditService.Infrastructure/AuditService.Infrastructure.csproj", "AuditService/AuditService.Infrastructure/"]

RUN dotnet restore "AuditService/AuditService.Api/AuditService.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/AuditService/AuditService.Api"
RUN dotnet build "AuditService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AuditService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Create non-root user
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

COPY --from=publish --chown=appuser:appuser /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "AuditService.Api.dll"]
