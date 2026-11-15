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

4. Load the cryptos and their starting prices (empty database only; the script refuses to run twice):

   ```bash
   docker exec -i crypto-postgres psql -U admin-user -d crypto-db -v ON_ERROR_STOP=1 < initdb.sql
   ```

   `initdb.sql` holds only the 15 cryptos and one current price each. There are no sample users: register in the UI or sign in as the admin (see below).

## Configuration

The API builds the connection string from `POSTGRES_DB`, `POSTGRES_USER` and `POSTGRES_PASSWORD` in
the git-ignored `.env` file (it looks in the project folder and its parents). Host and port default to
`localhost:5432`; override with `POSTGRES_HOST` / `POSTGRES_PORT`. No password lives in tracked files.
For any other environment, set `ConnectionStrings__DefaultConnection` instead; it wins over `.env`.

## Authentication

All endpoints require a JWT bearer token, except `POST /api/users/login`, `POST /api/users/register` and `POST /api/users/forgot-password`.
Configure signing in `.env` (see `.env.example`):

- `JWT_KEY`: signing key, at least 32 characters (startup fails otherwise). Generate one with `openssl rand -base64 48`.
- `JWT_ISSUER`, `JWT_AUDIENCE`: required.
- `JWT_EXPIRY_MINUTES`: optional, defaults to `60`.
- `RECOVERY_CODE_KEY`: secret key used to hash recovery codes, at least 32 characters (startup fails otherwise). Generate one with `openssl rand -base64 48`. Keep it only in the git-ignored `.env`; changing it makes all existing recovery codes stop working.

### First admin

The API creates the first admin at startup, only if no admin exists yet. Set these in `.env` (all three are required, startup fails otherwise):

- `ADMIN_USERNAME`, `ADMIN_EMAIL`: must not already belong to another user.
- `ADMIN_PASSWORD`: at least 8 characters.

`Admin__Username`, `Admin__Email` and `Admin__Password` win over `.env`. The admin has an empty wallet and no recovery code, so forgot-password never works for the admin. Later changes to these values do not touch an existing admin.

Environment variables `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` and `Jwt__ExpiryMinutes` win over `.env`.

Passwords are stored as BCrypt hashes.

`POST /api/users/login` returns `200` with:

```json
{
  "token": "<jwt>",
  "user": { "id": 1, "role": 0, "username": "...", "firstName": "...", "lastName": "...", "email": "..." }
}
```

`POST /api/users/register` returns `201` with the new user and a one-time recovery code:

```json
{
  "user": { "id": 1, "role": 0, "username": "...", "firstName": "...", "lastName": "...", "email": "..." },
  "recoveryCode": "Ab1!xY9?"
}
```

The recovery code is 8 characters (letters, digits and symbols). It is shown only once and only its hash is stored, so the user must save it.

`POST /api/users/forgot-password` resets a forgotten password. Body: `{ "email": "...", "recoveryCode": "...", "newPassword": "..." }`.
`200` returns `{ "recoveryCode": "<new code>" }`; the old code stops working, so save the new one.
`400` returns `Invalid email or recovery code.` (same text for an unknown email, a wrong code or a user with no code). A missing or blank field gets the standard validation `400` ProblemDetails instead.

Send the token on every other request: `Authorization: Bearer <jwt>`. Missing, invalid or expired tokens get `401`.
In Swagger UI use the Authorize button and paste the token.

The token carries the user id (`nameidentifier`), email and a role claim (`Admin` or `User`).

### CORS

