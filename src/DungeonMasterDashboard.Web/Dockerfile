# Build context is the repository root (see compose.yaml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/DungeonMasterDashboard.Contracts/DungeonMasterDashboard.Contracts.csproj DungeonMasterDashboard.Contracts/
COPY src/DungeonMasterDashboard.Web/DungeonMasterDashboard.Web.csproj DungeonMasterDashboard.Web/
RUN dotnet restore DungeonMasterDashboard.Web/DungeonMasterDashboard.Web.csproj
COPY src/DungeonMasterDashboard.Contracts/ DungeonMasterDashboard.Contracts/
COPY src/DungeonMasterDashboard.Web/ DungeonMasterDashboard.Web/
RUN dotnet publish DungeonMasterDashboard.Web/DungeonMasterDashboard.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
# /app/data holds the SQLite database and data protection keys (mounted as a volume).
RUN mkdir -p /app/data && chown $APP_UID /app/data
USER $APP_UID
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DungeonMasterDashboard.Web.dll"]
