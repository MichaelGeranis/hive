# Hive — Technical Architecture

This document is the complete technical reference for how Hive is built.
For business rules and domain logic, see [BUSINESS.md](BUSINESS.md).
For a file-by-file map of the repository, see [CODEBASE.md](CODEBASE.md).

---

## Clean Architecture

Hive follows Clean Architecture (Robert C. Martin) with strict layer separation.

```
┌─────────────────────────────────────┐
│         Frameworks & Drivers        │  ← EF Core, SQLite, Electron, Claude API
│  ┌───────────────────────────────┐  │
│  │     Interface Adapters        │  │  ← Controllers, auth, Swagger
│  │  ┌─────────────────────────┐  │  │
│  │  │        Services         │  │  │  ← Application business rules, DTOs
│  │  │  ┌───────────────────┐  │  │  │
│  │  │  │     Entities      │  │  │  │  ← Core domain, validation, invariants
│  │  │  └───────────────────┘  │  │  │
│  │  └─────────────────────────┘  │  │
│  └───────────────────────────────┘  │
└─────────────────────────────────────┘
```

**Principles:**
- **Separation of Concerns**: Business rules (inner layers) are isolated from infrastructure details (outer layers).
- **The Dependency Rule**: Inner layers know nothing about outer layers. Dependencies always point inward.
- **Independent of Frameworks/UI/DB**: The domain does not know that EF Core, SQLite, or React exist.
- **Testable**: Domain rules are tested with no database, no HTTP, and no mocks.
- **SOLID Principles**: Applied throughout for maintainability and flexibility.

---

## The Four Layers

```
Hive.Core              ← Entities (innermost — no dependencies)
Hive.Application       ← Services, DTOs (depends on Core only)
Hive.Infrastructure    ← Frameworks & Drivers (implements Core interfaces)
Hive.Api               ← Interface Adapters (thin HTTP layer, wires DI)
Hive.Desktop           ← Electron + React presentation
```

**The Dependency Rule: arrows always point inward. Never outward.**

```
Api → Application → Core ← Infrastructure
```

`Infrastructure` and `Api` both depend on `Core`.
`Infrastructure` never depends on `Application`.
`Application` never depends on `Infrastructure`.
`Core` depends on nothing.

---

## Layer Responsibilities

### Hive.Core — Entities Layer

**What belongs here:**
- Domain entities: `DirectReport`, `TeamTask`, `Leave`, `Initiative`, etc.
- Repository interfaces: `IDirectReportRepository`, `ITeamTaskRepository`, etc.
- Enums: `TaskStatus`, `LeaveType`, `ProficiencyLevel`, `WorkType`, etc.
- Domain exceptions: `DomainException` and its subclasses `NotFoundException`, `ConflictException`
- All entity validation and invariants

**What does NOT belong here:**
- Any NuGet package reference beyond the .NET base class library
- Any EF Core, ASP.NET Core, or HttpClient reference
- Any DTO, or any type that exists to cross a process boundary

**The rule that matters:** an entity can never be constructed in an invalid state.
Validation lives in the constructor and in the mutating methods — never in a service,
never in a controller, never in the frontend. `new Leave(...)` with an end date before
the start date throws; there is no code path that produces an invalid `Leave`.

Entities use **private setters** and expose intent-revealing methods (`Start()`,
`Complete()`, `Deactivate()`, `AddPoints()`) rather than public property assignment.

**Derived fields belong to the text they come from.** A note's title is derived from the
first line of its content (`NoteText.DeriveTitle`), and a 1:1's person and date are derived
from its tags. The entity owns the parts it can compute alone; resolving a tag to a
`DirectReport` needs the team, so that happens in `OneOnOneMeetingService`, which then calls
`LinkTo` on the entity.

### Hive.Application — Services Layer

**What belongs here:**
- Services: `DirectReportService`, `ReportingService`, `JiraImportService`, etc.
- Service interfaces: `IDirectReportService`, `IReportingService`, etc.
- DTOs: every type crossing the API boundary
- Cross-entity orchestration and analytics
- `IClaudeApiService` — the abstraction over the Claude API

