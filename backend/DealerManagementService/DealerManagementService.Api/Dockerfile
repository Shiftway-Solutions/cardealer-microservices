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

COPY ["DealerManagementService/DealerManagementService.Api/DealerManagementService.Api.csproj", "DealerManagementService/DealerManagementService.Api/"]
COPY ["DealerManagementService/DealerManagementService.Application/DealerManagementService.Application.csproj", "DealerManagementService/DealerManagementService.Application/"]
COPY ["DealerManagementService/DealerManagementService.Domain/DealerManagementService.Domain.csproj", "DealerManagementService/DealerManagementService.Domain/"]
COPY ["DealerManagementService/DealerManagementService.Infrastructure/DealerManagementService.Infrastructure.csproj", "DealerManagementService/DealerManagementService.Infrastructure/"]

RUN dotnet restore "DealerManagementService/DealerManagementService.Api/DealerManagementService.Api.csproj"

COPY DealerManagementService/ DealerManagementService/

WORKDIR "/src/DealerManagementService/DealerManagementService.Api"
RUN dotnet build "DealerManagementService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DealerManagementService.Api.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "DealerManagementService.Api.dll"]
