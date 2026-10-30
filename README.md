# Dungeon Master Dashboard

A web dashboard for dungeon masters.

## Stack

- **Frontend:** Blazor (.NET)
- **Backend:** ASP.NET Core Web API (.NET)
- **Runtime:** Docker containers (Docker Compose)

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