**What does NOT belong here:**
- EF Core, `DbContext`, or SQL of any kind
- HTTP concerns, `HttpContext`, or controller types
- Direct instantiation of Infrastructure classes
- Duplicated entity validation

**Dependency rule check:** `Hive.Application.csproj` references only `Hive.Core`.

**DTOs never leak entities.** A controller returns a DTO or an `ActionResult`, never a
`DirectReport`. Mapping happens in the service, in a private `MapTo*` method.

### Hive.Infrastructure — Frameworks & Drivers Layer

**What belongs here:**
- EF Core: `HiveDbContext`, migrations, SQLite repository implementations
- The in-memory context and its repository implementations
- `ClaudeApiService` — the real `HttpClient` call to the Anthropic API
- `DatabaseBackupService` — file-level SQLite backup
- `DatabaseInitializer` — the hosted service that migrates and seeds on startup

**What does NOT belong here:**
- Business logic or domain rules
- Service orchestration
- Any reference to `Hive.Application`

### Hive.Api — Interface Adapters Layer (thin)

**What belongs here:**
- ASP.NET Core controllers — thin, delegating immediately to services
- `BasicAuthenticationHandler`
- DI container registration in `Program.cs`
- Swagger configuration
- CORS policy

**What does NOT belong here:**
- Business logic of any kind
- Direct repository calls
- Entity construction

**Controller rule:** a controller action maps HTTP to a service call and back. If it
contains a conditional that is not "did the service return null → 404", the logic belongs
in `Hive.Application`.

### Hive.Desktop — Presentation Layer

- Electron 28 shell + React 18 + TypeScript + Vite + Tailwind CSS
- Talks to `Hive.Api` over REST via a single Axios client (`services/api.ts`)
- **Thin presentation layer — no business logic, no authoritative validation**
- All rules, calculations, and state transitions happen server-side

---

## Dependency Map

```
Hive.Api
  └── depends on → Hive.Application
                      └── depends on → Hive.Core
                                          └── depends on → nothing

Hive.Infrastructure
  └── depends on → Hive.Core (implements its interfaces)
  └── registered in → Hive.Api (DI container)

Hive.Desktop
  └── talks to → Hive.Api (over REST, http://localhost:5002/api)
  └── launches → Hive.Api (as a bundled child process)
```

---

## Dual Persistence

Hive ships **two complete, independent persistence stacks** behind the same `Core`
repository interfaces. This is the single most surprising thing about the codebase, and
the thing most likely to be missed when adding an entity.

| | Development | Production |
|---|---|---|
| Selected by | `UseInMemoryDatabase: true` | `UseInMemoryDatabase: false` |
| Registered by | `AddInfrastructureServices()` | `AddSqliteInfrastructureServices()` |
| Context | `InMemoryDbContext` (Singleton) | `HiveDbContext` (Scoped, EF Core) |
| Storage | `ConcurrentDictionary<Guid, T>` per entity | SQLite file |
| Repositories | `Persistence/Repositories/*Repository.cs` | `Persistence/Repositories/Sqlite/Sqlite*Repository.cs` |
| Seeding | Optional, in-process | On first run, via `DatabaseInitializer` |
| Survives restart | No | Yes |

`InMemoryDbContext` is **not** the EF Core in-memory provider. It is a hand-rolled,
thread-safe store built on `ConcurrentDictionary`, registered as a singleton so it behaves
like a database for the process lifetime.

**Consequences you must respect:**
- Adding a repository means writing **two** implementations and **two** DI registrations.
  Registering only one produces a runtime resolution failure in the mode you did not wire.
- The in-memory repositories must return **copies or immutable projections** where the
  SQLite ones would. A caller mutating a returned entity would otherwise silently mutate
  the "database".
- Behaviour must match. If a SQLite repository orders results, the in-memory one must
  order them identically, or tests pass in one mode and fail in the other.
- The default is environment-driven: `UseInMemoryDatabase` falls back to
  `builder.Environment.IsDevelopment()` when unset.

### Database location

Production SQLite lives in a platform-specific user data directory, resolved in
`Program.cs`:

| Platform | Path |
|----------|------|
| macOS | `~/Library/Application Support/Hive/hive.db` |
| Windows | `%APPDATA%/Hive/hive.db` |
| Linux | `~/.local/share/Hive/hive.db` |

`HIVE_DATABASE_PATH` overrides all three. The directory is created if missing.

---

## Data Model

All identifiers are `Guid`. All entities carry `CreatedAt` and, where mutable,
`UpdatedAt`. Relationships are by foreign-key `Guid` — entities do not hold navigation
collections, keeping `Core` free of ORM concerns.

```
DirectReport
  Id, FirstName, LastName, Email, JobTitle, Department, HireDate, IsDirect

Parent
  Id, Name, Labels, TimeSpentMinutes, TeamTaskId?

PerformanceReview
  Id, DirectReportId → DirectReport, ReviewPeriod, ReviewDate, Rating,
  Strengths, AreasForImprovement, ManagerNotes

OneOnOneMeeting
  Id, DirectReportId? → DirectReport, MeetingDate (DateOnly), Title, Content, Tags

ManagerNote
  Id, Title, Content, Tags, Priority, FolderId? → NoteFolder, IsPinned, IsTodo,
  IsCompleted, DueDate?, CompletedAt?

NoteFolder
  Id, Name, ParentFolderId? → NoteFolder, SortOrder

SkillCategory
  Id, Name, Description, SortOrder, IsActive

Skill
  Id, Name, Description, SkillCategoryId → SkillCategory, IsActive

SkillAssessment
  Id, DirectReportId → DirectReport, SkillId → Skill, Level, TargetLevel?, Notes

Leave
  Id, DirectReportId → DirectReport, Type, Status, StartDate, EndDate, Notes?

Project
  Id, Name, Description, Labels, Url

ProjectKnowledge
  Id, DirectReportId → DirectReport, ProjectId → Project, KnowledgeLevel (1–5)

KnowledgePoint
  Id, DirectReportId → DirectReport, ProjectId → Project, ManualPoints, Notes?

TeamTask
  Id, Title, Description, Type, Priority, Status,
  AssigneeId? → DirectReport, ProjectId? → Project, ParentId? → Parent,
  DueDate?, EstimatedHours?, StoryPoints?, TimeSpentMinutes?,
  Tags, Labels, Components?, Sprint, PreviousSprintsStoryPoints?, OverriddenFields

Sprint
  Id, Name, TeamName, Quarter, Year, SprintNumber, StartDate?, EndDate?

SprintCapacity
  Id, SprintId → Sprint, TotalCapacityPoints, AvailableMembers

SprintGoal
  Id, QuarterId → Quarter, SprintId → Sprint, Goal, Notes

Quarter
  Id, Year, QuarterNumber, Name, Status, OkrReference

Initiative
  Id, QuarterId → Quarter, Name, Description, Color, ProjectId? → Project,
  TshirtSize, Url, WorkType, StartSprintId? → Sprint

InitiativeMember
  Id, InitiativeId → Initiative, DirectReportId → DirectReport

Allocation
  Id, InitiativeId → Initiative, DirectReportId → DirectReport, SprintId → Sprint

InitiativeDependency
  Id, DependentInitiativeId → Initiative, DependencyInitiativeId → Initiative, Type, Notes

ChecklistTemplate
  Id, Name, Description, Type, IsActive

ChecklistTemplateItem
  Id, TemplateId → ChecklistTemplate, SortOrder, Content, ItemType,
  IsRequired, HelpText?, EstimatedMinutes?

ChecklistInstance
  Id, TemplateId → ChecklistTemplate, Type, Title, Status, Notes, CompletedAt?,
  CandidateName?, Position?, InterviewDate?,          ← Interview
  NewHireName?, StartDate?, TargetCompletionDate?     ← Onboarding

ChecklistInstanceItem
  Id, InstanceId → ChecklistInstance, TemplateItemId → ChecklistTemplateItem,
  SortOrder, Content, ItemType, IsRequired, Status, Notes,
  Score? (1–5), Assignee?, DueDate?, CompletedAt?

Document
  Id, Title, Content, Url?, Tags

Activity
  Id, ActivityType, EntityType, EntityId, EntityName, Description, Timestamp

SentimentAnalysisCache
  Id, DirectReportId → DirectReport, PositiveScore, NeutralScore, NegativeScore,
  OverallSentiment, KeyThemesJson, TrendDataJson,
  NotesAnalyzed, DaysAnalyzed, LatestNoteDate, AnalyzedAt

AppSettings
  Id, StoryPointMappings, TshirtSizeMappings, ClaudeApiKey?,
  SentimentAnalysisDays, SentimentAnalysisEnabled, SprintTeamFilter?,
  MaxInProgressTasks, MaxBlockedTasks, MaxInReviewTasks, MinProjectMembers,
  SupportLabels, MaintenanceLabels, JiraBaseUrl?
```

