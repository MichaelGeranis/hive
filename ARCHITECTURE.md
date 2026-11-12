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
- Enums: `TaskStatus`, `LeaveType`, `WorkType`, etc.
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
`Complete()`, `Deactivate()`, `SyncFromJira()`) rather than public property assignment.

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
- `IClaudeApiService` and `IOllamaApiService` — the abstractions over the two AI models, for sentiment and the retro summary

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
- `ClaudeApiService` and `OllamaApiService` — the real calls to the Anthropic API and to Ollama
- `DatabaseBackupService` — file-level SQLite backup on startup (§ Backups)
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

- Electron 45 shell + React 19 + TypeScript + Vite + Tailwind CSS 4
- Talks to `Hive.Api` over REST via a single Axios client (`services/api.ts`)
- **Thin presentation layer — no business logic, no authoritative validation**
- Light theme only. Dates are shown as `dd/MM/yyyy` through `utils/dateUtils.ts`; the
  Electron main process pins the Chromium locale to `en-GB` so native date pickers match
- All rules, calculations, and state transitions happen server-side
- **One set of UI pieces.** Every dialog is a `Modal` (Escape and × close it, focus stays in
  it and returns after, the page behind does not scroll; a backdrop click only closes it when
  asked). Every question before an action is `useConfirm()` from `ConfirmContext`, never the
  browser's `confirm()`. Every page starts with a `PageHeader`, and shows `LoadingState`,
  `ErrorState` and `EmptyState` from `PageState.tsx` rather than its own spinner or message
- **Hive's mark is a status light.** `HiveMark` in the sidebar: three honeycomb cells (people,
  delivery, planning) meeting at a dot. The dot lights up while the dashboard's Needs attention
  has warnings (the Dashboard reports the count to `HiveStatusContext` when it loads), and the
  cells fill in turn while a sync or a backup runs (`SyncButton` and Settings wrap their work in
  `trackWork`) and while the backend starts. It pops in once when Hive opens, its glow breathes
  while something needs attention, and a hover sends one wave through the cells; all of the
  motion is CSS in `index.css` and stops under reduced motion. The app icon (`assets/icon.svg`)
  is the same mark

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
- **SQLite reads are untracked.** Every `Get…` and `Search…` query uses `AsNoTracking()`, so
  reports that read whole tables pay nothing for change tracking. `GetByIdAsync` (`FindAsync`)
  and the queries inside delete methods stay tracked. A change is saved only by the
  repository's `UpdateAsync`, never by a later `SaveChanges` noticing a mutated entity, and
  `UpdateAsync` goes through `UpdateUntracked` (`DbSetExtensions`), which lets go of any other
  tracked instance of the same row first.
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

SkillRoadmap
  Id, Name (unique), Description, LibraryKey?

RoadmapSkill
  Id, RoadmapId → SkillRoadmap, Section, Title, Description, Category (Hard/Soft),
  IsOptional, FromStage (Junior/Mid/Senior/Staff), SortOrder

CareerPlan
  Id, DirectReportId → DirectReport (unique: one plan each), RoadmapName (copied),
  CurrentStage (Junior/Mid/Senior/Staff), Notes

CareerPlanSkill
  Id, CareerPlanId → CareerPlan, Section, Title, Description, Category, IsOptional, FromStage,
  SortOrder, Level (0–5), IsFocus, Evidence, LastLevelUpAt?

Parent
  Id, Name, Labels, Components, TeamTaskId?, JiraKey? (unique), JiraIssueType?

OneOnOneMeeting
  Id, DirectReportId? → DirectReport, MeetingDate (DateOnly), Title, Content, Tags

ManagerNote
  Id, Title, Content, Tags, FolderId? → NoteFolder, IsPinned

NoteFolder
  Id, Name, ParentFolderId? → NoteFolder, SortOrder

Leave
  Id, DirectReportId → DirectReport, Type, Status, StartDate, EndDate, Notes?

Product
  Id, Name (unique), Description, JiraComponentId? (unique)

Project
  Id, Name, Description, Url

ProjectProduct
  Id, ProjectId → Project, ProductId → Product            (unique pair)

ProjectKnowledge
  Id, DirectReportId → DirectReport, ProjectId → Project, KnowledgeLevel (1–5)


TeamTask
  Id, Title, Description, Type, Priority, Status, StatusChangedAt,
  AssigneeId? → DirectReport, ProjectId? → Project, ParentId? → Parent,
  DueDate?, StoryPoints?,
  Tags, Labels, Components?, Sprint, PreviousSprintsStoryPoints?, OverriddenFields

Sprint
  Id, Name, TeamName, Quarter, Year, SprintNumber, StartDate?, EndDate?

SprintCapacity
  Id, SprintId → Sprint, TotalCapacityPoints, AvailableMembers

SprintGoal
  Id, QuarterId → Quarter, SprintId → Sprint, Goal, Notes     ← the grid's sprint notes

Quarter
  Id, Year, QuarterNumber, Name, Status, OkrReference,
  ProductRoadmapPercent, TechRoadmapPercent, MaintenancePercent, CommittedAt?, Baseline (JSON)

QuarterMember
  Id, QuarterId → Quarter, DirectReportId → DirectReport, CapacityPercent   (unique pair)

Initiative
  Id, QuarterId → Quarter, Name, Description, Color, TshirtSize, Url, WorkType,
  Kind (Option/Leftover/Bucket), Status, Rank, Readiness (flags), DecisionNote,
  EarliestSprintId? → Sprint, ArrivedOn?, DueDate?

InitiativeProduct
  Id, InitiativeId → Initiative, ProductId → Product                        (unique pair)

InitiativeMember
  Id, InitiativeId → Initiative, DirectReportId → DirectReport, Role (Lead/Contributor)

ExternalDependency
  Id, InitiativeId → Initiative, Team, Need, Status

Allocation
  Id, InitiativeId → Initiative, DirectReportId → DirectReport, SprintId → Sprint, Percent (1–100)

InitiativeDependency
  Id, DependentInitiativeId → Initiative, DependencyInitiativeId → Initiative, Type, Notes

