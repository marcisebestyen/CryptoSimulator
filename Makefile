# Run every target from the repo root, e.g. `make backend-build`.
# Run `make help` for the list.

.DEFAULT_GOAL := help
.PHONY: help setup env tools restore backend-build backend-run backend-test backend-test-one \
	backend-coverage backend-format backend-format-check migration-add db-update \
	db-up db-down db-logs db-health db-seed db-reset frontend-install frontend-dev \
	frontend-build frontend-lint frontend-test frontend-test-watch frontend-coverage \
	frontend-preview check

API_PROJECT := CryptoSimulator/CryptoSimulator/CryptoSimulator.csproj
EF_PROJECT := CryptoSimulator/CryptoSimulator
DB_CONTAINER := crypto-postgres
DB_USER := admin-user
DB_NAME := crypto-db

help: ## Show this list
	@grep -E '^[a-zA-Z_-]+:.*## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*## "} {printf "  %-22s %s\n", $$1, $$2}'

# --- Setup ---

setup: env tools restore frontend-install ## First-time setup: .env, tools, backend restore, frontend install

env: ## Create .env from .env.example if it is missing
	@test -f .env || (cp .env.example .env && echo "Created .env from .env.example. Edit the values.")

tools: ## Install or update the dotnet-ef CLI tool (global)
	dotnet tool update -g dotnet-ef --prerelease

restore: ## Restore NuGet packages for the backend
	cd CryptoSimulator && dotnet restore

# --- Backend (.NET) ---

backend-build: ## Build the backend with warnings as errors
	cd CryptoSimulator && dotnet build --no-restore -warnaserror

backend-run: ## Run the API (Swagger on http://localhost:5185/swagger)
	dotnet run --project $(API_PROJECT)

backend-test: ## Run all backend tests
	cd CryptoSimulator && dotnet test

backend-test-one: ## Run one backend test by name: make backend-test-one TEST=SomeTestName
	@test -n "$(TEST)" || (echo "Usage: make backend-test-one TEST=SomeTestName" && exit 1)
	cd CryptoSimulator && dotnet test --filter "FullyQualifiedName~$(TEST)"

backend-coverage: ## Run backend tests with coverage (no report files kept in the repo)
	cd CryptoSimulator && dotnet test --collect:"XPlat Code Coverage" --results-directory "$$(mktemp -d)"

backend-format: ## Format the backend code
	cd CryptoSimulator && dotnet format

backend-format-check: ## Check backend formatting without changing files
	cd CryptoSimulator && dotnet format --verify-no-changes

# --- Database (EF Core migrations) ---

migration-add: ## Add an EF Core migration: make migration-add NAME=AddSomething
	@test -n "$(NAME)" || (echo "Usage: make migration-add NAME=AddSomething" && exit 1)
	dotnet ef migrations add $(NAME) --project $(EF_PROJECT)

db-update: ## Apply EF Core migrations to the local database
	dotnet ef database update --project $(EF_PROJECT)

# --- Database (Docker) ---

db-up: ## Start PostgreSQL in Docker
	docker compose up -d

db-down: ## Stop PostgreSQL (keeps data)
	docker compose down

db-logs: ## Follow PostgreSQL container logs
	docker compose logs -f postgres

db-health: ## Show the PostgreSQL health status (expect: healthy)
	docker inspect -f '{{.State.Health.Status}}' $(DB_CONTAINER)

db-seed: ## Load sample data (empty database only)
	docker exec -i $(DB_CONTAINER) psql -U $(DB_USER) -d $(DB_NAME) -v ON_ERROR_STOP=1 < initdb.sql

db-reset: ## DELETE all local DB data and start fresh (asks first)
	@read -p "This deletes all local database data. Type 'yes' to continue: " ans; \
	if [ "$$ans" = "yes" ]; then docker compose down -v && docker compose up -d; else echo "Cancelled."; fi

# --- Frontend (React + Vite) ---

frontend-install: ## Install frontend packages from the lockfile
	cd frontend && pnpm install --frozen-lockfile

frontend-run: ## Start the Vite dev server on http://localhost:5173
	cd frontend && pnpm dev

frontend-build: ## Type-check and build the frontend
	cd frontend && pnpm build

frontend-lint: ## Lint the frontend with oxlint
	cd frontend && pnpm lint

frontend-test: ## Run frontend tests once
	cd frontend && pnpm test

frontend-test-watch: ## Run frontend tests in watch mode
	cd frontend && pnpm test:watch

frontend-coverage: ## Run frontend tests with coverage
	cd frontend && pnpm coverage

frontend-preview: ## Preview the production frontend build
	cd frontend && pnpm preview

# --- All checks ---

check: backend-build backend-test frontend-lint frontend-build frontend-test ## Build, lint and test everything