---

## Task State Machine

`TeamTask` is the only entity with a non-trivial state machine, and it is enforced on the
entity itself — there is no separate state machine class.

```
Backlog ──> Todo ──> InProgress ──> InReview ──> InTest ──> POAcceptance ──> ReadyToRelease ──> Done
              ▲          │                                                                        │
              │          └──> Blocked                                                             │
              └──────────────────── Reopen ◄─────────────────────────────────────────────────────┘
                                                        Cancelled  (from any non-Done state)
```

| Method | Precondition | Throws when violated |
|--------|--------------|----------------------|
| `MoveToBacklog()` / `MoveToTodo()` | none | — |
| `Start()` | not `Done` or `Cancelled` | `InvalidOperationException` |
| `Block()` | not `Done` or `Cancelled` | `InvalidOperationException` |
| `MoveToReview()` | `InProgress`, `Blocked`, or `InTest` | `InvalidOperationException` |
| `MoveToTest()` | not `Done` or `Cancelled` | `InvalidOperationException` |
| `MoveToPOAcceptance()` | not `Done` or `Cancelled` | `InvalidOperationException` |
| `MoveToReadyToRelease()` | not `Done` or `Cancelled` | `InvalidOperationException` |
| `Complete()` | not `Cancelled` | `InvalidOperationException` |
| `Cancel()` | not `Done` | `InvalidOperationException` |
| `Reopen()` | `Done` or `Cancelled` → lands in `Todo` | `InvalidOperationException` |

Other entities enforce narrower invariants the same way: `Quarter` (`Activate`,
`Complete`, `ResetToPlanning`), `ChecklistInstance` (start/complete/cancel), and
`ChecklistInstanceItem` (a completed item's status is frozen; a required item cannot be
skipped).

---

## Authentication

All API endpoints require **HTTP Basic Authentication**, implemented by
`BasicAuthenticationHandler` in `Hive.Api/Authentication/`.

- Credentials come from the `AdminCredentials` options object. Its compiled-in defaults are
  `admin` / `admin123`; the `AdminCredentials` configuration section is bound over them,
  and `HIVE_ADMIN_USERNAME` / `HIVE_ADMIN_PASSWORD` override both. The API logs a startup
  warning while the default credentials are still in effect.
- The frontend holds the credentials in `services/api.ts` and sends them on every request.
- `/health` is the one anonymous endpoint.

**This is deliberately minimal, and it is only defensible because of what Hive is.** Hive
is a single-user desktop application whose API binds to localhost and whose database is a
file in the user's own profile. The auth boundary exists to stop another process on the
machine from casually reading the API, not to separate users — there are no users. Adding
real accounts would mean a `User` entity, per-row ownership, and a rewrite of every
repository query.

**If Hive is ever hosted**, this scheme is inadequate and must be replaced before anything
else. Basic Auth over a shared deployment with credentials in a settings file and in
frontend source is not a security model.

---

## Claude API Integration

Sentiment analysis is the only AI feature, and it is opt-in.

```
SentimentAnalysisService (Application)
        │  depends on IClaudeApiService (Application/Interfaces)
        ▼
ClaudeApiService (Infrastructure/Services)  ← registered via AddHttpClient
        │
        ▼
   Anthropic API
```

- The API key is stored in **`AppSettings` in the database**, not in configuration. The
  manager enters it on the Settings page at runtime. `ClaudeApiService` reads settings on
  each call and refuses to proceed when the key is missing or blank.
