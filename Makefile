# Hive - Engineering Manager Tool
# Common commands for development and deployment

.PHONY: help install build test run dev clean backend frontend electron-dev electron-build

# Default target
help:
	@echo "Hive - Common Commands"
	@echo ""
	@echo "Development:"
	@echo "  make install        - Install all dependencies (backend + frontend)"
	@echo "  make dev            - Run backend and frontend in development mode"
	@echo "  make backend        - Run backend only (port 5002)"
	@echo "  make frontend       - Run frontend only (port 5173)"
	@echo "  make electron-dev   - Run Electron app in development mode"
	@echo ""
	@echo "Testing:"
	@echo "  make test           - Run all backend tests"
	@echo "  make test-watch     - Run tests in watch mode"
	@echo ""
	@echo "Building:"
	@echo "  make build          - Build backend and frontend"
	@echo "  make build-backend  - Build backend for current platform"
	@echo "  make build-all      - Build backend for all platforms"
	@echo "  make electron-build - Build standalone Electron app"
	@echo ""
	@echo "Other:"
	@echo "  make clean          - Clean build artifacts"
	@echo "  make restore        - Restore NuGet packages"
	@echo "  make reset-db       - Reset SQLite database (creates backup)"
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

# Restore NuGet packages
restore:
	dotnet restore Hive.sln

# Build everything
build:
	dotnet build Hive.sln
	cd src/Hive.Desktop && npm run build

# Run all tests
test:
	dotnet test Hive.sln

# Run tests in watch mode
test-watch:
	dotnet watch test --project tests/Hive.Tests/Hive.Tests.csproj

# Run backend (development mode with in-memory database)
backend:
	dotnet run --project src/Hive.Api/Hive.Api.csproj

# Run backend with SQLite (production mode)
backend-sqlite:
	UseInMemoryDatabase=false dotnet run --project src/Hive.Api/Hive.Api.csproj

# Run frontend (Vite dev server)
frontend:
	cd src/Hive.Desktop && npm run dev

# Run Electron in development mode (requires backend running separately)
electron-dev:
	cd src/Hive.Desktop && npm run electron:dev

# Run both backend and frontend in development mode
dev:
	@echo "Starting backend and frontend..."
	@echo "Backend: http://localhost:5002"
	@echo "Frontend: http://localhost:5173"
	@echo ""
	@make -j2 backend frontend

# Build backend for current platform
build-backend:
	./scripts/build-backend.sh

# Build backend for macOS ARM64
build-backend-mac:
	./scripts/build-backend.sh osx-arm64

# Build backend for Windows
build-backend-win:
	./scripts/build-backend.sh win-x64

# Build backend for all platforms
build-all:
	./scripts/build-backend.sh all

# Build standalone Electron app for current platform
electron-build:
	cd src/Hive.Desktop && npm run electron:build

# Build Electron app for macOS
electron-build-mac:
	cd src/Hive.Desktop && npm run electron:build:mac

# Build Electron app for Windows
electron-build-win:
	cd src/Hive.Desktop && npm run electron:build:win

# Clean build artifacts
clean:
	dotnet clean Hive.sln
	rm -rf src/Hive.Desktop/dist
	rm -rf src/Hive.Desktop/dist-electron
	rm -rf src/Hive.Desktop/release
	rm -rf src/Hive.Desktop/backend
	find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
	find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true

# Check code health
check:
	dotnet build Hive.sln --warnaserror
	cd src/Hive.Desktop && npx tsc --noEmit

# Reset the SQLite database (backs up existing database)
reset-db:
	@echo "Resetting SQLite database..."
	@if [ -f "$$HOME/Library/Application Support/Hive/hive.db" ]; then \
		echo "Backing up existing database..."; \
		cp "$$HOME/Library/Application Support/Hive/hive.db" "$$HOME/Library/Application Support/Hive/hive.db.backup.$$(date +%Y%m%d_%H%M%S)"; \
		rm "$$HOME/Library/Application Support/Hive/hive.db"; \
		echo "Database deleted. It will be recreated on next backend start."; \
	else \
		echo "No database file found."; \
	fi

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
