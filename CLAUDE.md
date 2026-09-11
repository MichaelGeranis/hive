# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Hive is the Engineering Manager's operating system: a single-user, local-first desktop
application for managing people, delivery, planning and process. .NET 9 backend in Clean
Architecture, Electron + React desktop client, SQLite for storage.

# Bash commands

### Make (preferred)
```bash
make help                            # All available commands
make install                         # Install backend + frontend dependencies
make dev                             # Run backend and frontend together
make backend-sqlite                  # Backend with persistent SQLite
make backend-inmemory                # Backend with in-memory database
make frontend                        # Vite dev server (port 5173)
make test                            # All tests
make test-coverage                   # Tests with coverage
make check                           # Build and test
make kill-backend                    # Free port 5002
```

### Backend
```bash
dotnet build Hive.sln
dotnet run --project src/Hive.Api                        # port 5002
dotnet test Hive.sln
dotnet test --filter "FullyQualifiedName~DirectReport"   # filtered
dotnet format
```

### Frontend
```bash
cd src/Hive.Desktop
npm run electron:dev                 # Vite + Electron
npm run test                         # Vitest
npm run electron:build               # Package the desktop app
```

### Migrations
```bash
make migration-add                   # Create (prompts for a name)
make migration-update                # Apply
make migration-list                  # List
make migration-remove                # Remove the last one
```

# Code style

- **Dependency rule is absolute**: never reference an outer layer from an inner one.
  `Core` depends on nothing. `Application` depends only on `Core`.
- **Validation lives on the entity**: an entity can never be constructed in an invalid
  state. Never re-validate in a service, a controller, or the frontend.
- **Entities use private setters** and intent-revealing methods (`Start()`, `Complete()`,
  `Deactivate()`), never public property assignment.
- **No business logic in controllers**: an action maps HTTP to a service call and back.
- **No business logic in the frontend**: `Hive.Desktop` renders API responses. It may
  guide input; it is never the authority.
- **Never return entities from a controller**: DTOs cross the API boundary, always.
- **Every repository is implemented twice** — in-memory and SQLite — and registered twice.
- **No `DbContext` outside `Hive.Infrastructure/Persistence/`.**
- **No `HttpClient` instantiation** — use `AddHttpClient` registration.
- **All frontend API calls go through `services/api.ts`**, never Axios in a component.

## Naming Conventions

| Type | Convention | Example |
|------|-----------|---------|
| Domain entities | PascalCase noun | `DirectReport`, `TeamTask` |
| Enums | PascalCase | `TaskStatus.InProgress` |
| Repository interfaces (Core) | `I{Name}Repository` | `ILeaveRepository` |
| In-memory repositories | `{Name}Repository` | `LeaveRepository` |
| SQLite repositories | `Sqlite{Name}Repository` | `SqliteLeaveRepository` |
| Service interfaces | `I{Name}Service` | `ILeaveService` |
| Services | `{Name}Service` | `LeaveService` |
| Controllers | `{Plural}Controller` | `LeavesController` |
| DTOs | `{Name}Dto` / `Create{Name}Dto` / `Update{Name}Dto` | `LeaveDto` |
| EF migrations | `{YYYYMMDDHHMMSS}_{Description}` | `20260101135546_InitialCreate` |
| React pages | PascalCase `.tsx` in `pages/` | `Leaves.tsx` |
| React components | PascalCase `.tsx` in `components/` | `SkillsHeatmap.tsx` |
| Tests | `{Subject}Tests.cs` | `LeaveServiceTests.cs` |
| Test methods | `MethodName_Scenario_ExpectedBehavior` | `Update_EndBeforeStart_Throws` |

# Workflow rules

- Run the whole test suite after finishing a task to confirm nothing else broke.
- Change a test's expected behaviour **only** when the task requires it. If your change
  breaks an unrelated test, the change is wrong — stop, do not commit.
- Format the files you edited (`dotnet format` for C#).
- Update `CODEBASE.md` when you add or remove a file.
- Update `ARCHITECTURE.md` when you change structure; update `BUSINESS.md` when you change
  a domain rule.

## Branch Naming
- Feature: `feat/short-description`
- Bug fix: `fix/short-description`
- Documentation: `docs/topic`
- Never commit directly to `main`.
- Commit types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`.

## Pull Requests
- Squash commits before merge (keep `main` linear).
- Resolve all review conversations before merging.
- No merge conflicts — rebase on `main` if conflicts exist.

## Testing
- Unit tests for all domain rules and service logic.
- Entity tests construct entities directly — no mocks, no database.
- Service tests mock repository interfaces from `Core` with Moq.
- Integration tests derive from `IntegrationTestBase` and use the full DI container.
- Frontend tests use Vitest with MSW handlers in `src/test/mocks/`.
- No unit test may make a real HTTP call or hit a real database.
- Use `[Fact]` for single cases, `[Theory]` for parameterised ones.
- Assert with FluentAssertions.

## Pre-Commit Checklist

Before finalizing any implementation, verify:
- `Hive.Core` references no infrastructure packages
- `Hive.Application` references only `Hive.Core`
- No `DbContext` outside `Hive.Infrastructure/Persistence/`
- No business logic in `Hive.Api` controllers
- No entity returned from a controller — DTOs only
- **Both** repository implementations written, and **both** DI registrations added
  (`AddInfrastructureServices` *and* `AddSqliteInfrastructureServices`)
- New entity added to both `InMemoryDbContext` and `HiveDbContext`, with a migration
- New dashboard widget carries `badge={ALL_TIME_BADGE}` if its data ignores the sprint filter
- New entity has an `EntityType` value if it should appear in the activity feed
- Tests added for entity, repository, service, and controller
- `CODEBASE.md` updated for new or removed files
- Domain terms match the Ubiquitous Language table in `BUSINESS.md`

## What To Do When Unsure Where Something Goes

1. **Is it a domain rule, an invariant, or an entity?** → `Hive.Core`
2. **Is it orchestration across entities, or a DTO?** → `Hive.Application`
3. **Is it a call to an external system (database, Claude API, filesystem)?** → `Hive.Infrastructure`
4. **Is it HTTP in/out, auth, or DI wiring?** → `Hive.Api`
5. **Is it purely visual?** → `Hive.Desktop`
6. **Still unsure?** → Ask the human.

# Additional Instructions

- See @README.md for project overview, getting started, and configuration.
  This document is the entry point. Keep it current when setup or commands change.
- See @ARCHITECTURE.md before making architectural changes.
  This is the complete technical reference. It takes precedence over CLAUDE.md on
  structural matters. Update it whenever a significant architectural decision is made.
- See @BUSINESS.md for domain concepts and business rules.
  It takes precedence over CLAUDE.md on business matters. Update it when a domain rule
  changes; the Ubiquitous Language table is binding on naming.
- See @CODEBASE.md for the file-by-file map. Read it before generating code, and update it
  whenever you add or remove a file.
- See @docs/DEBUGGING.md for IDE and browser debugging setups.