Activity
  Id, ActivityType, EntityType, EntityId, EntityName, Description, Timestamp
  (EntityType keeps Document, ChecklistTemplate and ChecklistInstance, features since
  removed, so older activity still reads)

SentimentAnalysisCache
  Id, DirectReportId → DirectReport, PositiveScore, NeutralScore, NegativeScore,
  OverallSentiment, KeyThemesJson, TrendDataJson, Reasoning,
  NotesAnalyzed, DaysAnalyzed, LatestNoteDate, AnalyzedAt

RetroSummary
  Id, Summary, WarningsJson, ActionItemsJson, SuggestionsJson, Model, RetrosAnalyzed,
  LatestRetroDate, SourceFingerprint, AnalyzedAt

TaskReadinessReview
  Id, TeamTaskId → TeamTask (unique), Verdict (Ready/NeedsWork/Unclear), Summary, MissingJson,
  Model, SourceFingerprint, AnalyzedAt

AppSettings
  Id, TshirtSizeMappings, SentimentProvider, ClaudeApiKey?, OllamaBaseUrl, OllamaModel,
  SentimentAnalysisDays, SentimentAnalysisEnabled, RetroSummaryEnabled, RetroSummaryCount,
  TaskReviewEnabled,
  SprintTeamFilter?,
  MaxInProgressTasks, MaxBlockedTasks, MaxInReviewTasks, MinProductMembers, MinMemberProducts, OneOnOneCadenceDays,
  StaleTaskDays,
  MaintenanceLabels, WorkCategoryEpicNames, JiraBaseUrl?, JiraEmail?, JiraApiToken?, JiraBoardId?,
  JiraProjectKey?, GoogleCalendarUrl?
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

Other entities enforce narrower invariants the same way, such as `Quarter` (`Activate`,
`Complete`, `ResetToPlanning`).

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

## AI Models: Ollama and Claude

Hive has three AI features, all opt-in: sentiment analysis of 1:1 notes, the retro summary
(§ Retro Summary) and the task readiness review (§ Task Readiness). `AppSettings.SentimentProvider`
chooses the model for all three: **Ollama** (a local model, the default) or **Claude**.
`SentimentAnalysisService`, `RetroSummaryService` and `TaskReadinessService` read the setting and call `IOllamaApiService` or `IClaudeApiService`; everything after that
(caching, the DTOs) is the same whichever model answered.

- **One question for both models.** `SentimentPrompt`, `RetroPrompt` and `TaskPrompt` (Infrastructure) each
  build a prompt, hold its JSON schema, and read the answer. Both clients use them, so the two
  models are asked the same thing and their results compare. Each client sends every request
  through one private method (`CreateJsonAsync` for Claude, `ChatJsonAsync` for Ollama), so the
  refusal, truncation and context checks below apply to every feature.
- **One failure type.** `ClaudeApiException` and `OllamaApiException` both derive from
  `AiProviderException`. The sentiment controller maps it to **502 Bad Gateway** with the
  message shown to the manager; the retro summary and the task review return it inside their
  DTO instead, next to the last result.

### Ollama

```
OllamaApiService (Infrastructure/Services)  ← registered via AddHttpClient, 10 min timeout
        │  POST {OllamaBaseUrl}/api/chat   stream: false, format: the JSON schema,
        │                                  temperature 0, num_ctx sized to the prompt
        │  GET  {OllamaBaseUrl}/api/tags   "Test connection": is the model installed?
        ▼
   Ollama (default http://localhost:11434, model llama3.1:8b)
```

- **Structured outputs:** the schema goes in Ollama's `format`, which constrains the model's
  output to it, so a small model still returns parseable JSON.
- **The context window is sized, not assumed.** Ollama's default window is a few thousand
  tokens and silently drops whatever does not fit, which would lose the oldest notes. The
  client asks for a window sized to the prompt (8k upwards, at most 128k), and if Ollama
  reports the prompt filled it, the analysis fails with a message rather than a result that
  read only some notes. `done_reason: "length"` (a cut-off answer) fails the same way.
- **Failures the manager can act on:** Ollama not running, the model not installed (with the
  `ollama pull` command to fix it), a timeout, or anything else Ollama reports.
- There is no key. The address and model live in `AppSettings`, and the address is not a
  secret, so the API returns it.

### Claude

```
SentimentAnalysisController (Api)   ClaudeApiException → 502 Bad Gateway
        │
        ▼
SentimentAnalysisService (Application)
        │  reads the key from AppSettings, checks the cache,
        │  depends on IClaudeApiService (Application/Interfaces)
        ▼
ClaudeApiService (Infrastructure/Services)  ← registered via AddHttpClient
        │  official Anthropic C# SDK (NuGet `Anthropic`), over the factory's HttpClient
        ▼
   Anthropic API   POST /v1/messages (beta), GET /v1/models/{id}
```

- **The official SDK, not hand-written HTTP.** `ClaudeApiService` builds an
  `AnthropicClient` per call around the `HttpClient` that
  `AddHttpClient<IClaudeApiService, ClaudeApiService>()` supplies, so the factory still owns
  connection lifetime. That `HttpClient` has an infinite timeout; the SDK applies its own
  (5 minutes) and retries rate limits, server errors and dropped connections itself.
  Never instantiate `HttpClient` directly.
- **Model:** `claude-opus-5`, set in one constant. It thinks adaptively by default, so no
  `thinking` parameter is sent, and `max_tokens` is 16,000 to leave room for that thinking.
- **Structured outputs:** the answer's shape is enforced with a JSON schema in
  `output_config.format`, so the response is always parseable JSON. The prompt no longer
  describes the format, and nothing is scraped out of free text.
- **Refusals:** a policy decline arrives as a normal response with `stop_reason: "refusal"`.
  Requests opt into server-side fallbacks (`fallbacks: "default"`, beta
  `server-side-fallback-2026-07-01`), so a decline is retried on the model Anthropic
  recommends for that category. The stop reason is checked before any content is read, and
  after a fallback only the content following the last `fallback` block is used.
