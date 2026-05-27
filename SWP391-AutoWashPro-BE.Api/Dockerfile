FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SWP391-AutoWashPro-BE.Api/SWP391-AutoWashPro-BE.Api.csproj", "SWP391-AutoWashPro-BE.Api/"]
COPY ["SWP391-AutoWashPro-BE.Service/SWP391-AutoWashPro-BE.Service.csproj", "SWP391-AutoWashPro-BE.Service/"]
COPY ["SWP391-AutoWashPro-BE.Repository/SWP391-AutoWashPro-BE.Repository.csproj", "SWP391-AutoWashPro-BE.Repository/"]
RUN dotnet restore "SWP391-AutoWashPro-BE.Api/SWP391-AutoWashPro-BE.Api.csproj"
COPY . .
WORKDIR "/src/SWP391-AutoWashPro-BE.Api"
RUN dotnet build "./SWP391-AutoWashPro-BE.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SWP391-AutoWashPro-BE.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SWP391-AutoWashPro-BE.Api.dll"]
