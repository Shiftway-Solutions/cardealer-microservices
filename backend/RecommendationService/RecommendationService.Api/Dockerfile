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

COPY ["RecommendationService/RecommendationService.Api/RecommendationService.Api.csproj", "RecommendationService/RecommendationService.Api/"]
COPY ["RecommendationService/RecommendationService.Application/RecommendationService.Application.csproj", "RecommendationService/RecommendationService.Application/"]
COPY ["RecommendationService/RecommendationService.Domain/RecommendationService.Domain.csproj", "RecommendationService/RecommendationService.Domain/"]
COPY ["RecommendationService/RecommendationService.Infrastructure/RecommendationService.Infrastructure.csproj", "RecommendationService/RecommendationService.Infrastructure/"]

RUN dotnet restore "RecommendationService/RecommendationService.Api/RecommendationService.Api.csproj"

COPY . .
WORKDIR "/src/RecommendationService/RecommendationService.Api"
RUN dotnet build "RecommendationService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RecommendationService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RecommendationService.Api.dll"]