- **Notes are data.** Each 1:1 body goes into the prompt inside its own
  `<note date="…">` element with markup escaped, so markdown in a note cannot be mistaken
  for instructions or run into the next note.
- The API key is stored in **`AppSettings` in the database**, not in configuration. The
  manager enters it on the Settings page at runtime. `SentimentAnalysisService` reads it
  and passes it to `IClaudeApiService`, which does not touch persistence. This is the same
  split as the Jira client, and it lets both be tested against a stubbed
  `HttpMessageHandler`.
- Validating a key looks up the model (`GET /v1/models/claude-opus-5`), which confirms the
  key works and can use the model without generating any tokens.
- Every failure (bad key, missing permission, rate limiting, outage, network, refusal,
  truncated answer) becomes a `ClaudeApiException` with a message the manager can act on.
  The controller maps it to **502 Bad Gateway**; the UI shows the message.
- Results are cached in `SentimentAnalysisCache`, which records what was analysed so
  staleness can be detected.
- With Claude chosen and no key configured, the feature is inert. Nothing is sent anywhere.

## Retro Summary

The Retro summary tile in Team health summarises the notes in the Retro folder with the model
chosen above. Its rules are in [BUSINESS.md § Retro summary](BUSINESS.md#retro-summary).

```
RetroSummaryController (Api)   GET  /api/retro-summary          cached, or made again when stale
        │                      POST /api/retro-summary/refresh  made again
        ▼
RetroSummaryService (Application)
        │  AppSettings: RetroSummaryEnabled, RetroSummaryCount, the model
        │  INoteFolderRepository + IManagerNoteRepository: the notes of every folder where
        │    NoteFolder.IsRetro holds
        │  Retrospective.Latest / .Fingerprint (Core): which retros, and what they are now
        │  IRetroSummaryRepository: the one cached RetroSummary
        ▼
IClaudeApiService / IOllamaApiService .SummarizeRetrosAsync   ← RetroPrompt (Infrastructure)
```

- **Stale by fingerprint, not by date.** `Retrospective.Fingerprint` hashes each retro's note
  id, last change and date. `RetroSummary.IsStaleFor` compares it with the one stored, so
  adding, editing or removing a retro, or changing how many are read, all make the summary
  stale; a GET then makes it again before answering.
- **A failure is part of the answer.** The service catches `AiProviderException` and returns
  the cached summary with `Error` set and `IsUpToDate: false`, so the tile can keep showing the
  last summary and say why it is not newer. The controller therefore always answers 200.
- **No folder, no call.** With the feature off, no key for Claude, no Retro folder or no
  written retro, the service returns what is missing and asks no model.
- **Retros are data.** Each retro goes into the prompt inside its own `<retro date="…">`
  element, oldest first, with markup escaped, as the 1:1 notes are for sentiment.
- **The answer is bounded.** `RetroPrompt.Parse` keeps at most 5 warnings, 10 action items and
  5 suggestions, each clipped to 500 characters, so the stored lists always fit their columns.
  An action item's date that is not `yyyy-MM-dd` is dropped rather than guessed.
- The cache is in both backups, like every table (§ Backups).

## Task Readiness

The Task readiness tile reviews the upcoming tasks with the model chosen above. Its rules
are in [BUSINESS.md § Task readiness](BUSINESS.md#task-readiness).

```
TaskReadinessController (Api)   GET  /api/task-readiness                  facts and reviews, no model
        │                       POST /api/task-readiness/review-next      reviews one task
        │                       POST /api/task-readiness/tasks/{id}/review  reviews that task again
        ▼
TaskReadinessService (Application)
        │  TaskReadiness.FindUpcoming / FindSprints / Fingerprint (Core): which tasks, and what they are
        │  TeamTask.HasAcceptanceCriteria (Core): the fact, no model
        │  ITaskReadinessReviewRepository: one cached TaskReadinessReview per task
        ▼
IClaudeApiService / IOllamaApiService .ReviewTaskAsync   ← TaskPrompt (Infrastructure)
```

- **One task per request.** A local model takes 10–30 seconds a task, so no request reviews a
  whole sprint. The tile calls `review-next` in a loop while it is on screen, one call at a
  time (a ref guards the call in flight, so a re-render never sends a task twice), and stops
  on the first error or when paused. Nothing runs in the background.
- **Stale by fingerprint.** `TaskReadiness.Fingerprint` hashes the title, type and description;
  a Jira sync that changes none of them keeps the review.
- **Facts are not judgement.** `TeamTask.IsMissingAcceptanceCriteria` is a rule on the entity.
  `ReportingService.BuildDashboardWarnings` raises `MissingAcceptanceCriteria` from it for the
  upcoming tasks, AI or not; the AI verdict never reaches Needs attention.
- **The bee badge rides on the task.** `TeamTaskDto.AiReview` carries each task's review
  (`TeamTaskService` loads the reviews of a page in one query), so the Tasks page needs no second
  call.
- **Reviews follow their tasks.** Every read removes the reviews of tasks that no longer exist,
  so a task Jira stops returning takes its review with it.
- Descriptions come from Jira whole, as Markdown (§ Jira API Integration), so the model sees
  headings such as "Acceptance criteria" as headings.
- The cache is in both backups, like every table (§ Backups).

---

## Jira API Integration

Hive reads from Jira Cloud on demand: sprint dates, products (components), parents and
tasks. The client is the place to add further reads.

```
JiraSyncController (Api)
  POST /api/jira-sync               everything; each part's result or error
  POST /api/jira-sync/sprint-dates  Sprints page
  POST /api/jira-sync/products      Products page
  POST /api/jira-sync/parents       Parents page
  POST /api/jira-sync/tasks         Tasks page (parents first)
        │
        ▼
JiraSyncService (Application)
        │  reads AppSettings; sprint dates by sprint name through ISprintService,
        │  products by component id through IProductRepository,
        │  parents by issue key through IParentRepository, tasks by their jira:KEY tag
        │  mapped by JiraTaskRules (shared with the CSV import)
        │  depends on IJiraApiClient (Application/Interfaces)
        ▼
JiraApiClient (Infrastructure/Services)  ← registered via AddHttpClient, 30 s timeout
        │  GET  /rest/agile/1.0/board/{id}/sprint   paged by startAt
        │  GET  /rest/api/3/field                   finds the Story Points and Sprint fields
        │  GET  /rest/api/3/project/{key}/components  every component, one response
        │  POST /rest/api/3/search/jql              paged by nextPageToken
        ▼
   Jira Cloud
```

- **One mapping for both paths.** `JiraTaskRules` holds how a Jira issue becomes a
  `TeamTask`: status, type and priority mapping, assignee lookup, story points, and the
  create and update rules that respect `OverriddenFields`. The CSV import and the API sync
  both call it, so the same issue produces the same task either way.
- **Custom fields are found, not configured.** Story points and sprints live in custom
  fields whose ids differ per site. The client reads `/rest/api/3/field` and picks the field
  typed `gh-sprint`, and "Story Points" (or "Story point estimate") of type number.
- **Descriptions** arrive in Atlassian Document Format (JSON) and are turned into Markdown by
  `AtlassianDocumentText` in Infrastructure, next to the client that receives them: headings,
  bold, italic and code, lists and checklists, code blocks, quotes and pipe tables. They are
  stored whole; the description column has no length limit.
- **Mirroring is guarded.** The product, parent and task syncs remove records Jira no
  longer returns, but only after the whole fetch has succeeded and returned something, and
  only records that carry a Jira key or component id. `Fingerprint` compares a task before and after applying Jira's
  data, so an unchanged task is not rewritten and "updated" counts real changes.
- Each sync has its own configuration check on `AppSettings`:
  `GetMissingJiraSprintSyncSettings()` needs the board ID, and
  `GetMissingJiraParentSyncSettings()` needs the space key, for products, parents and tasks
  alike. "Sync all" runs every part and
  reports a part that fails in place of its result, so one missing setting does not block
  the rest.
- Parents are matched on `Parent.JiraKey`, which has a unique index. `Parent.FromJira` and
  `Parent.SyncFromJira` are the only ways a key, issue type and components are set, and a
  parent cannot change issue.
- Products are matched on `Product.JiraComponentId`, which has a unique index, the same way.
  A product made by hand is linked by name on the first sync that finds its component.

### Products: how work is attributed

A product is a Jira component. **Nothing stores which product a task or parent belongs to**:
`Product.IsNamedIn(components)` decides it from the task's or parent's own `Components`
text, ignoring case. So a product renamed in Jira keeps its work as soon as the next task
sync brings the new component names, and a product made by hand gets its work the moment
its name matches. Projects are linked to products through `ProjectProduct`, set by hand on
the project; a project is never linked to tasks.
- **A parent is any issue with children, not only an epic.** JQL cannot ask for "issues
  that have sub-tasks", so `GetParentIssuesAsync` makes three searches on one session:
  the epics (`issuetype = Epic`), the sub-tasks (`issuetype in subTaskIssueTypes()`) to
  learn their parents' keys, then those parents by key (`key in (...)`, 50 per search).
  Every search asks only for the fields Hive reads (`summary`, `issuetype`, `status`,
  `parent`, `components`).

- Credentials live in **`AppSettings` in the database**, entered on the Settings page. The
  site root is derived from `JiraBaseUrl` by `AppSettings.GetJiraSiteUrl()`, so the one URL
  serves both issue links and API calls.
- Authentication is HTTP Basic with the account email and an Atlassian API token. The token
  is write-only through the API: `AppSettingsDto` exposes only `HasJiraApiToken`. It is not
  included in the JSON backup.
- **Classic and scoped tokens both work.** A classic token is accepted on the site URL. A
  scoped token ("API token with scopes") is rejected there with a 401 and only works
  through the gateway, `https://api.atlassian.com/ex/jira/{cloudId}`. On a 401 the client
  looks up the cloud ID from the site's public `/_edge/tenant_info` endpoint and retries
  through the gateway, then stays there for the rest of the call. Only if the gateway also
  answers 401 are the credentials reported as rejected. A scoped token costs one extra
  request and lookup per sync.
- **A failed login is not always a 401.** JQL search answers a rejected login with `200 OK`
  and no issues, run as an anonymous user, flagging the failure only in the
  `X-Seraph-LoginReason: AUTHENTICATED_FAILED` header. The client treats that header as a
  401, so an empty result always means Jira really has nothing to return.
- On the gateway, a scoped token missing a scope gets a 401 "scope does not match"; that is
  reported as a missing scope rather than as bad credentials.
- `JiraApiClient` is a pure HTTP adapter. It takes a `JiraConnection` rather than reading
  settings itself, so it has no dependency on persistence and is tested against a stubbed
  `HttpMessageHandler`.
- Every failure Jira can produce (bad credentials, missing board, a Kanban board with no
  sprints, rate limiting, network errors) becomes a `JiraApiException` whose message is safe
  to show the manager. The controller maps it to **502 Bad Gateway**; missing configuration
  is a `ValidationException` and maps to **400**.
- Dates are applied through `ISprintService.UpdateAsync`, not written to the repository
  directly, so a synced date behaves exactly like one typed on the Sprints page: capacity is
  recalculated and an activity is logged.

---

## Career Plans

One career plan per direct report records their skills: which they have, which not yet, which
they are working on now. On top of the ratings, two things are derived on every read: where the
skills sit on the career ladder, and a small game. It is the manager's private view; direct
reports never see it, and **nothing ever compares one person's plan with another's**.

```
SkillRoadmapsController   /api/skill-roadmaps           roadmaps, their skills, the library
CareerPlansController     /api/career-plans             plans, skill levels and focus, the overview
        │
        ▼
SkillRoadmapService ── RoadmapLibrary (Application)     the built-in roadmaps, as code
CareerPlanService   ── CareerLadder (Core)              stage fit, reading, strengths, weaknesses
                    ── CareerProgress (Core)            XP, player level, badges
        │
        ▼
ISkillRoadmapRepository · IRoadmapSkillRepository · ICareerPlanRepository · ICareerPlanSkillRepository
```

- **A plan has no goal.** `CareerPlan.CurrentStage` is the stage of the person's role, a fact
  the manager updates on a promotion. Stages are only ever a yardstick:
  `CareerStageExpectations.For` gives the level a stage expects on a skill, given the stage the
  skill is expected from (`FromStage`).
- **A plan copies its roadmap**, every skill of every stage. Editing or deleting a roadmap never
  touches a plan.
- **The ladder reading is derived.** `CareerLadder.Assess` (Core) counts, for each stage, the
  expectations the person's skills meet; their skills sit at the highest stage met at 80% or
  more (with every stage before it), and that reads above, at or below their role. Strengths and
  weaknesses are read against their own stage only.
- **The game is derived, and private to the plan.** `CareerProgress.Calculate` (Core) computes
  XP, the player level and title, cleared sections and badges. It appears only in
  `CareerPlanDto.Game`: list summaries (`CareerPlanSummaryDto`) and the dashboard overview
  (`CareerOverviewDto`) carry no scores at all, so there is nothing to set side by side.
- **Rating a skill reports what it unlocked.** `PUT .../skills/{id}/level` returns a
  `SkillLevelChangeDto`: the plan, whether the skill and the player levelled up, and the badges
  newly earned, compared before and after on the server, so the page only celebrates.
- **The library is code, not seed data.** `RoadmapLibrary` holds the built-in roadmaps as
  static definitions. Adding one (`POST /api/skill-roadmaps/library/{key}`) creates an ordinary
  `SkillRoadmap` with a `LibraryKey`, so an existing database gets them on demand without a
  data migration, and the manager's edits are never overwritten.
- One plan per direct report is enforced by a unique index and checked in `CareerPlanService`
  (`ExistsForDirectReportAsync`) for a friendly conflict. The `SimplifyCareerPlans` migration
  kept each person's newest plan when that rule arrived.
- A plan nothing on which changed for `CareerPlan.QuietDays` (60) raises the `CareerPlans`
  warning in `ReportingService.BuildDashboardWarnings`.
- Career plans are in both backups, like every table (§ Backups).

## Quarterly Planning

The planning page follows the manager's quarter in four steps: **Options** (what the PM
proposes, sized and designed), **Capacity** (who is here, the mix, what fits), **Allocation**
(people on options, sprint by sprint) and **Commit & review** (the commitment, then each option
against it). Every number on it is derived server-side on each read; nothing but the
commitment's baseline is stored.

```
QuarterlyPlanningController (Api)   /api/quarterly-planning
        │  quarters, initiatives, members, dependencies, roster, cells (writes)
        │  board, draft, scenario, commit, review, calibration, export (reads)
        ▼
QuarterlyPlanningService (Application)    changes; cross-entity checks (same quarter, one lead,
        │                                  a cell ≤ 100%), the default roster made concrete
PlanningBoardService (Application)        loads a quarter into a QuarterPlan and maps it
        │── QuarterPlan                    supply, forecasts, waterline, mix, draft, scenarios
        │── PlanningInsights               what the plan warns about
        │── PlanCatalogueExport            the Plan-Catalogue spreadsheet (ClosedXML)
        ▼
Core: Initiative, Quarter, Allocation, ExternalDependency, QuarterMember,
      InitiativeProduct, SprintAvailability
```

- **The unit is the dev-sprint**: one person for one whole sprint. A T-shirt size maps to
  dev-sprints in `AppSettings.TshirtSizeMappings`; a size with no mapping counts as nothing and
  raises `UnknownSize`. Sizes are validated by shape on the entity (`S`, `M`, `L`, `XL`, `XXL`,
  optionally with `+`), and by mapping on the board.
- **Supply** is `SprintAvailability.Availability` (working days less active leave, public
  holidays included) × the person's `QuarterMember.CapacityPercent`. An allocation is worth its
  percent of that supply, so planning someone on leave plans less than it looks.
- **The roster** is the quarter's `QuarterMember` rows or, while there are none, every direct
  report at 100% (`QuarterPlan.ResolveRoster`). The first roster change writes that default
  down, so it never shifts under the manager afterwards.
- **The waterline** is the options in priority order (leftovers, then `Rank`, unranked last;
  dropped and lanes left out) against the capacity for options: supply less the Maintenance and
  Tech roadmap shares of the quarter's mix. Standing lanes (`InitiativeKind.Bucket`) are sized by
  that share, never by a T-shirt.
- **A cell is written whole** (`PUT quarters/{id}/cells`): the grid sends each person-sprint with
  all its shares, `Allocation.ValidateCell` checks them, and the service adds, updates and deletes
  allocations to match. Painting many cells is one call.
- **The draft** (`QuarterPlan.Draft`) only fills free cells, in 25% steps: options above the line
  or committed, in priority order, their lead first (then contributors, then the people who
  know the product best, then whoever is freest), from the option's earliest sprint, never taking
  the lanes' share of a sprint; then the lanes towards the target mix. `apply=false` previews.
- **Scenarios** reuse the same plan with people's supply set to zero for some sprints, or an
  option's allocations before a sprint ignored (`QuarterPlan.WithScenario`). Nothing is saved.
- **Knowledge** comes from the option's products: their projects (`ProjectProduct`) give
  `ProjectKnowledge` levels, and tasks whose components name them give who has done the work.
  Suggestions and the `LeadKnowledge` insight use both.
- **The commitment** serialises a `BaselineDto` into `Quarter.Baseline`. The review compares
  each option then and now.
- **Calibration** reads every delivered option across quarters: its size's estimate against the
  dev-sprints its quarter's plan gave it.
- Like every table, planning is in both backups (§ Backups).

## Google Calendar Integration

Hive shows the manager's Google Calendar on the Daily Planner by reading the calendar's
**secret address in iCal format**. There is no OAuth, no Google Cloud project and no API key:
the address is the credential.

```
GoogleCalendarController (Api)
  GET  /api/google-calendar/events?from=yyyy-MM-dd&to=yyyy-MM-dd   the last sync only; never asks Google
  POST /api/google-calendar/sync?from=yyyy-MM-dd&to=yyyy-MM-dd     asks Google unless synced < 30 min ago
        │                        GoogleCalendarException → 502, a bad range → 400
        ▼
GoogleCalendarService (Application)
        │  reads GoogleCalendarUrl from AppSettings; not set → IsConfigured: false, no request
        │  depends on IGoogleCalendarClient (Application/Interfaces)
        ▼
GoogleCalendarClient (Infrastructure/Services)  ← registered via AddHttpClient, 30 s timeout
        │  GoogleCalendarRead.CachedOnly or CachedOrFetch
        ├──> GoogleCalendarFeedCache (singleton)   the parsed feed, for 30 minutes
        │  GET the .ics feed, parsed and expanded with Ical.Net
        ▼
   calendar.google.com
```

- **Why not sign in with a Google account?** Google no longer accepts a password from a
  third-party app, and its CalDAV endpoint only takes OAuth tokens. The secret iCal address is
  the one way to read a private calendar without registering an OAuth client.
- **Fetched on a sync, held in memory for 30 minutes, never stored.** Google is asked only
  when the manager presses **Sync Google Calendar**. The parsed feed is then held by
  `GoogleCalendarFeedCache`, a singleton, for 30 minutes from the fetch: opening the page,
  changing month and syncing again are all served from memory until it expires. It is the
  whole feed, not one month's events, so any month can be expanded from it. Nothing goes in
  the database, so there is no entity, repository or migration beyond the address itself, and
  a restart simply empties the cache.
- **Opening the page never asks Google.** `GET events` reads only the cache and answers
  `IsLoaded: false` when it is empty or expired. The UI then offers the Sync button and shows
  when the last sync ran (`SyncedAt`).
- The cache holds one feed, keyed by a SHA-256 hash of the secret address, so saving a new
  address misses it rather than showing the old calendar. A failed fetch stores nothing.
  Ical.Net evaluates recurrences on shared state, so every read of the cached calendar takes
  its lock. Expiry is measured with an injected `TimeProvider`, which tests replace.
- **Recurrence is expanded by Ical.Net**, including moved and excluded instances
  (`RECURRENCE-ID`, `EXDATE`). Cancelled events are dropped. A range is capped at 100 days
  and an answer at 5,000 occurrences so a runaway rule cannot stall a request.
- **Times cross the API as UTC instants** (`StartsAt`, `EndsAt`) and the client shows them in
  the machine's local zone. All-day events cross as dates (`StartDate`, `EndDate`, the end
  made inclusive), never as instants, so no time zone moves them to the wrong day. The
  service widens the requested range by a day each side for the same reason.
