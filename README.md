# CryptoSimulator

ASP.NET Core (.NET 11) API that simulates a cryptocurrency exchange. Data lives in PostgreSQL (EF Core + Npgsql).

## Requirements

- .NET 11 SDK
- Docker
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

## Run

```bash
dotnet run --project CryptoSimulator/CryptoSimulator/CryptoSimulator.csproj
```

Swagger UI (Development only): `http://localhost:5185/swagger`

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
