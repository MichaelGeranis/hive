# Hive

The Engineering Manager's operating system.

An EM's work is scattered across half a dozen tools that do not talk to each other:
reviews in an HR system, 1:1 notes in a personal doc, delivery data in Jira, capacity in a
spreadsheet, quarterly plans in slides. The manager becomes the integration layer, and the
questions that actually matter take an afternoon to answer.

Hive puts people, delivery, planning and process in one local desktop application, and
derives the answers instead of making you assemble them.

```
People ──┐
Delivery ─┼──> Capacity · Velocity · Knowledge risk · Planning insights · Sentiment
Planning ─┘
```

---

## Documentation

| Document | What's in it |
|----------|--------------|
| [BUSINESS.md](BUSINESS.md) | Domain model, business rules, glossary, ubiquitous language |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Layers, dependency rule, data model, state machine, extension recipes |
| [CODEBASE.md](CODEBASE.md) | File-by-file map of the repository |
| [CLAUDE.md](CLAUDE.md) | Working agreement for AI agents and contributors |
| [docs/DEBUGGING.md](docs/DEBUGGING.md) | IDE and browser debugging setups |

---

## Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Backend API | ASP.NET Core (C#) | .NET 9.0 |
| Desktop shell | Electron | 28.x |
| Frontend | React + TypeScript + Vite | React 18 |
| Styling | Tailwind CSS | 3.x |
| Database | SQLite + Entity Framework Core | EF Core 9.0 |
| Charts | Recharts | 2.x |
| AI (optional) | Claude API via HttpClient | — |
| Backend testing | xUnit + Moq + FluentAssertions | xUnit 2.6 |
| Frontend testing | Vitest + Testing Library + MSW | Vitest 4.x |

---

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org/)

All projects target `net9.0`. `global.json` sets a floor of SDK 8.0.0 with
`rollForward: latestMajor`, so a newer installed SDK is used automatically.

---

## Getting Started

### 1. Clone and install

```bash
git clone https://github.com/MichaelGeranis/hive.git
cd hive
make install
```

`make install` restores NuGet packages and installs frontend dependencies.

### 2. Run it

```bash
make dev
```

This starts the backend on `http://localhost:5002` and the frontend on
`http://localhost:5173`.

To run the halves separately:

```bash
make backend-inmemory   # backend, in-memory database, resets on restart
make backend-sqlite     # backend, persistent SQLite database
make frontend           # Vite dev server only
```

**The backend must be running before the frontend is useful.** If port 5002 is already
held, `make kill-backend` frees it.

### 3. Sign in

All endpoints require HTTP Basic Authentication. The default credentials are
`admin` / `admin123`, configured in `src/Hive.Api/appsettings.json`.

### 4. Explore the API

With the backend running in development, Swagger UI is at `http://localhost:5002`.

### 5. Run the tests

```bash
make test               # backend + frontend
make test-coverage      # with coverage collection
make coverage-report    # generate and open the HTML report
```

---

## Common Commands

Run `make help` for the full list.

| Command | Does |
|---------|------|
| `make install` | Install backend and frontend dependencies |
| `make dev` | Run backend and frontend together |
| `make backend-sqlite` | Run backend with a persistent database |
| `make backend-inmemory` | Run backend with an in-memory database |
| `make frontend` | Run the Vite dev server (port 5173) |
| `make test` | Run all tests |
| `make test-coverage` | Run tests with coverage |
| `make coverage-report` | Generate and open the HTML coverage report |
| `make build` | Build backend and frontend |
| `make check` | Build and test |
| `make clean` | Remove build artifacts |
| `make kill-backend` | Free port 5002 |
| `make migration-add` | Create a new EF Core migration (prompts for a name) |
| `make migration-update` | Apply migrations to the database |
| `make migration-list` | List all migrations |
| `make migration-remove` | Remove the last migration |
| `make electron-build` | Build the packaged desktop application |

Direct equivalents:

```bash
dotnet build Hive.sln
dotnet run --project src/Hive.Api
dotnet test Hive.sln
dotnet test --filter "FullyQualifiedName~DirectReport"

cd src/Hive.Desktop
npm run electron:dev        # Vite + Electron together
npm run test                # Vitest
npm run electron:build      # Package the desktop app
```

---

## Database

Hive runs against one of two persistence modes, chosen by `UseInMemoryDatabase` in
`appsettings.json`. Unset, it defaults to in-memory in the Development environment.

| | In-memory | SQLite |
|---|---|---|
| Setting | `UseInMemoryDatabase: true` | `UseInMemoryDatabase: false` |
| Survives restart | No | Yes |
| Seeds data | Optional | On first run |
| Use for | Development, tests | Production, anything you want to keep |

The SQLite file lives in a platform-specific user data directory:

| Platform | Path |
|----------|------|
| macOS | `~/Library/Application Support/Hive/hive.db` |
| Windows | `%APPDATA%/Hive/hive.db` |
| Linux | `~/.local/share/Hive/hive.db` |

`HIVE_DATABASE_PATH` overrides all three.

> **Note for contributors:** these are two genuinely separate implementations, not one
> store with two backends. Every repository interface is implemented twice. See
> [ARCHITECTURE.md § Dual Persistence](ARCHITECTURE.md#dual-persistence) before adding an
> entity.

---

## Contributing

### Development workflow

1. Create a branch: `git checkout -b feat/your-feature`.
2. Read [ARCHITECTURE.md](ARCHITECTURE.md) if you are adding an entity, a page, or a
   dashboard widget — each has a recipe, and each has a step that is easy to miss.
3. Make your changes. Domain rules go on the entity, not in a service.
4. Run `make test`. Add tests for what you changed.
5. Update [CODEBASE.md](CODEBASE.md) if you added or removed a file.
6. Commit using [conventional commits](https://www.conventionalcommits.org/):
   `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`.
7. Open a pull request against `main`.

### Before you open a PR

- Both repository implementations registered, if you added one
- DTOs cover every field the frontend needs — controllers never return entities
- Tests added for entity, repository, service, and controller
- Docs updated: CODEBASE.md for files, ARCHITECTURE.md for structure, BUSINESS.md for rules

Full checklist in [CLAUDE.md](CLAUDE.md).

---

## Configuration

### Environment variables

| Variable | Description |
|----------|-------------|
| `HIVE_DATABASE_PATH` | Override the default SQLite database location |
| `ASPNETCORE_ENVIRONMENT` | `Development` enables Swagger and defaults to the in-memory database |

### appsettings.json

| Key | Description |
|-----|-------------|
| `AdminCredentials:Username` | Basic Auth username (default `admin`) |
| `AdminCredentials:Password` | Basic Auth password (default `admin123`) |
| `UseInMemoryDatabase` | `true` for in-memory, `false` for SQLite |

### Runtime settings

Everything the manager can change while the app is running is stored in the database and
edited on the Settings page, not in a config file: story point mappings, T-shirt size
mappings, dashboard warning thresholds, support and maintenance labels, the Jira base URL,
and the optional Claude API key.

### Frontend

The API base URL is set in `src/Hive.Desktop/src/services/api.ts`
(`http://localhost:5002/api`). CORS on the backend allows exactly one origin,
`http://localhost:5173`.

---

## Optional: sentiment analysis

Hive can analyse the tone of your 1:1 notes using the Claude API. It is **off by default**
and requires your own API key, entered on the Settings page. With no key configured
nothing is sent anywhere. See [BUSINESS.md § Sentiment insights](BUSINESS.md#sentiment-insights).

---

## Security note

Hive is a **single-user, local-first desktop application**. Basic Authentication against a
credential pair in a settings file is adequate for an API bound to localhost on the
manager's own machine, and it is not adequate for anything else. Do not deploy Hive to a
shared host without replacing the authentication scheme first — see
[ARCHITECTURE.md § Authentication](ARCHITECTURE.md#authentication).

---

## License

TBD
