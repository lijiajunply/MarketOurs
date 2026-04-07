# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Project Overview

**MarketOurs** ("我们的集市，不属于任何人") is a campus marketplace platform consisting of:
- `api/` — ASP.NET Core 10.0 backend
- `app/webapp/` — React 19 customer-facing web app
- `app/admin_app/` — React 19 admin dashboard
- `app/mobile_app/` — Flutter mobile app

---

## Commands

### Backend (.NET)

```bash
# Run with SQLite (default, no Redis required)
dotnet run --project api/MarketOurs.WebAPI/MarketOurs.WebAPI/

# Build
dotnet build api/MarketOurs.WebAPI/MarketOurs.WebAPI.sln

# Run all tests
dotnet test api/MarketOurs.WebAPI/MarketOurs.WebAPI.sln

# Run a specific test class
dotnet test api/MarketOurs.WebAPI/MarketOurs.WebAPI.sln --filter "ClassName=PostServiceTests"

# Run tests by category (integration tests require Docker)
dotnet test api/MarketOurs.WebAPI/MarketOurs.WebAPI.sln --filter "Category=HighLoad"
```

Integration tests (`MarketOurs.Test/Integration/`) auto-skip if Docker is unavailable — they spin up real PostgreSQL and Redis via Testcontainers.

### Docker (Full Stack)

```bash
# Start API + Redis + Mailpit (SMTP mock)
docker compose -f api/MarketOurs.WebAPI/compose.yaml up

# With PostgreSQL (uncomment postgres service and SQL env var in compose.yaml first)
docker compose -f api/MarketOurs.WebAPI/compose.yaml up postgres
```

Ports: API `8080/8081`, Redis `6379`, PostgreSQL `5432`, Mailpit UI `8025`.

### Web Apps (webapp / admin_app)

```bash
cd app/webapp       # or app/admin_app
pnpm install
pnpm run dev        # Vite dev server
pnpm run build      # TypeScript check + production bundle
pnpm run lint
```

### Mobile (Flutter)

```bash
cd app/mobile_app
flutter pub get
flutter run
```

---

## Backend Architecture

Three-layer architecture under `api/MarketOurs.WebAPI/`:

| Project | Role |
|---|---|
| `MarketOurs.WebAPI` | Controllers, middleware, Program.cs, filters |
| `MarketOurs.DataAPI` | Services, repositories (interfaces + implementations), background jobs |
| `MarketOurs.Data` | EF Core `MarketContext`, data models, DTOs, migrations |
| `MarketOurs.Test` | NUnit tests (unit, integration, stress, concurrency) |

### Key Design Points

**Database**: Defaults to SQLite (`Data.db`) locally; switches to PostgreSQL when the `SQL` env var is set. EF Core migrations run automatically on startup. `IDbContextFactory<MarketContext>` is used (not scoped DbContext directly).

**Caching**: Two-tier — Redis (`IDistributedCache`) as primary, `IMemoryCache` (1000-item LRU) as local layer. Redis connection via `IConnectionMultiplexer` is also injected directly for advanced operations.

**Authentication**: RSA-2048 JWT (20-min access token, 72-hour refresh token via `X-Refresh-Token` response header). OAuth2 via GitHub, Google, WeChat. Keys stored at `./app/keys/` and auto-rotate every 90 days (`RsaKeyManager`).

**Rate limiting**: Custom `RateLimitMiddleware` + IP blacklist controller. Data masking via `DataMaskingMiddleware`.

**Like system**: Uses `ILockService` (distributed Redis locks) to prevent duplicate likes. Background service syncs Redis like counts back to the database.

**Service registration**: Done via extension methods `RegisterRepositoriesAndServices()` and `RegisterSecurityServices()` in `MarketOurs.WebAPI/Extensions/`.

**Logging**: Console in development; Serilog → SQLite + rolling file in production. Sensitive data filtered via `SensitiveDataFilter` enricher.

### Data Model Relationships

```
User ──< Posts ──< Comments (self-referential parent/child)
User >──< LikePosts, DislikesPosts (N:M)
User >──< LikeComments, DislikesComments (N:M)
```

### Environment Variables (`.env`)

Key variables read at startup via `DotNetEnv.Env.Load()`:

| Variable | Purpose |
|---|---|
| `SQL` | PostgreSQL connection string (empty = SQLite) |
| `REDIS` | Redis connection string |
| `JWT_RSA_PRIVATE_KEY_PATH` / `JWT_RSA_PUBLIC_KEY_PATH` | RSA key file paths |
| `EMAIL_HOST/PORT/USERNAME/PASSWORD/EMAIL` | SMTP config |
| `GITHUB_CLIENTID/CLIENTSECRET` | OAuth2 |
| `GOOGLE_CLIENTID/CLIENTSECRET` | OAuth2 |
| `WEIXIN_CLIENTID/CLIENTSECRET` | OAuth2 |
| `USER` | Comma-separated `username,password` for initial admin seed |

### CORS

Allowed origins: `*.zeabur.app`, `*.xauat.site`, `http://localhost*`. The `X-Refresh-Token` response header is exposed to frontends.

### API Documentation

Scalar UI is available at `/scalar` when running. OpenAPI spec at `/openapi`.
Prometheus metrics at `/metrics`.
