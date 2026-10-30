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

After signing in, the overview shows **Worlds** (links to each campaign's wiki, opened in
a new tab) and **Tools** (one tile per game system). Picking a game system lists its tools
in a side menu. A tool can
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

The catalog checks on startup that ids are unique and only refer to existing systems.

### Worlds

Worlds are managed on the overview: **+** next to the "Worlds" heading adds one, the pencil
on a world tile edits or deletes it. A world links to its wiki at any address (Foundry VTT
journal export, CC: Web Hub, World Anvil, Kanka, LegendKeeper, Obsidian Publish, Notion,
MediaWiki, …);
the chosen wiki software only sets the link text. Like game systems, a world has a tile
image (upload and crop) and a theme, which can be copied from any system or world. Worlds
are stored by the Web API (`api-data` volume).

Older setups kept world links in `worlds.env`; the API copies those into the two worlds the
app came with on startup if they have no link yet, after which the file can be removed.

### Editing a game system

The pencil in the top-right corner of a game system tile opens its settings: name and
tagline, tile image (upload, then drag and zoom to fit the tile), theme colors and fonts
(with contrast warnings), which tools show in its side menu, and whether it's shown on the
overview. Only values that differ from the defaults in code are saved, in the Web API's
database (`api-data` volume); "Reset to defaults" removes them again.

### Tile images

Each game system tile has an original illustration in
`src/DungeonMasterDashboard.Web/wwwroot/img/tiles/{id}.svg` (add one for a new system); new
worlds start with `world.svg`. The easiest way to use your own image is uploading it in the
tile's settings. For game systems you can alternatively drop an image into `tile-images/`
named after the system's id (see `tile-images/README.md`); those are git-ignored and only
served to signed-in users. An uploaded image takes precedence.

## Running with Docker

1. Create `.env` with a freshly generated API signing key:

   ```bash
   sed "s|^API_SIGNING_KEY=.*|API_SIGNING_KEY=$(openssl rand -base64 48)|" .env.example > .env
   ```

2. Create `users.env`, then open it and fill in your email and an initial password:

   ```bash
   cp users.env.example users.env
   ```


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

Open `Dungeon Master Dashboard.sln` in Rider or Visual Studio, or use the command line:

```bash
dotnet user-secrets --project src/DungeonMasterDashboard.Web set "Auth:SeedUsers:0:Email" "you@example.com"
dotnet user-secrets --project src/DungeonMasterDashboard.Web set "Auth:SeedUsers:0:Password" "<password>"
dotnet run --project src/DungeonMasterDashboard.Api --launch-profile http
dotnet run --project src/DungeonMasterDashboard.Web --launch-profile http
```

## Tests

```bash
dotnet test DungeonMasterDashboard.slnx
```

The NUnit project in `tests/DungeonMasterDashboard.Tests` covers:

- **Contracts:** validation rules (web addresses, colors, fonts, ids).
- **Web logic:** game system catalog, themes and editor drafts, contrast checks, world tiles
  and wiki software, the API client (against a fake API), API tokens, tile images and user
  seeding.
- **Web API:** every endpoint against an in-memory API with its own SQLite database,
  including authentication, validation, image checks and the world link import.
- **Web app:** sign-in, lockout, logout, password change and which pages need a login, run
  against an in-memory web app.
- **Components:** tiles, theme editor, contrast checks, side menu, the overview and both
  settings dialogs, rendered with [bUnit](https://bunit.dev) (browser scripts are stubbed).

Not covered: the in-browser cropper script and the template's two-factor and passkey
sign-in flows, which need a real browser or authenticator.

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
