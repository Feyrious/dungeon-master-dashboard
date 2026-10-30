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

The database and login encryption keys live in the `web-data` volume.

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