- Results are cached in `SentimentAnalysisCache`, which records what was analysed so
  staleness can be detected.
- `HttpClient` is obtained through `AddHttpClient<IClaudeApiService, ClaudeApiService>()`.
  Never instantiate `HttpClient` directly.
- With no key configured, the feature is inert. Nothing is sent anywhere.

---

## Desktop Packaging

The Electron app bundles the .NET backend and runs it as a child process.

```
Electron main process (electron/main.ts)
        │
        ├── spawns → Hive.Api self-contained binary (electron/backend.ts)
        │              listening on http://localhost:5002
        │
        └── loads  → Vite-built React bundle
                       └── Axios → http://localhost:5002/api
```

- Platform-specific backend binaries are built by `scripts/build-backend.sh`
  (`osx-arm64`, `osx-x64`, `win-x64`, `linux-x64`) into `src/Hive.Desktop/backend/`.
- `electron/preload.ts` is the isolated bridge between renderer and main.
- Release output lands in `src/Hive.Desktop/release/`.
- In development the backend is run separately and Vite serves the renderer on port 5173.

---

## Configuration

| Mechanism | Used for | Where |
|-----------|----------|-------|
| `appsettings.json` | Credentials, logging, database mode | `src/Hive.Api/` |
| Environment variables | Database path override | `HIVE_DATABASE_PATH` |
| `AppSettings` database row | Everything the manager edits at runtime | Settings page |

The split is intentional: **anything the manager can change while the app is running lives
in the database**, because a desktop user cannot edit a JSON file inside an installed app
bundle and restart it. Story point mappings, T-shirt sizes, dashboard thresholds, support
and maintenance labels, the Claude key, and the Jira base URL are all database-backed.

CORS allows exactly one origin, `http://localhost:5173`, for the Vite dev server.

---

## Dashboard Sprint History Filter

The Dashboard has a "Sprint History" dropdown limiting data to the last N sprints. Not all
widgets honour it. Widgets showing all-time data are marked with the `ALL_TIME_BADGE` pill
defined in `Dashboard.tsx`.

### Not affected by the sprint filter (all-time data)

| Widget | Data source | Why unaffected |
|--------|-------------|----------------|
| Team Members (stat) | `dashboard.team.totalReports` | `GetTeamOverviewAsync()` takes no sprint parameter |
| Projects (stat) | `dashboard.tasks.projects.totalProjects` | Backend uses `projects.Count` (all projects) |
| 1:1s logged (stat) | `meetingsApi.getCount()` | Independent API, no sprint parameter |
| TODOs (stat) | `notesApi.getPending()` | Independent API, no sprint parameter |
| Projects Distribution | Local computation from `tasksApi.getAll()` | Fetches all tasks |
| Members by Project | Local computation from `tasksApi.getAll()` | Fetches all tasks |
| Knowledge Level Suggestions | `knowledgePointsApi.getSuggestions()` | Independent API, no sprint parameter |
| Team Sentiment | `sentimentApi` (own component) | Independent API, no sprint parameter |

### Affected by the sprint filter

| Widget | Data source |
|--------|-------------|
| Warnings (stat) | Mixed — depends on filtered capacity, velocity, accuracy |
| Sprint & Tasks Overview (bar) | `capacityAnalysis` + `dashboard.tasks.tasks` |
| Tasks Distribution / (SP) / (Hours) | `dashboard.tasks.tasksByType*` |
| Members Workload | `dashboard.tasks.tasksByAssignee` |
| Capacity Analysis | `reportsApi.getCapacityAnalysis(sprintFilter)` |
| Sprint Capacity Suggestions | Uses `sprintFilter` to limit upcoming sprints |
| Team Velocity | `reportsApi.getTeamVelocity(sprintFilter)` |
| Estimation Accuracy | `reportsApi.getEstimationAccuracy(sprintFilter)` |
| Support Hours / by Assignee | `dashboard.tasks.supportDistribution` |
| Components Distribution | `dashboard.tasks.tasksByComponent` |

### The backend detail that causes this

In `ReportingService.GetTasksAnalyticsAsync(sprintCount)`:
- The `tasks` variable **is** filtered by sprint.
- The `projects` variable **is not** — `TotalProjects = projects.Count` counts all projects.
- `allSprints`, `directReports` and `parents` are never sprint-filtered.

