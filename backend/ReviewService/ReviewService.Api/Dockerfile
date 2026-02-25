# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
EXPOSE 8080

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Speed up dotnet restore and prevent network hangs
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1 \
    NUGET_XMLDOC_MODE=skip \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false
COPY ["backend/ReviewService/ReviewService.Api/ReviewService.Api.csproj", "backend/ReviewService/ReviewService.Api/"]
COPY ["backend/ReviewService/ReviewService.Application/ReviewService.Application.csproj", "backend/ReviewService/ReviewService.Application/"]
COPY ["backend/ReviewService/ReviewService.Domain/ReviewService.Domain.csproj", "backend/ReviewService/ReviewService.Domain/"]
COPY ["backend/_Shared/CarDealer.Shared/CarDealer.Shared.csproj", "backend/_Shared/CarDealer.Shared/"]
COPY ["backend/_Shared/CarDealer.Contracts/CarDealer.Contracts.csproj", "backend/_Shared/CarDealer.Contracts/"]
COPY ["backend/ReviewService/ReviewService.Infrastructure/ReviewService.Infrastructure.csproj", "backend/ReviewService/ReviewService.Infrastructure/"]
RUN dotnet restore "./backend/ReviewService/ReviewService.Api/ReviewService.Api.csproj"
COPY . .
WORKDIR "/src/backend/ReviewService/ReviewService.Api"
RUN dotnet build "./ReviewService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ReviewService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReviewService.Api.dll"]