- **The address is a secret.** It is only accepted over `https`, stored in `AppSettings`,
  never returned by the API (`AppSettingsDto` exposes only `HasGoogleCalendarUrl`), never
  logged, never put in an error message, and not included in the JSON backup.
- A failure (address reset, network, rate limiting, an HTML page instead of a feed) becomes a
  `GoogleCalendarException` with a message the manager can act on. The Calendar page shows it
  inline and still shows notes and 1:1s.

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

- **Logs.** The main process writes every log entry to a session file in
  `userData/logs/` (JSON lines, one file per run, the last 10 kept, 5 MB each), so the log
  of a run that crashed survives it. Entries come from four sources: the renderer's console
  and uncaught errors, failed API calls (the Axios interceptor in `services/api.ts`), the
  main process, and the bundled backend's console output, parsed by
  `electron/backendOutput.ts`. The renderer sends its own entries over IPC (`log:write`)
  and reads earlier sessions back (`log:list-sessions`, `log:read-session`). The Logs page
  groups repeats and builds an error report with the app version, OS and the entries
  leading up to each problem. The older plain-text `hive-debug.log` is still written by
  packaged builds for the debug scripts.
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
bundle and restart it. T-shirt sizes, dashboard thresholds, support
and maintenance labels, the sentiment model (Ollama or Claude, and its key), the Jira base
URL and API credentials, and the
Google Calendar address are all database-backed.

