# Build context is the repository root (see compose.yaml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/DungeonMasterDashboard.Api/DungeonMasterDashboard.Api.csproj DungeonMasterDashboard.Api/
RUN dotnet restore DungeonMasterDashboard.Api/DungeonMasterDashboard.Api.csproj
COPY src/DungeonMasterDashboard.Api/ DungeonMasterDashboard.Api/
RUN dotnet publish DungeonMasterDashboard.Api/DungeonMasterDashboard.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DungeonMasterDashboard.Api.dll"]