Browsers may only call the API from origins listed in `Cors:AllowedOrigins` (a string array; startup fails if it is empty).
`appsettings.Development.json` allows `http://localhost:5173` (the Vite dev server). For any other environment set
`Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ... Allowed headers are `Authorization` and `Content-Type`.

### Access rules

Users may only touch their own data. Foreign data returns `403`; a missing user, wallet, transaction or alert returns `404`.

- Owner or `Admin`: `GET/PUT/DELETE /api/users/{id}`, `PUT/DELETE /api/wallets/{id}`, `GET /api/transactions/{id}`, and the `GET /api/users/{id}/...` routes (portfolio, profit-loss, profit-loss/summary, transactions, alerts, alerts/history).
- Owner only (an admin gets `403`): `POST /api/users/{id}/change-password`, `POST /api/trades/buy` and `/sell` (body `userId`), `POST /api/alerts` (body `userId`) and `DELETE /api/alerts/{id}`, and the deposit routes `POST /api/deposits` (body `userId`), `GET /api/users/{id}/deposits`, `GET /api/users/{id}/cards` and `DELETE /api/users/{id}/cards/{cardId}`. A `savedCardId` that belongs to another user returns `404`.
- `Admin` only (a normal user gets `403`): `POST /api/cryptos`, `PUT /api/cryptos/{id}` (rename), `PUT /api/cryptos/{id}/price` and `DELETE /api/cryptos/{id}` (`409` when any offer, open or closed, references the crypto). Every logged-in user may `GET` cryptos and their price history.

### Cryptos

- `POST /api/cryptos` takes `{ "name": "...", "initialPrice": 10.5 }`. `initialPrice` is required and must be at least `0.01` (`400` otherwise); it is saved as the first open-ended price record, so the crypto shows up in `GET /api/cryptos` right away. `PUT /api/cryptos/{id}` (rename) takes `{ "name": "..." }` and returns `204`, or `404` for an unknown crypto. For both, a name already used by another crypto (case-insensitive) returns `409`.
- `GET /api/cryptos/{id}/price-history?page=1&pageSize=25` returns the price records newest first:

  ```json
  { "items": [ { "...": "..." } ], "totalCount": 120, "page": 1, "pageSize": 25 }
  ```

  `page` starts at 1 and `pageSize` defaults to 25. A `pageSize` above 100 is capped at 100 and the response shows the capped value. `page` or `pageSize` below 1 returns `400`; an unknown crypto returns `404`.
  **Breaking change:** this route used to return a plain array of all records.

### Validation (`400`)

- `amount` of `POST /api/trades/buy` and `/sell` must be at least `0.0001` (it was `0.00000001` before the offers feature).
- `balance` of `PUT /api/wallets/{id}` must not be negative.
- `POST /api/deposits`: `amount` from `10.00` to `100000.00` with at most 2 decimals; card number 15-16 digits that pass the Luhn check; CVV 3-4 digits; expiry not in the past (a card is valid through the end of its month, at most 20 years ahead); exactly one of `savedCardId` or the new-card details (`holderName`, `cardNumber`, `expiryMonth`, `expiryYear`); `holderName` at most 100 characters; a saved card that has expired is rejected.

`POST /api/deposits` returns `201` with `{ id, amount, last4, network, date, newBalance }`. `GET /api/users/{id}/deposits` returns a list of `{ id, amount, last4, network, date }` (newest first), and `GET /api/users/{id}/cards` a list of `{ id, holderName, last4, network, expiryMonth, expiryYear }`.

`POST /api/trades/buy` and `/sell` return `204` on success and `400` with `"Transaction failed"` when the trade fails.

### P2P offers

All offer routes need a logged-in user; the seller or buyer is always the caller (no `userId` in the body). Errors are plain text messages, or ProblemDetails for request body validation.

| Route | Body | Success | Errors |
| --- | --- | --- | --- |
| `GET /api/trades/offers` | - | `200` list of `MarketOfferDto` (open offers of other users) | - |
| `GET /api/trades/offers/mine` | - | `200` list of `MyOfferDto` (the caller's offers) | - |
| `POST /api/trades/offers` | `{ "cryptoId", "amount", "askingPrice" }` | `201` with the `MyOfferDto` | `400` invalid amount or price, crypto without a price, or not enough holdings; `404` unknown crypto or wallet |
| `DELETE /api/trades/offers/{offerId}` | - | `204` | `403` "Only the seller can cancel this offer." (not your offer), `404` unknown offer, `409` offer already closed |
| `POST /api/trades/offers/{offerId}/buy` | `{ "amount" }` | `204` | `400` invalid amount, more than is left, own offer not enough balance or a seller balance that cannot take the payment, `404` unknown offer, `409` offer already closed |

`MarketOfferDto` is `{ id, sellerUsername, sellerCompletionRate, sellerTrades, cryptoId, cryptoName, remainingAmount, askingPrice, currentRate }`. `sellerCompletionRate` is `null` for a seller with no finished offers; otherwise it is the share of the seller's offers with a sale among offers with a sale plus cancelled offers without one.

`MyOfferDto` is `{ id, cryptoId, cryptoName, amount, remainingAmount, soldAmount, askingPrice, currentRate, status, createdAt }`.

How P2P offers work:

- When you offer crypto it is put in escrow: it leaves your holdings until it is sold or the offer is cancelled.
- A buyer may take only part of an offer (partial buy). The rest stays open.
- Cancelling an offer returns the remaining (unsold) amount to the seller.
- You cannot buy your own offer (`400`).
- The buyer pays `amount x askingPrice`, rounded to 2 decimals. A buy whose total rounds to 0 is rejected.
- Minimum `amount` is `0.0001` (at most 4 decimals); `askingPrice` is positive, below `10,000,000,000` and has at most 8 decimals.
- Every fill records a sell transaction for the seller and a buy transaction for the buyer, so both feed the usual profit/loss stats.

`GET /api/users/{id}/portfolio` returns `{ userId, fiatBalance, holdings, totalCryptoValue, totalPortfolioValue }`. Each entry in `holdings` is `{ cryptoId, cryptoName, amountOwned, currentExchangeRate, currentValue }`; `currentValue` is `amountOwned * currentExchangeRate`.

`PUT /api/users/{id}` no longer accepts `email`; the body takes only the other profile fields and the email stays unchanged.

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

Open `http://localhost:5173`. Vite proxies `/api` to `http://localhost:5185`, so no CORS setup is needed. The app has login, register and forgot-password pages.

Pages:

- Register: creates an account and shows the recovery code once.
- Forgot password: resets the password with the recovery code and shows the new code.
- Cryptos: list of cryptos with current prices.
- Price history: paged price records per crypto.
- Profile: portfolio with holdings chart and profit/loss.
- Market: buy and sell cryptos.
- Transactions: transaction history.

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

Then repeat steps 3 and 4 (or `make db-update` and `make db-seed`).

## Notes

- Timestamps are stored as UTC (`timestamptz`); always use `DateTime.UtcNow`.
- Npgsql 11 is a release candidate, so the EF Core packages are pinned to the matching
  `11.0.0-rc.3` build. Move all of them to stable `11.0.0` when Npgsql releases it.