CORS allows exactly one origin, `http://localhost:5173`, for the Vite dev server.

---

## Backups

Hive keeps two backups, and both hold every table and every stored field.

| | Automatic | Manual |
|---|---|---|
| When | When the backend starts, then every hour while it runs, if something changed (SQLite mode) | **Export Backup** on the Settings page |
| What | A copy of `hive.db`, next to it: `hive.db.backup.<timestamp>`; the newest 24 kept, then one a day for 30 days | A JSON file, `BackupDto` format 2.0 |
| Secrets | Included: it is the database file | Never: the Claude key, Jira token and Google Calendar address are left out |
| Restore | Quit Hive, replace `hive.db` with the copy | **Choose Backup File**: adds what is missing, keeps what exists, takes settings but not secrets |

- **The automatic copy is SQLite's own.** `DatabaseBackupService` writes it with `VACUUM INTO`:
  one consistent snapshot read while the app goes on writing (a file copy of a live database
  can catch it half-written), including what still sits in `hive.db-wal` after a backend was
  killed rather than stopped, written compacted. It goes to `hive.db.backing-up` first and is
  renamed when complete, so a crash never leaves half a backup.
- **Nothing is copied twice.** When neither `hive.db` nor its WAL was written since the newest
  backup's snapshot (its file time), no backup is taken. An idle Hive adds no files.
