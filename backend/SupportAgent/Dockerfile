# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy shared libraries
COPY ["_Shared/CarDealer.Shared/CarDealer.Shared.csproj", "_Shared/CarDealer.Shared/"]
COPY ["_Shared/CarDealer.Contracts/CarDealer.Contracts.csproj", "_Shared/CarDealer.Contracts/"]
COPY ["_Shared/CarDealer.Shared.Logging/CarDealer.Shared.Logging.csproj", "_Shared/CarDealer.Shared.Logging/"]
COPY ["_Shared/CarDealer.Shared.ErrorHandling/CarDealer.Shared.ErrorHandling.csproj", "_Shared/CarDealer.Shared.ErrorHandling/"]
COPY ["_Shared/CarDealer.Shared.Observability/CarDealer.Shared.Observability.csproj", "_Shared/CarDealer.Shared.Observability/"]
COPY ["_Shared/CarDealer.Shared.Audit/CarDealer.Shared.Audit.csproj", "_Shared/CarDealer.Shared.Audit/"]
COPY ["_Shared/CarDealer.Shared.Resilience/CarDealer.Shared.Resilience.csproj", "_Shared/CarDealer.Shared.Resilience/"]

# Copy SupportAgent projects
COPY ["SupportAgent/SupportAgent.Api/SupportAgent.Api.csproj", "SupportAgent/SupportAgent.Api/"]
COPY ["SupportAgent/SupportAgent.Application/SupportAgent.Application.csproj", "SupportAgent/SupportAgent.Application/"]
COPY ["SupportAgent/SupportAgent.Domain/SupportAgent.Domain.csproj", "SupportAgent/SupportAgent.Domain/"]
COPY ["SupportAgent/SupportAgent.Infrastructure/SupportAgent.Infrastructure.csproj", "SupportAgent/SupportAgent.Infrastructure/"]

RUN dotnet restore "SupportAgent/SupportAgent.Api/SupportAgent.Api.csproj"

COPY . .
WORKDIR "/src/SupportAgent/SupportAgent.Api"

FROM build AS publish
RUN dotnet publish "SupportAgent.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080

RUN apk add --no-cache wget
RUN mkdir -p /run/secrets

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

RUN adduser -D -u 1000 appuser && chown -R appuser:appuser /app /run/secrets
USER appuser

COPY --from=publish --chown=appuser:appuser /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "SupportAgent.Api.dll"]
