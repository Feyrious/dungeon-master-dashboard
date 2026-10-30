# Dungeon Master Dashboard

A web dashboard for dungeon masters.

## Stack

- **Frontend:** Blazor (.NET)
- **Backend:** ASP.NET Core Web API (.NET)
- **Runtime:** Docker containers (Docker Compose)

## Access control

The site is private. There is no public sign-up: accounts are created on startup from
`users.env` (see `users.env.example`), and everything except the login page requires a
signed-in user. Failed logins lock the account for 5 minutes after 5 attempts. Users can
change their password and enable two-factor authentication or passkeys under their profile.

The Web API is not exposed outside Docker. The frontend calls it with a short-lived token
signed with `API_SIGNING_KEY`, and the API rejects any request without a valid one.

## Worlds, game systems and tools

After signing in, the overview shows **Worlds** (campaign journals exported from
Foundry VTT, opened in a new tab) and **Tools** (one tile per game system). Picking a
game system lists its tools in a side menu. A tool can
belong to several systems and receives the system it was opened from, so it can adapt
(e.g. name lists per setting). Everything is registered in
`src/DungeonMasterDashboard.Web/GameSystems/GameSystemCatalog.cs`.

- **Add a game system:** copy a file in `GameSystems/Systems/`, give it a new id, name,
  tagline and theme colors, and add its `Definition` to the `systems` list in the catalog.
- **Add a tool:** create a component in `Components/Tools/` that `@inherits ToolBase`
  (the current system is in `System`) and declares a static `Definition` with an id, name,
  description, icon and `AvailableIn` (`ToolAvailability.AllSystems` or
  `ToolAvailability.Only(...)`). Add the `Definition` to the `tools` list in the catalog.
  It appears at `/systems/{system}/{tool}`.
- **Add a world:** copy a file in `GameSystems/Worlds/`, give it a new id, name, tagline,
  game system and theme, and add its `Definition` to the `worlds` list in the catalog.
  Its address goes in `worlds.env` as `Worlds__{id}__Url=` (see `worlds.env.example`).

The catalog checks on startup that ids are unique and only refer to existing systems.

### Editing a game system

The pencil in the top-right corner of a game system tile opens its settings: name and
tagline, tile image (upload, then drag and zoom to fit the tile), theme colors and fonts
(with contrast warnings), which tools show in its side menu, and whether it's shown on the
overview. Only values that differ from the defaults in code are saved, in the Web API's
database (`api-data` volume); "Reset to defaults" removes them again.

### Tile images

Each world and game system tile has an original illustration in
`src/DungeonMasterDashboard.Web/wwwroot/img/tiles/{id}.svg` (add one for a new tile). To use
your own image instead, e.g. cover art from a book you own, drop it into `tile-images/` named
after the tile's id (see `tile-images/README.md`). Those images are git-ignored and only
served to signed-in users. An image uploaded in a game system's settings takes precedence
over both.

## Running with Docker

1. Create `.env` with a freshly generated API signing key:

   ```bash
   sed "s|^API_SIGNING_KEY=.*|API_SIGNING_KEY=$(openssl rand -base64 48)|" .env.example > .env
   ```

2. Create `users.env`, then open it and fill in your email and an initial password:

   ```bash
   cp users.env.example users.env
   ```

   Optionally create `worlds.env` the same way from `worlds.env.example` and fill in the
   addresses of your exported Foundry VTT journals.

3. Build and start:

   ```bash
   docker compose up -d --build
   ```

Check `docker compose logs web` for `Created user ...` on first start.

The site listens on `127.0.0.1:8080`. Point your Cloudflare Tunnel's public hostname at
`http://localhost:8080` (cloudflared on the host), or set `TUNNEL_TOKEN` in `.env` and run
`docker compose --profile tunnel up -d` to run cloudflared as a container
(public hostname service: `http://web:8080`).

Accounts and login encryption keys live in the `web-data` volume; game system settings
and uploaded images in the `api-data` volume.

## Running locally

```bash
dotnet user-secrets --project src/DungeonMasterDashboard.Web set "Auth:SeedUsers:0:Email" "you@example.com"
dotnet user-secrets --project src/DungeonMasterDashboard.Web set "Auth:SeedUsers:0:Password" "<password>"
dotnet run --project src/DungeonMasterDashboard.Api --launch-profile http
dotnet run --project src/DungeonMasterDashboard.Web --launch-profile http
```

## Branching model

This repository uses [git-flow](https://github.com/petervanderdoes/gitflow-avh):

| Branch       | Purpose                              |
|--------------|--------------------------------------|
| `main`       | Production-ready releases            |
| `develop`    | Integration branch for next release  |
| `feature/*`  | New features (from `develop`)        |
| `bugfix/*`   | Bug fixes (from `develop`)           |
| `release/*`  | Release preparation                  |
| `hotfix/*`   | Urgent fixes (from `main`)           |
