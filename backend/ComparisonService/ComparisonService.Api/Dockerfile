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
COPY ["backend/ComparisonService/ComparisonService.Api/ComparisonService.Api.csproj", "backend/ComparisonService/ComparisonService.Api/"]
COPY ["backend/ComparisonService/ComparisonService.Domain/ComparisonService.Domain.csproj", "backend/ComparisonService/ComparisonService.Domain/"]
COPY ["backend/ComparisonService/ComparisonService.Infrastructure/ComparisonService.Infrastructure.csproj", "backend/ComparisonService/ComparisonService.Infrastructure/"]
RUN dotnet restore "backend/ComparisonService/ComparisonService.Api/ComparisonService.Api.csproj"
COPY . .
WORKDIR "/src/backend/ComparisonService/ComparisonService.Api"
RUN dotnet build "ComparisonService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ComparisonService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ComparisonService.Api.dll"]
