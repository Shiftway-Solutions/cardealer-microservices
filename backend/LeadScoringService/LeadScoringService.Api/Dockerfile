# Stage 1: Build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy csproj files and restore dependencies
COPY ["LeadScoringService.Api/LeadScoringService.Api.csproj", "LeadScoringService.Api/"]
COPY ["LeadScoringService.Application/LeadScoringService.Application.csproj", "LeadScoringService.Application/"]
COPY ["LeadScoringService.Domain/LeadScoringService.Domain.csproj", "LeadScoringService.Domain/"]
COPY ["LeadScoringService.Infrastructure/LeadScoringService.Infrastructure.csproj", "LeadScoringService.Infrastructure/"]

RUN dotnet restore "LeadScoringService.Api/LeadScoringService.Api.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/LeadScoringService.Api"
RUN dotnet build "LeadScoringService.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "LeadScoringService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "LeadScoringService.Api.dll"]