- **When.** `DatabaseInitializer` takes one at startup, before the migrations run;
  `DatabaseBackupScheduler`, a hosted service, takes one every hour after, off the request
  path. A lock keeps two from running at once. The in-memory database has no file, so it
  registers `NoDatabaseBackups` instead.
- **Kept.** The newest 24 (a day of hourly copies), then the newest of each earlier day for 30
  days. A file whose name carries no timestamp is never deleted.
- **Seen.** `GET /api/backup/automatic` (`IAutomaticBackups`, implemented by
  `DatabaseBackupStatus`) says where the backups are, how many there are and the last one;
  the Settings page shows it under Backup & restore.
- **The JSON backup is generic.** `BackupService` lists every table once, in the order a restore
  adds them, and `EntitySnapshot` captures each row as every property with a setter (computed
  ones are left out). A restore builds the entity through its parameterless constructor and sets
  those properties, the way EF materialises a row, so nothing is replayed through the domain's
  methods and nothing is lost. `BackupIntegrationTests` fails when a table in `HiveDbContext`
  is missing from the list, and round-trips every table through both persistence modes.
- **A new table** is one line in `BackupService`'s table list; its repository needs
  `GetAllAsync` and `AddAsync`.
- A backup in format 1.0 (typed lists of some tables) still restores, through the older path.
- **A column default needs a sentinel when zero is a real value.** EF skips a property whose
  value is the CLR default and lets the database fill in its default, so `WorkType.Maintenance`
  (0) and a 0% share of the mix were stored as the defaults. `HasSentinel(-1)` on those columns,
  as on the career stages, makes EF write them, and the `FixMaintenanceLanes` migration put the
  standing lanes stored as Product roadmap back to Maintenance.

