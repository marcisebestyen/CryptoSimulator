# CryptoSimulator

ASP.NET Core (.NET 11) API that simulates a cryptocurrency exchange. Data lives in PostgreSQL (EF Core + Npgsql).

## Requirements

- .NET 11 SDK
- Docker
- Node.js and `pnpm` (frontend)
- `dotnet-ef` tool (`dotnet tool install -g dotnet-ef --prerelease`)

## Database setup

1. Create the local settings file from the example and set the values:

   ```bash
   cp .env.example .env
   ```

   `.env` is git-ignored. Defaults: database `crypto-db`, user `admin-user`.

2. Start PostgreSQL (port `5432`) and wait until it is healthy:

   ```bash
   docker compose up -d
   docker inspect -f '{{.State.Health.Status}}' crypto-postgres
   ```

3. Create the tables:

   ```bash
   dotnet ef database update --project CryptoSimulator/CryptoSimulator
   ```

4. Load the sample data (empty database only; the script refuses to run twice):

   ```bash
   docker exec -i crypto-postgres psql -U admin-user -d crypto-db -v ON_ERROR_STOP=1 < initdb.sql
   ```

## Configuration

The API builds the connection string from `POSTGRES_DB`, `POSTGRES_USER` and `POSTGRES_PASSWORD` in
the git-ignored `.env` file (it looks in the project folder and its parents). Host and port default to
`localhost:5432`; override with `POSTGRES_HOST` / `POSTGRES_PORT`. No password lives in tracked files.
For any other environment, set `ConnectionStrings__DefaultConnection` instead; it wins over `.env`.

## Authentication

All endpoints require a JWT bearer token, except `POST /api/users/login` and `POST /api/users/register`.
Configure signing in `.env` (see `.env.example`):

- `JWT_KEY`: signing key, at least 32 characters (startup fails otherwise). Generate one with `openssl rand -base64 48`.
- `JWT_ISSUER`, `JWT_AUDIENCE`: required.
- `JWT_EXPIRY_MINUTES`: optional, defaults to `60`.

Environment variables `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` and `Jwt__ExpiryMinutes` win over `.env`.

`POST /api/users/login` returns `200` with:

```json
{
  "token": "<jwt>",
  "user": { "id": 1, "role": 0, "username": "...", "firstName": "...", "lastName": "...", "email": "..." }
}
```

Send the token on every other request: `Authorization: Bearer <jwt>`. Missing, invalid or expired tokens get `401`.
In Swagger UI use the Authorize button and paste the token.

The token carries the user id (`nameidentifier`), email and a role claim (`Admin` or `User`).

### CORS

Browsers may only call the API from origins listed in `Cors:AllowedOrigins` (a string array; startup fails if it is empty).
`appsettings.Development.json` allows `http://localhost:5173` (the Vite dev server). For any other environment set
`Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ... Allowed headers are `Authorization` and `Content-Type`.

### Access rules

Users may only touch their own data. Foreign data returns `403`; a missing user, wallet, transaction or alert returns `404`.

- Owner or `Admin`: `GET/PUT/DELETE /api/users/{id}`, `PUT/DELETE /api/wallets/{id}`, `GET /api/transactions/{id}`, and the `GET /api/users/{id}/...` routes (wallet, portfolio, profit-loss, profit-loss/summary, transactions, alerts, alerts/history).
- Owner only (an admin gets `403`): `POST /api/users/{id}/change-password`, `POST /api/trades/buy` and `/sell` (body `userId`), `POST /api/alerts` (body `userId`) and `DELETE /api/alerts/{id}`.

### Validation (`400`)

- `amount` of `POST /api/trades/buy` and `/sell` must be greater than 0.
- `balance` of `PUT /api/wallets/{id}` must not be negative.

`PUT /api/cryptos/{id}/price` returns `404` for an unknown crypto.

## Run

```bash
dotnet run --project CryptoSimulator/CryptoSimulator/CryptoSimulator.csproj
```

Swagger UI (Development only): `http://localhost:5185/swagger`

## Frontend

React + Vite + TypeScript in `frontend/`. Start the backend first, then:

```bash
cd frontend
pnpm install
pnpm dev
```

Open `http://localhost:5173`. Vite proxies `/api` to `http://localhost:5185`, so no CORS setup is needed. The page has a login form that calls `POST /api/users/login` to check the link.

Tests (Vitest, Testing Library, MSW; no backend needed):

```bash
pnpm test        # run once
pnpm test:watch  # watch mode
pnpm coverage    # with coverage
```

## Migrations

```bash
dotnet ef migrations add <Name> --project CryptoSimulator/CryptoSimulator
dotnet ef database update --project CryptoSimulator/CryptoSimulator
```

## Reset the local database

This deletes all data in the Docker volume:

```bash
docker compose down -v && docker compose up -d
```

Then repeat steps 3 and 4.

## Notes

- Timestamps are stored as UTC (`timestamptz`); always use `DateTime.UtcNow`.
- Npgsql 11 is a release candidate, so the EF Core packages are pinned to the matching
  `11.0.0-rc.3` build. Move all of them to stable `11.0.0` when Npgsql releases it.