**When adding a dashboard widget**, decide whether its data source honours the filter. If
it does not, add `badge={ALL_TIME_BADGE}` to its `CardHeader`, or the `badge` prop on
`StatCard`. A widget that silently shows all-time data next to filtered widgets is a bug.

---

## Testing Architecture

```
tests/Hive.Tests/
├── Core/Entities/                # Domain rules — no mocks, no database
├── Core/Exceptions/              # Exception behaviour
├── Application/Services/         # Service logic with mocked repositories (Moq)
├── Api/Controllers/              # Controller behaviour with mocked services
├── Api/Authentication/           # BasicAuthenticationHandler
├── Infrastructure/Persistence/   # Context and backup behaviour
├── Infrastructure/Repositories/  # Repository implementations
└── Integration/                  # Real services, real DI container, in-memory database
    ├── IntegrationTestBase.cs
    └── *IntegrationTests.cs
```

- Entity tests construct entities directly and assert on thrown exceptions. They are the
  authority on domain rules and should be the first place a rule is pinned down.
- Service tests mock repository interfaces from `Core`. They never touch a database.
- Integration tests derive from `IntegrationTestBase`, which builds the full DI container
  against the in-memory database — the right place to catch a missing DI registration.
- Frontend tests use Vitest with mocks under `src/test/mocks/`.

Coverage is collected via Coverlet, configured in `coverage.runsettings` and the test
project file. Excluded: the `Hive.Tests` assembly, `**/Migrations/**`, `**/Interfaces/**`.

---

## Extension Recipes

### Adding a new entity

1. Create the entity in `src/Hive.Core/Entities/`, with validation in the constructor and
   in every mutating method. Private setters.
2. Add any enums it needs in `src/Hive.Core/Entities/` (Hive keeps enums beside entities).
3. Add the repository interface in `src/Hive.Core/Interfaces/`.
4. Implement the in-memory repository in `src/Hive.Infrastructure/Persistence/Repositories/`.
5. Implement the SQLite repository in `.../Repositories/Sqlite/`. **Both are required.**
6. Add the entity to `InMemoryDbContext` (a `ConcurrentDictionary`) and to `HiveDbContext`
   (a `DbSet`, plus its mapping inside `OnModelCreating` — Hive configures entities inline
   rather than in separate `IEntityTypeConfiguration` classes).
7. Create the EF Core migration: `make migration-add`.
8. Create DTOs in `src/Hive.Application/DTOs/`.
9. Create the service interface in `src/Hive.Application/Interfaces/` and the
   implementation in `src/Hive.Application/Services/`.
10. Create the controller in `src/Hive.Api/Controllers/`.
11. Register the service in `Hive.Application/DependencyInjection.cs` and **both**
    repositories in `Hive.Infrastructure/DependencyInjection.cs` — once in
    `AddInfrastructureServices` and once in `AddSqliteInfrastructureServices`.
12. Add an `EntityType` enum value if the entity should appear in the activity feed.
13. Add tests: entity, both repositories, service, controller.

### Adding a new page

1. Create the page component in `src/Hive.Desktop/src/pages/`.
2. Add the route in `App.tsx`.
3. Add types in `src/types/`.
4. Add API functions to `services/api.ts` — never call Axios directly from a component.
5. Add the navigation entry in `components/Layout.tsx`.

### Adding a dashboard widget

1. Add the data source — a `ReportingService` method, or an existing API.
2. Decide whether it honours the sprint filter. If not, add `badge={ALL_TIME_BADGE}`.
3. Update the tables in this document.

---

## What To Do When Unsure Where Something Goes

1. **Is it a domain rule, an invariant, or an entity?** → `Hive.Core`
2. **Is it orchestration across entities, or a DTO?** → `Hive.Application`
3. **Is it a call to an external system (database, Claude API, filesystem)?** → `Hive.Infrastructure`
4. **Is it HTTP in/out, auth, or DI wiring?** → `Hive.Api`
5. **Is it purely visual?** → `Hive.Desktop`
6. **Still unsure?** → Ask.