## Dashboard

The Dashboard is **action-first**. It reads top to bottom as "what needs me now", then "how
has delivery been going", then "how is the team":

```
Header           date · Sync Jira · Customize
Today            Needs attention (the warning list)  │  Current sprint
People           1:1s  │  To-dos  │  Who's out  │  Milestones (only in a week with one)
Delivery         Task readiness                      (unstarted work in the current and next sprint)
                 Velocity │ Capacity                  (each with its own sprint window)
                 Carry-over trend │ Bug & support inflow   (each with its own sprint window)
                 Workload                            (all sprints)
                 Work mix │ Product engagement   (all sprints │ its own period in months)
Team health      Team sentiment                      (all sprints)
                 Retro summary                       (the latest retros)
                 Career plans                        (today: what needs the manager, no scores)
```

The widgets live in two component files: `DashboardFocus.tsx` for the top half and
`DashboardAnalytics.tsx` for Delivery and Team health. `Dashboard.tsx` only fetches, lays out, and
remembers which widgets are visible (`hive-dashboard-widgets-v2` in local storage; unknown
keys are dropped, so a removed widget never lingers in Customize).

### Two data sources, two time frames

| Source | Endpoint | Time frame | Feeds |
|--------|----------|------------|-------|
| Focus panel | `GET /api/reports/focus` → `DashboardFocusDto` | Today | Current sprint, 1:1s, To-dos, Who's out, Milestones |
| Dashboard overview | `GET /api/reports/dashboard` → `DashboardOverviewDto` | All sprints | Needs attention, Workload, Work mix |
| Velocity | `GET /api/reports/team-velocity?sprintCount=N` | The widget's own sprint window | Velocity |
| Capacity | `GET /api/reports/capacity-analysis?sprintCount=N` | The widget's own sprint window | Capacity |
| Carry-over | `GET /api/reports/carry-over?sprintCount=N` → `CarryOverTrendDto` | The widget's own sprint window | Carry-over trend |
| Inflow | `GET /api/reports/bug-support-inflow?sprintCount=N` → `BugSupportInflowDto` | The widget's own sprint window | Bug & support inflow |
| Product engagement | `GET /api/reports/product-engagement?months=N` → `ProductEngagementDto` | Its own period: 3 months (default), 6, or all time | Product engagement |

There is no page-wide sprint filter. The four widgets that chart sprint by sprint each have
their own **sprint window** in their header (Current, Last 3, Last 6, Last 12, All; All by
default, not remembered), built on `SprintHistoryCard` in `DashboardAnalytics.tsx`. Each
fetches its own data, so it is only fetched while the widget is visible, and fetches again
when its window changes or the dashboard reloads (its `reloadKey` is the overview's
`generatedAt`, which changes after a Jira sync). Changing one widget's window never touches
another. The dashboard overview and the focus panel are fetched once on load and again
after a Jira sync. The overview endpoint still accepts `sprintCount`, but the dashboard no
longer sends it.

`Sprint.FindCurrent` (Core) decides the current sprint for the focus panel, the capacity
analysis and the Tasks page's "Current sprint" filter (`GET /api/sprints/current`): the
sprint containing today, else the next to start, else the most recent. The focus panel also reports the sprint's **pace** (done work against working days
gone), computed server-side like everything else on the page.

### Time frame, widget by widget

| Widget | Data source | Time frame |
|--------|-------------|------------|
| Needs attention | `dashboard.insights.warnings` | All sprints, not badged: it is about today |
| Current sprint, 1:1s, To-dos, Who's out, Milestones | `reportsApi.getFocus()` | Today, not badged: they are explicitly "now" |
| Task readiness | `taskReadinessApi.get()` in `TaskReadinessCard` → `TaskReadinessOverviewDto` | The current and next sprint, badged "Current & next sprint"; its AI verdicts stay on the tile |
| Velocity | `reportsApi.getTeamVelocity` in `VelocityCard` | Its own sprint window |
| Capacity | `reportsApi.getCapacityAnalysis` in `CapacityCard` | Its own sprint window |
| Carry-over trend | `reportsApi.getCarryOverTrend` in `CarryOverCard` | Its own sprint window |
| Bug & support inflow | `reportsApi.getBugSupportInflow` in `BugSupportInflowCard` | Its own sprint window |
| Workload | `dashboard.tasks.tasksByAssignee` (By status), `supportDistribution` (Support & maintenance) | All sprints — `ALL_TIME_BADGE` |
| Work mix | `dashboard.tasks.tasksByType*`, `tasksByLabel`, `tasksByComponent`, `tasksByWorkCategory` (from `WorkCategories` in Core) | All sprints — `ALL_TIME_BADGE` |
| Product engagement | `reportsApi.getProductEngagement(months)` | Its own period selector (3 months, 6 months, All time), not badged |
| Team sentiment | `sentimentApi` (own component) | All sprints — `ALL_TIME_BADGE` |
| Retro summary | `retroSummaryApi.get()` in `RetroSummaryCard` → `RetroSummaryDto` | The latest N retros, badged "Latest N retros"; its warnings stay on the tile, not in Needs attention |
| Career plans | `careerPlansApi.getOverview()` (own component) → `CareerOverviewDto` | Today, not badged: who has no plan, nothing in focus, quiet plans |

