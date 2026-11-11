# Hive - Engineering Manager Tool
# Common commands for development and deployment

.PHONY: help install build test test-backend test-frontend test-coverage coverage-report dev backend-inmemory backend-sqlite frontend build-backend build-all electron-build clean check format lint kill-backend migration-add migration-remove migration-update migration-list

# Default target
help:
	@echo "Hive - Common Commands"
	@echo ""
	@echo "Development:"
	@echo "  make install          - Install all dependencies (backend + frontend)"
	@echo "  make dev              - Run backend (in-memory) and frontend together"
	@echo "  make backend-inmemory - Run backend only, in-memory database (port 5002)"
	@echo "  make backend-sqlite   - Run backend only, persistent SQLite (port 5002)"
	@echo "  make frontend         - Run Vite dev server only (port 5173)"
	@echo ""
	@echo "Testing:"
	@echo "  make test             - Run backend and frontend tests"
	@echo "  make test-backend     - Run backend tests only"
	@echo "  make test-frontend    - Run frontend tests only"
	@echo "  make test-coverage    - Run backend and frontend tests with coverage"
	@echo "  make coverage-report  - Generate and open the backend HTML coverage report"
	@echo ""
	@echo "Quality:"
	@echo "  make check            - Build (warnings as errors), typecheck, lint and test"
	@echo "  make format           - Format C# (dotnet format) and TypeScript (eslint --fix)"
	@echo "  make lint             - Verify formatting and lint without changing files"
	@echo ""
	@echo "Building:"
	@echo "  make build            - Build backend and frontend"
	@echo "  make build-backend    - Publish backend for current platform"
	@echo "  make build-all        - Publish backend for all platforms"
	@echo "  make electron-build   - Build standalone Electron app"
	@echo ""
	@echo "Other:"
	@echo "  make clean            - Clean build artifacts"
	@echo "  make kill-backend     - Kill process using backend port 5002"
	@echo ""
	@echo "Migrations:"
	@echo "  make migration-add     - Create a new migration"
	@echo "  make migration-remove  - Remove the last migration"
	@echo "  make migration-update  - Apply migrations to database"
	@echo "  make migration-list    - List all migrations"

# Install dependencies
install:
	dotnet restore Hive.sln
	cd src/Hive.Desktop && npm install

# Build everything
build:
	dotnet build Hive.sln --restore
	cd src/Hive.Desktop && npm run build

# Run all tests
test: test-backend test-frontend

test-backend:
	dotnet test Hive.sln

test-frontend:
	cd src/Hive.Desktop && npm test

# Run backend and frontend together (backend in-memory, resets on restart)
dev:
	cd src/Hive.Desktop && npx concurrently --kill-others --names backend,frontend --prefix-colors blue,green \
		"cd ../.. && dotnet run --project src/Hive.Api/Hive.Api.csproj" \
		"npm run dev"

# Run backend (development mode with in-memory database)
backend-inmemory:
	dotnet run --project src/Hive.Api/Hive.Api.csproj

# Run backend with SQLite (production mode)
backend-sqlite:
	UseInMemoryDatabase=false dotnet run --project src/Hive.Api/Hive.Api.csproj

# Run frontend (Vite dev server)
frontend:
	cd src/Hive.Desktop && npm run dev

# Build backend for current platform
build-backend:
	./scripts/build-backend.sh

# Build backend for all platforms
build-all:
	./scripts/build-backend.sh all

# Build standalone Electron app for current platform
electron-build:
	cd src/Hive.Desktop && npm run electron:build

# Clean build artifacts
clean:
	dotnet clean Hive.sln
	rm -rf src/Hive.Desktop/dist
	rm -rf src/Hive.Desktop/dist-electron
	rm -rf src/Hive.Desktop/release
	rm -rf src/Hive.Desktop/backend
	find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
	find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true

# Check code health: build with warnings as errors, typecheck, lint, then run every test
check: lint
	dotnet build Hive.sln --warnaserror
	cd src/Hive.Desktop && npx tsc --noEmit
	$(MAKE) test

# Format the code in place
format:
	dotnet format Hive.sln
	cd src/Hive.Desktop && npm run lint:fix

# Verify formatting and lint rules without touching files (what CI runs)
lint:
	dotnet format Hive.sln --verify-no-changes
	cd src/Hive.Desktop && npm run lint

# Create a new migration
migration-add:
	@read -p "Enter migration name: " name; \
	dotnet ef migrations add $$name --project src/Hive.Infrastructure/Hive.Infrastructure.csproj --startup-project src/Hive.Api/Hive.Api.csproj --context HiveDbContext

# Remove the last migration
migration-remove:
	dotnet ef migrations remove --project src/Hive.Infrastructure/Hive.Infrastructure.csproj --startup-project src/Hive.Api/Hive.Api.csproj --context HiveDbContext

# Apply migrations to the database
migration-update:
	dotnet ef database update --project src/Hive.Infrastructure/Hive.Infrastructure.csproj --startup-project src/Hive.Api/Hive.Api.csproj --context HiveDbContext

# List all migrations
migration-list:
	dotnet ef migrations list --project src/Hive.Infrastructure/Hive.Infrastructure.csproj --startup-project src/Hive.Api/Hive.Api.csproj --context HiveDbContext

# Kill process using backend port 5002
kill-backend:
	@echo "Killing process on port 5002..."
	@lsof -ti:5002 | xargs kill -9 2>/dev/null || echo "No process found on port 5002"

# Run tests with code coverage (excludes migrations, interfaces, and tests)
test-coverage:
	@rm -rf coverage
	dotnet test tests/Hive.Tests/Hive.Tests.csproj \
		/p:CollectCoverage=true \
		/p:CoverletOutputFormat=cobertura \
		/p:CoverletOutput=../../coverage/ \
		/p:Exclude="[Hive.Tests]*%2c[*]*.Migrations.*" \
		/p:ExcludeByFile="**/Migrations/**/*.cs%2c**/Interfaces/**/*.cs"
	cd src/Hive.Desktop && npm run test:coverage

# Generate HTML coverage report (requires reportgenerator tool)
coverage-report: test-coverage
	@command -v reportgenerator >/dev/null 2>&1 || (echo "Installing reportgenerator..." && dotnet tool install -g dotnet-reportgenerator-globaltool)
	reportgenerator -reports:"coverage/coverage.cobertura.xml" -targetdir:"coverage/report" -reporttypes:Html
	@echo "Coverage report generated at coverage/report/index.html"
	@open coverage/report/index.html 2>/dev/null || xdg-open coverage/report/index.html 2>/dev/null || echo "Open coverage/report/index.html in your browser"