`ALL_TIME_BADGE` is exported from `DashboardAnalytics.tsx`.

### All sprints means the warnings are stable

The dashboard asks for the overview without a `sprintCount`, so `tasks` and `insights` both
cover every sprint. `workloadWarnings` is derived from `tasksByAssignee`, which is why it used
to follow the old page-wide filter; now it, and the Needs attention count, never depend on
what a widget is showing.

### To-dos come from the journal

Notes carry no to-do state. The To-dos card shows `DashboardFocusDto.Todos`, which
`ReportingService` reads from the latest entry of the journal: the notes of every folder
where `NoteFolder.IsJournal` holds, and of those the one `ManagerNote.FindLatestJournalEntry`
picks (the greatest title). Note lists use the same order, pinned first and then title
descending, through `NoteText.TitleComparer` in memory and SQLite's `NOCASE` collation in the
database, which compare identically, so the top unpinned note in Journal is always the one
the card reads. `ManagerNote.GetHotItems` lists the bullets under its `hot`
heading, parsed by `NoteText.ExtractSectionItems` in Core. The DTO carries the note's id,
and each row opens it through `/notes?note=<id>`, which the Notes page reads on load. There
is no to-do warning.

### The warning list is server-side

`insights.warnings` is the complete list, assembled by `BuildDashboardWarnings`, and the
Needs attention card only renders it. Deriving it in the client made the total depend on
which widgets were visible, because velocity and capacity are only fetched for a visible
widget. Warnings are ordered people first — overdue 1:1s (`OneOnOnes`), `Workload`,
`UnengagedMembers`, `NarrowlyEngagedMembers`, quiet career plans (`CareerPlans`) — then delivery risks, led by `OverdueTasks`
(open tasks past their due date, all-time) and `StaleTasks` (in-flight tasks whose status
has not changed for `StaleTaskDays`). Each carries a `Category`
the client maps to a destination page via `WARNING_ROUTES`.

### Stale tasks: Hive's own clock

`TeamTask.StatusChangedAt` is set by the entity whenever its status actually changes, in
every transition method. A Jira sync or CSV import changes status through those same methods
(`JiraTaskRules.UpdateTaskStatus`), so the time recorded is **when Hive saw the change**, not
the transition time Jira holds, and a sync that finds a task where it was leaves it alone.
`TeamTask.IsStale` and the repositories' `TeamTaskFilter.StaleBefore` apply the same rule: an
in-flight status (`TeamTask.InFlightStatuses`) unchanged since the cutoff. The migration that
added the column filled it with each task's last update, so staleness is never overstated.

### Drill-downs

Every delivery widget opens the tasks behind it on the Tasks page, through query parameters
the page reads on load and shows as removable chips: `assignee` (an id, or `unassigned`),
`type` (a `TaskType` name), `label`, `component`, `sprint`, `filter` (`overdue`, `stale`,
or a status), `unattributed` (tasks whose components name no product, the Unattributed tasks
warning's drill-down), `unestimated` (the tasks the "no story points" warning counts,
`TeamTask.FindMissingStoryPoints`), `noAcceptanceCriteria` (the upcoming stories and tasks
the acceptance criteria warning counts), `search` (a Jira key or title, from the Task readiness
tile) and `excludeParents`. They map onto `TeamTaskFilter`, so the filtering happens server-side like the
page's own filters. The Tasks page filters one sprint at a time, so a widget scoped to the
last N sprints opens all of that person's, type's or label's tasks rather than exactly the
N sprints it counted.

**Product engagement is server-side too.** `ReportingService.BuildProductEngagement`
attributes every task but an epic to the products its components name, counting only direct
reports. The client cannot compute this — task fetches are paged, and `PaginationParams` caps
a page at 100 — so it would only ever see a slice of the tasks. The widget's period is a
number of months, and a task is in it when one of its sprints overlaps it
(`GetEstimatedStartDate`/`GetEstimatedEndDate`). The dashboard insights call the same
builder over all time for the `UnworkedProducts`, `KnowledgeSilos`, `UnengagedMembers` and
`NarrowlyEngagedMembers` warnings, so the warnings never follow the widget's period.

**When adding a dashboard widget** to the Delivery section, decide its time frame. A widget
that charts sprint by sprint is built on `SprintHistoryCard`, which gives it its own sprint
window. A widget with another period of its own gets its own selector, as Product engagement
does, or a badge naming a fixed one, as Task readiness does ("Current & next sprint"). Any other
Delivery widget covers all sprints and adds `badge={ALL_TIME_BADGE}` to its
`CardHeader`. A widget that silently shows all-time data next to windowed widgets is a bug.

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

1. Add the data source — a `ReportingService` method, or an existing API. Anything derived
   from the full task set belongs in `ReportingService`, not in the page: task fetches are
   paged and capped at 100 rows.
2. Decide where it goes: "now" data belongs in the focus panel (`/reports/focus`,
   `DashboardFocus.tsx`); delivery history belongs in the Delivery section, and how people
   are doing in Team health (`DashboardAnalytics.tsx`).
   A Delivery widget that charts sprint by sprint uses `SprintHistoryCard` for its own sprint
   window; any other Delivery widget without a period selector of its own gets
   `badge={ALL_TIME_BADGE}`.
3. Add a key to `WidgetVisibility`, `DEFAULT_WIDGETS` and `WIDGET_LABELS`, and gate the
   widget on it — every widget on the page is toggleable from Customize.
4. If it can raise a warning, add it to `BuildDashboardWarnings` with a `Category`, and map
   that category in `WARNING_ROUTES`. A widget that raises a warning only while it is
   visible is a bug.
5. Update the tables in this document.

---

## What To Do When Unsure Where Something Goes

1. **Is it a domain rule, an invariant, or an entity?** → `Hive.Core`
2. **Is it orchestration across entities, or a DTO?** → `Hive.Application`
3. **Is it a call to an external system (database, Claude API, filesystem)?** → `Hive.Infrastructure`
4. **Is it HTTP in/out, auth, or DI wiring?** → `Hive.Api`
5. **Is it purely visual?** → `Hive.Desktop`
6. **Still unsure?** → Ask.
