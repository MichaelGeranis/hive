# Hive — Codebase Map

This file describes the project structure. It is read by engineers and AI agents to
understand what exists before writing code. **Keep it up to date** — a file added without
a line here is a file the next contributor will not find.

For the reasoning behind this structure, see [ARCHITECTURE.md](ARCHITECTURE.md).

## Architecture

Clean Architecture with four .NET layers + an Electron/React desktop client:

```
Core (entities, interfaces, enums)      → zero infrastructure dependencies
Application (services, DTOs)            → depends on Core only
Infrastructure (EF Core, SQLite, Claude) → implements Core interfaces
Api (controllers, auth, DI)             → thin HTTP layer
Desktop (Electron + React)              → presentation only, calls the API
```

**Two persistence stacks implement the same interfaces.** Every repository exists twice:
an in-memory implementation for development and a SQLite implementation for production.
See [ARCHITECTURE.md § Dual Persistence](ARCHITECTURE.md#dual-persistence).

## Project Structure

```
src/
├── Hive.Core/                                   — Domain layer, no infrastructure deps
│   ├── Hive.Core.csproj                         — No references: Core depends on nothing
│   ├── Entities/
│   │   ├── Activity.cs                          — Audit trail entry
│   │   ├── ActivityEnums.cs                     — ActivityType, EntityType
│   │   ├── Allocation.cs                        — Person + initiative + sprint, for a share (1–100%); the cell rule (≤ 100%)
│   │   ├── AppSettings.cs                       — Runtime configuration, database-backed
│   │   ├── CareerEnums.cs                       — SkillCategory, CareerSkillStatus (not yet, working on, has), LadderReading
│   │   ├── CareerLadder.cs                      — Where a person's skills sit on the ladder against their stage; strengths, weaknesses
│   │   ├── CareerPlan.cs                        — A direct report's one plan: the stage of their role, notes; the quiet rule
│   │   ├── CareerPlanSkill.cs                   — One skill on a plan: level 0–5, working on it, evidence, level-ups
│   │   ├── CareerProgress.cs                    — The game, derived: XP, player level and title, sections cleared, badges
│   │   ├── CareerStage.cs                       — Junior, Mid, Senior, Staff; the level each stage expects on a skill
│   │   ├── DependencyType.cs                    — Kind of initiative dependency
│   │   ├── DirectReport.cs                      — A person the manager is responsible for
│   │   ├── ExternalDependency.cs                — What an initiative needs from another team, and its status
│   │   ├── Initiative.cs                        — An option, leftover or standing lane on a quarter's plan: size, rank, status, readiness, carry-over
│   │   ├── InitiativeProduct.cs                 — An initiative touching a product (for knowledge and suggestions)
│   │   ├── InitiativeDependency.cs              — Directed dependency between initiatives
│   │   ├── InitiativeMember.cs                  — A person on an initiative: lead or contributor
│   │   ├── Leave.cs                             — Time a person is unavailable
│   │   ├── LeaveEnums.cs                        — LeaveType, LeaveStatus
│   │   ├── ManagerNote.cs                       — Manager's note: markdown body, folder, pin; hot items and the latest journal entry
│   │   ├── NoteFolder.cs                        — Folder grouping manager notes, nestable; recognises the Journal and Retro folders
│   │   ├── NoteText.cs                          — Shared markdown rules: derived titles, tag normalisation, bullets under a heading
│   │   ├── OneOnOneCadence.cs                   — When a 1:1 is due: the cadence rule and OneOnOneCadenceStatus
│   │   ├── OneOnOneMeeting.cs                   — One 1:1, written as a single markdown note
│   │   ├── Parent.cs                            — Jira parent issue grouping tasks; JiraKey and JiraIssueType link it to Jira, Components name its products
│   │   ├── Product.cs                           — A Jira component; decides which components name it (IsNamedIn)
│   │   ├── Project.cs                           — A code repository serving products
│   │   ├── ProjectProduct.cs                    — A project serving a product (many-to-many link)
│   │   ├── ProjectKnowledge.cs                  — One person's 1–5 knowledge level on a project
│   │   ├── PlanningEnums.cs                     — InitiativeKind, InitiativeStatus, ReadinessIssue, InitiativeRole, ExternalDependencyStatus
│   │   ├── Quarter.cs                           — Planning period: status, target mix, commitment baseline
│   │   ├── QuarterEnums.cs                      — QuarterStatus
│   │   ├── QuarterMember.cs                     — A person on a quarter's plan and their planned capacity
│   │   ├── RetroSummary.cs                      — The cached AI summary of the latest retros: warnings, action items, suggestions; stale by fingerprint
│   │   ├── Retrospective.cs                     — One retro (a note in the Retro folder) and its date; the latest N; their fingerprint
│   │   ├── RoadmapSkill.cs                      — One skill on a skill roadmap, in a section
│   │   ├── SentimentAnalysisCache.cs            — Cached sentiment result
│   │   ├── SentimentProvider.cs                 — Which model reads the notes: Ollama (local) or Claude
│   │   ├── SkillLevel.cs                        — The 0–5 skill level scale and its labels
│   │   ├── SkillRoadmap.cs                      — A reusable roadmap of skills for a role
│   │   ├── Sprint.cs                            — Time-boxed iteration with deterministic ordering; FindCurrent
│   │   ├── SprintCapacity.cs                    — Team capacity for one sprint
│   │   ├── SprintAvailability.cs                — A person's availability in a sprint: working days less leave
│   │   ├── SprintGoal.cs                        — Notes for a sprint in a quarter
│   │   ├── TaskEnums.cs                         — TaskPriority, TaskStatus, TaskType
│   │   ├── TaskReadiness.cs                     — TaskReadinessVerdict; the upcoming tasks (current and next sprint, not started), their fingerprint
│   │   ├── TaskReadinessReview.cs               — The cached AI review of one task: verdict, summary, what is missing
│   │   ├── TeamTask.cs                          — Delivery work item + its state machine; StatusChangedAt and the stale rule; acceptance criteria
│   │   ├── TenureMilestone.cs                   — Six months, then every year: the milestone shown in the week before it
│   │   ├── WorkCategory.cs                      — WorkCategory, and WorkCategories: a task's epic, and the category its name gives
│   │   └── WorkType.cs                          — Maintenance, ProductRoadmap, TechRoadmap
│   ├── Exceptions/
│   │   └── DomainException.cs                   — DomainException, NotFoundException, ConflictException
│   └── Interfaces/                              — Repository contracts, implemented twice in Infrastructure
│       ├── IActivityRepository.cs
│       ├── IAllocationRepository.cs
│       ├── IAppSettingsRepository.cs
│       ├── ICareerPlanRepository.cs
│       ├── ICareerPlanSkillRepository.cs
│       ├── IDirectReportRepository.cs
│       ├── IExternalDependencyRepository.cs
│       ├── IInitiativeDependencyRepository.cs
│       ├── IInitiativeMemberRepository.cs
│       ├── IInitiativeProductRepository.cs
│       ├── IInitiativeRepository.cs
│       ├── ILeaveRepository.cs
│       ├── IManagerNoteRepository.cs
│       ├── INoteFolderRepository.cs
│       ├── IOneOnOneMeetingRepository.cs
│       ├── IParentRepository.cs
│       ├── IProjectKnowledgeRepository.cs
│       ├── IProductRepository.cs
│       ├── IProjectProductRepository.cs
│       ├── IProjectRepository.cs
│       ├── IQuarterMemberRepository.cs
│       ├── IQuarterRepository.cs
│       ├── IRetroSummaryRepository.cs
│       ├── IRoadmapSkillRepository.cs
│       ├── ISentimentAnalysisCacheRepository.cs
│       ├── ISkillRoadmapRepository.cs
│       ├── ISprintCapacityRepository.cs
│       ├── ISprintGoalRepository.cs
│       ├── ISprintRepository.cs
│       ├── ITaskReadinessReviewRepository.cs
│       └── ITeamTaskRepository.cs
│
├── Hive.Application/                            — Services and DTOs, depends on Core only
│   ├── Hive.Application.csproj                  — References Hive.Core only; packages: ClosedXML (the Microsoft.Extensions abstractions come with .NET 11)
│   ├── DependencyInjection.cs                   — Registers all application services
│   ├── Formatting/
│   │   └── DisplayFormat.cs                     — dd/MM/yyyy for dates in activity text, insights and Excel
│   ├── DTOs/
│   │   ├── ActivityDto.cs                       — Activity feed payloads
│   │   ├── AppSettingsDto.cs                    — Settings read/update payloads
│   │   ├── BackupDto.cs                         — Backup file: every table (format 2.0), the typed 1.0 lists for old files; restore result
│   │   ├── CareerPlanDto.cs                     — Career plan payloads: skills by section, ladder reading, game, level changes, overview
│   │   ├── DirectReportDto.cs                   — Team member payloads
│   │   ├── GoogleCalendarDto.cs                 — Google Calendar events in a date range
│   │   ├── JiraImportDto.cs                     — Import preview, request and result
│   │   ├── JiraSyncDto.cs                       — Sprint date, parent and sync-all results
│   │   ├── LeaveDto.cs                          — Leave payloads
│   │   ├── ManagerNoteDto.cs                    — Manager note payloads
│   │   ├── NoteFolderDto.cs                     — Note folder payloads
│   │   ├── OneOnOneMeetingDto.cs                — 1:1 payloads: body, tags, resolved person
│   │   ├── PaginationDto.cs                     — Shared paged-result wrapper
│   │   ├── ParentDto.cs                         — Parent work item payloads
│   │   ├── ProductDto.cs                        — Product payloads: task counts, parents, serving projects
│   │   ├── ProjectDto.cs                        — Project payloads, with the products each serves
│   │   ├── ProjectKnowledgeDto.cs               — Knowledge level payloads
│   │   ├── QuarterlyPlanningDto.cs              — Quarter, initiative, grid, board (capacity, waterline, mix, insights), draft, scenario, review payloads
│   │   ├── ReportDto.cs                         — Dashboard, focus panel, velocity, capacity payloads
│   │   ├── RetroSummaryDto.cs                   — Retro summary payload: the retros read, summary, warnings, action items, suggestions, error
│   │   ├── SentimentAnalysisDto.cs              — Sentiment result and status payloads
│   │   ├── SkillRoadmapDto.cs                   — Roadmap, roadmap skill and library payloads
│   │   ├── SprintDto.cs                         — Sprint and capacity payloads
│   │   ├── TaskReadinessDto.cs                  — Task readiness overview, its items, and the AI review (the bee badge)
│   │   └── TeamTaskDto.cs                       — Task payloads
│   ├── Interfaces/                              — Service contracts consumed by controllers
│   │   ├── IActivityService.cs
│   │   ├── IAppSettingsService.cs
│   │   ├── IAutomaticBackups.cs                 — The automatic database backups: whether they run, where, the last one
│   │   ├── IBackupService.cs
│   │   ├── ICareerPlanService.cs
│   │   ├── IClaudeApiService.cs                 — Claude API abstraction, AiProviderException, ClaudeApiException; shared sentiment, retro and task review records
│   │   ├── IDirectReportService.cs
│   │   ├── IGoogleCalendarClient.cs             — Read-only iCal feed client contract, GoogleCalendarException
│   │   ├── IGoogleCalendarService.cs
│   │   ├── IJiraApiClient.cs                    — Read-only Jira Cloud client contract, JiraApiException
│   │   ├── IJiraImportService.cs
│   │   ├── IJiraSyncService.cs                  — On-demand pulls from Jira
│   │   ├── ILeaveService.cs
│   │   ├── IManagerNoteService.cs
│   │   ├── INoteFolderService.cs
│   │   ├── IOllamaApiService.cs                 — Local model (Ollama) sentiment, retro summary and task review contract, OllamaConnection, OllamaApiException
│   │   ├── IOneOnOneMeetingService.cs
│   │   ├── IParentService.cs
│   │   ├── IProjectKnowledgeService.cs
│   │   ├── IProductService.cs
│   │   ├── IPlanningBoardService.cs
│   │   ├── IProjectService.cs
│   │   ├── IQuarterlyPlanningService.cs
│   │   ├── IReportingService.cs
│   │   ├── IRetroSummaryService.cs
│   │   ├── ISentimentAnalysisService.cs
│   │   ├── ISkillRoadmapService.cs
│   │   ├── ISprintCapacityService.cs
│   │   ├── ISprintService.cs
│   │   ├── ITaskReadinessService.cs
│   │   └── ITeamTaskService.cs
│   └── Services/
│       ├── ActivityService.cs                   — Writes and queries the audit trail
│       ├── AppSettingsService.cs                — Runtime settings read/update
│       ├── BackupService.cs                     — Export and restore of every table and settings (secrets aside); the 1.0 restore path
│       ├── CareerPlanService.cs                 — Career plans: one per person, rate skills and set focus, ladder reading, game, overview
│       ├── DirectReportService.cs               — Team member CRUD
│       ├── EntitySnapshot.cs                    — An entity as stored: every property with a setter, to JSON and back
│       ├── GoogleCalendarService.cs             — Google Calendar events for a range: from the last sync, or synced
│       ├── JiraImportService.cs                 — CSV preview, validation and import
│       ├── JiraSyncService.cs                   — Sprint dates, parents and tasks from Jira; mirrors Jira; sync all
│       ├── JiraTaskRules.cs                     — Jira issue → TeamTask mapping and update rules, shared with the CSV import
│       ├── LeaveService.cs                      — Leave CRUD and overlap queries
│       ├── ManagerNoteService.cs                — Manager notes: writing, folders, pinning
│       ├── NoteFolderService.cs                 — Note folders: nesting, cycle checks, non-destructive delete
│       ├── OneOnOneMeetingService.cs            — 1:1 CRUD and tag-to-person resolution
│       ├── ParentService.cs                     — Parent work item CRUD
│       ├── ProjectKnowledgeService.cs           — Knowledge level CRUD
│       ├── ProductService.cs                    — Product CRUD; attributes tasks and parents by component
│       ├── ProjectService.cs                    — Project CRUD and the products each serves
│       ├── PlanCatalogueExport.cs               — The plan as a Plan-Catalogue spreadsheet (ClosedXML)
│       ├── PlanningBoardService.cs              — The board, draft, scenarios, commitment, review, calibration, export
│       ├── PlanningInsights.cs                  — What a plan warns about: readiness, dependencies, leads, forecasts, mix
│       ├── QuarterPlan.cs                       — A quarter's plan as numbers: supply, forecasts, waterline, mix, draft; the roster rule
│       ├── QuarterlyPlanningService.cs          — Changes to a plan: quarters, initiatives, people, dependencies, roster, grid
│       ├── RoadmapLibrary.cs                    — The built-in skill roadmaps (Backend, Frontend, ASP.NET Core, DevOps, QA, Architect, EM)
│       ├── RetroSummaryService.cs               — The retro summary: the latest retros, cached until they change, on Ollama or Claude
│       ├── ReportingService.cs                  — Dashboard, focus panel (sprint, 1:1 cadence, journal hot to-dos, leave, milestones), velocity, capacity, carry-over, bug & support inflow
│       ├── SentimentAnalysisService.cs          — Sentiment with caching, on Ollama or Claude as chosen in settings
│       ├── SkillRoadmapService.cs               — Skill roadmaps and their skills; adding one from the library
│       ├── SprintCapacityService.cs             — Capacity CRUD
│       ├── SprintService.cs                     — Sprint CRUD and ordering
│       ├── TaskReadinessService.cs              — The upcoming tasks, their facts and AI reviews, one task per call, on Ollama or Claude
│       ├── TeamTaskService.cs                   — Task CRUD and status transitions
│       └── WorkMix.cs                           — Work by category in tasks and story points: the Dashboard's Work mix and the planning mix's past work
│
├── Hive.Infrastructure/                         — Frameworks and drivers
│   ├── Hive.Infrastructure.csproj               — EF Core with SQLite, the Anthropic SDK, Ical.Net; references Core and Application
│   ├── DependencyInjection.cs                   — AddInfrastructureServices (in-memory) + AddSqliteInfrastructureServices; DatabaseInitializer
│   ├── Migrations/                              — EF Core migrations (SQLite schema history)
│   ├── Persistence/
│   │   ├── DatabaseBackupScheduler.cs           — Hosted service: a database backup every hour while the backend runs
│   │   ├── DatabaseBackupService.cs             — Copies hive.db with VACUUM INTO when changed; keeps 24, then one a day for 30 days
│   │   ├── DatabaseBackupStatus.cs              — IAutomaticBackups over the backup files beside hive.db
│   │   ├── HiveDbContext.cs                     — EF Core context; entity mappings in OnModelCreating
│   │   ├── HiveDbContextFactory.cs              — Design-time factory for `dotnet ef`
│   │   ├── InMemoryDbContext.cs                 — ConcurrentDictionary store; NOT the EF in-memory provider
│   │   ├── NoDatabaseBackups.cs                 — IAutomaticBackups for the in-memory database: nothing to back up
│   │   └── Repositories/                        — In-memory repository implementations
│   │       ├── ActivityRepository.cs
│   │       ├── AllocationRepository.cs
│   │       ├── AppSettingsRepository.cs
│   │       ├── CareerPlanRepository.cs
│   │       ├── CareerPlanSkillRepository.cs
│   │       ├── DirectReportRepository.cs
│   │       ├── ExternalDependencyRepository.cs
│   │       ├── InitiativeDependencyRepository.cs
│   │       ├── InitiativeMemberRepository.cs
│   │       ├── InitiativeProductRepository.cs
│   │       ├── InitiativeRepository.cs
│   │       ├── LeaveRepository.cs
│   │       ├── ManagerNoteRepository.cs
│   │       ├── NoteFolderRepository.cs
│   │       ├── OneOnOneMeetingRepository.cs
│   │       ├── ParentRepository.cs
│   │       ├── ProjectKnowledgeRepository.cs
│   │       ├── ProductRepository.cs
│   │       ├── ProjectProductRepository.cs
│   │       ├── ProjectRepository.cs
│   │       ├── QuarterMemberRepository.cs
│   │       ├── QuarterRepository.cs
│   │       ├── RetroSummaryRepository.cs
│   │       ├── RoadmapSkillRepository.cs
│   │       ├── SentimentAnalysisCacheRepository.cs
│   │       ├── SkillRoadmapRepository.cs
│   │       ├── SprintCapacityRepository.cs
│   │       ├── SprintGoalRepository.cs
│   │       ├── SprintRepository.cs
│   │       ├── TaskReadinessReviewRepository.cs
│   │       ├── TeamTaskRepository.cs
│   │       └── Sqlite/                          — EF Core repository implementations (same interfaces)
│   │           ├── DbSetExtensions.cs           — UpdateUntracked: saves an entity whichever instance of its row is tracked
│   │           ├── SqliteActivityRepository.cs
│   │           ├── SqliteAllocationRepository.cs
│   │           ├── SqliteAppSettingsRepository.cs
│   │           ├── SqliteCareerPlanRepository.cs
│   │           ├── SqliteCareerPlanSkillRepository.cs
│   │           ├── SqliteDirectReportRepository.cs
│   │           ├── SqliteExternalDependencyRepository.cs
│   │           ├── SqliteInitiativeDependencyRepository.cs
│   │           ├── SqliteInitiativeMemberRepository.cs
│   │           ├── SqliteInitiativeProductRepository.cs
│   │           ├── SqliteInitiativeRepository.cs
│   │           ├── SqliteLeaveRepository.cs
│   │           ├── SqliteManagerNoteRepository.cs
│   │           ├── SqliteNoteFolderRepository.cs
│   │           ├── SqliteOneOnOneMeetingRepository.cs
│   │           ├── SqliteParentRepository.cs
│   │           ├── SqliteProjectKnowledgeRepository.cs
│   │           ├── SqliteProductRepository.cs
│   │           ├── SqliteProjectProductRepository.cs
│   │           ├── SqliteProjectRepository.cs
│   │           ├── SqliteQuarterMemberRepository.cs
│   │           ├── SqliteQuarterRepository.cs
│   │           ├── SqliteRetroSummaryRepository.cs
│   │           ├── SqliteRoadmapSkillRepository.cs
│   │           ├── SqliteSentimentAnalysisCacheRepository.cs
│   │           ├── SqliteSkillRoadmapRepository.cs
│   │           ├── SqliteSprintCapacityRepository.cs
│   │           ├── SqliteSprintGoalRepository.cs
│   │           ├── SqliteSprintRepository.cs
│   │           ├── SqliteTaskReadinessReviewRepository.cs
│   │           └── SqliteTeamTaskRepository.cs
│   └── Services/
│       ├── AtlassianDocumentText.cs             — Jira rich text (Atlassian Document Format) to Markdown
│       ├── ClaudeApiService.cs                  — IClaudeApiService via the Anthropic SDK: structured output, refusal fallbacks; sentiment, retros, task reviews
│       ├── GoogleCalendarClient.cs              — IGoogleCalendarClient over the secret iCal address, parsed with Ical.Net
│       ├── GoogleCalendarFeedCache.cs           — Holds the last fetched feed in memory for 30 minutes (singleton)
│       ├── JiraApiClient.cs                     — IJiraApiClient over the Jira Cloud REST API
│       ├── OllamaApiService.cs                  — IOllamaApiService over Ollama's REST API: structured output, sized context; sentiment, retros, task reviews
│       ├── RetroPrompt.cs                       — The retro summary prompt, JSON schema and answer parsing, shared by both models
│       ├── SentimentPrompt.cs                   — The sentiment prompt, JSON schema and answer parsing, shared by both models
│       └── TaskPrompt.cs                        — The task readiness prompt, JSON schema and answer parsing, shared by both models
│
├── Hive.Api/                                    — Thin HTTP layer
│   ├── Hive.Api.csproj                          — References Application and Infrastructure; the ASP.NET Core host
│   ├── Program.cs                               — Startup, DI wiring, database mode selection, CORS, Swagger, /health
│   ├── appsettings.json                         — Credentials, logging, database mode
│   ├── appsettings.Development.json             — Development overrides
│   ├── Properties/
│   │   └── launchSettings.json                  — Local run profiles: port 5002, Development environment
│   ├── Authentication/
│   │   └── BasicAuthenticationHandler.cs        — HTTP Basic Auth against AdminCredentials
│   └── Controllers/
│       ├── ActivityFeedController.cs            — Activity feed and audit trail
│       ├── BackupController.cs                  — Export and restore
│       ├── CareerPlansController.cs             — Career plans, skill levels and focus, the dashboard overview
│       ├── DirectReportsController.cs           — Team members
│       ├── GoogleCalendarController.cs          — Google Calendar events from the last sync, and sync
│       ├── JiraImportController.cs              — CSV preview and import
│       ├── JiraSyncController.cs                — On-demand sync from Jira: all, sprint dates, parents, tasks
│       ├── LeavesController.cs                  — Leave records
│       ├── ManagerNotesController.cs            — Manager notes
│       ├── NoteFoldersController.cs             — Note folders
│       ├── OneOnOneMeetingsController.cs        — 1:1 meetings
│       ├── ParentsController.cs                 — Parent work items
│       ├── ProjectKnowledgeController.cs        — Project knowledge levels
│       ├── ProductsController.cs                — Products
│       ├── ProjectsController.cs                — Projects
│       ├── QuarterlyPlanningController.cs       — Quarters, initiatives, roster, grid, board, draft, scenario, commit, review, export
│       ├── ReportsController.cs                 — Dashboard, focus panel and analytics endpoints
│       ├── RetroSummaryController.cs            — The retro summary, and refreshing it
│       ├── SentimentAnalysisController.cs       — Sentiment analysis and key validation
│       ├── SettingsController.cs                — Runtime application settings
│       ├── SkillRoadmapsController.cs           — Skill roadmaps, their skills and the built-in library
│       ├── SprintCapacityController.cs          — Sprint capacity
│       ├── SprintsController.cs                 — Sprints
│       ├── TaskReadinessController.cs           — Task readiness: the overview, reviewing the next task, reviewing one again
│       └── TeamTasksController.cs               — Tasks
│
└── Hive.Desktop/                                — Electron 45 + React 19 + TypeScript + Vite + Tailwind 4
    ├── eslint.config.js                         — ESLint rules
    ├── index.html                               — Vite entry page
    ├── package.json                             — Scripts, dependencies and the electron-builder config (icons, targets)
    ├── package-lock.json                        — Locked dependency versions
    ├── postcss.config.js                        — PostCSS with the Tailwind plugin (@tailwindcss/postcss)
    ├── tsconfig.json                            — TypeScript config for the renderer
    ├── tsconfig.node.json                       — TypeScript config for Vite and the Electron main process
    ├── vite.config.ts                           — Vite build and dev server config
    ├── vitest.config.ts                         — Vitest config and coverage thresholds
    ├── electron/
    │   ├── backend.ts                           — Spawns and supervises the bundled .NET backend
    │   ├── backendOutput.ts                     — Splits the backend's console output into log entries with levels
    │   ├── backendOutput.test.ts                — Backend output parsing into levels
    │   ├── logger.ts                            — Main-process log: one session file per run in userData/logs, last 10 kept
    │   ├── main.ts                              — Electron main process, window lifecycle, log and app-info IPC
    │   └── preload.ts                           — Context-isolated renderer bridge
    ├── backend/                                 — Bundled backend binaries (build output, gitignored)
    └── src/
        ├── App.tsx                              — Route table
        ├── main.tsx                             — React entry point, mounts App
        ├── global.d.ts                          — Ambient types for the preload bridge
        ├── index.css                            — Tailwind import and theme (Hive colours, v3 palette), v3 compatibility rules, toast and Hive mark animations
        ├── vite-env.d.ts                        — Vite client types
        ├── assets/                              — Application icons (icns, ico, png, svg)
        ├── components/
        │   ├── AiBeeBadge.tsx                   — The AI bee badge on a reviewed task: verdict colour, summary and what is missing on hover
        │   ├── AllocationGrid.tsx               — Planning step 3: people × sprints, paint and split cells, forecasts, draft preview
        │   ├── AutomaticBackupsPanel.tsx        — Settings: when the database is backed up, the last backup, where
        │   ├── Card.tsx                         — Generic card + CardHeader used by dashboard widgets
        │   ├── CareerForms.tsx                  — Shared career modal, skill form fields and stage select
        │   ├── CareerGame.tsx                   — Career plan pieces: stage and quiet chips, level token, XP bar, badge shelf
        │   ├── CareerPlanDetail.tsx             — One person's plan: working on now, ladder position, strengths and weaknesses, skill map, editor
        │   ├── CollapsibleTags.tsx              — Tag/label chips collapsed past five, "+N more" to expand
        │   ├── DashboardAnalytics.tsx           — Dashboard delivery and team health widgets (incl. task readiness with its one-task-at-a-time AI review, the retro summary, and career plans: what needs the manager): velocity, capacity, carry-over, bug & support inflow (each with its own sprint window, SprintHistoryCard), workload (by status, and support & maintenance per person), work mix (type, label, component, work category pies), product engagement; drill-downs to Tasks; ALL_TIME_BADGE
        │   ├── DashboardFocus.tsx               — Dashboard focus widgets: needs attention, current sprint, 1:1s, to-dos, who's out, milestones
        │   ├── HiveMark.tsx                     — Hive's mark (three cells and a dot) and wordmark; the dot lights for Needs attention, the cells fill while busy
        │   ├── IssueTypeBadge.tsx               — Coloured Jira issue type badge (Epic purple, Story green, …)
        │   ├── JiraSyncButton.tsx               — The "Sync Jira" button used on the Dashboard and per page
        │   ├── Layout.tsx                       — App shell and navigation
        │   ├── LoadingScreen.tsx                — Backend startup state
        │   ├── MarkdownEditor.tsx               — Shared writing surface: toolbar, shortcuts, preview
        │   ├── MarkdownPreview.tsx              — Renders note markdown (GFM: tables, task lists)
        │   ├── MeetingEditor.tsx                — 1:1 editor: tags, resolved person chip, date
        │   ├── Modal.tsx                        — The one dialog: title, ×, Escape (innermost only), focus kept inside and returned, footer
        │   ├── NoteEditor.tsx                   — Note writing surface: markdown shortcuts, preview, tags
        │   ├── NoteFolderTree.tsx               — Folder sidebar with nesting, rename and drop targets
        │   ├── OptionDrawer.tsx                 — One option: decision, solution design, people, dependencies
        │   ├── PageHeader.tsx                   — The top of every page: title, subtitle, actions
        │   ├── PageState.tsx                    — LoadingState, ErrorState (with Retry) and EmptyState
        │   ├── PlanningCapacity.tsx             — Planning step 2: supply, waterline, mix (with the team's average), roster
        │   ├── PlanningOptions.tsx              — Planning step 1: the option catalogue, drag to rank, standing lanes
        │   ├── PlanningReview.tsx               — Planning step 4: commit, PM summary, options against the commitment, calibration
        │   ├── SentimentInsights.tsx            — Sentiment analysis display
        │   ├── SkillRoadmaps.tsx                — Roadmaps tab: the library, the manager's roadmaps, editing their skills
        │   ├── SkillTree.tsx                    — Roadmap-style skill map: sections along a path, skills coloured by status
        │   ├── SyncButton.tsx                   — Shared sync button with a spinner: Jira and Google Calendar
        │   ├── WorkCategoryNamesCard.tsx        — Settings: the epic names of each work category, added, removed and saved
        │   └── {AiBeeBadge,Card,CollapsibleTags,HiveMark,LoadingScreen,MarkdownPreview,Modal,SentimentInsights}.test.tsx
        ├── contexts/
        │   ├── ConfirmContext.tsx               — useConfirm(): an awaitable question in Hive's own dialog, instead of confirm()
        │   ├── HiveStatusContext.tsx            — What the mark shows from any page: Needs attention's count, and whether a sync or backup is running
        │   └── ToastContext.tsx                 — Transient notification state
        ├── hooks/
        │   ├── useEscapeKey.ts                  — Escape-to-close for modals
        │   └── useEscapeKey.test.ts             — Escape handling and cleanup
        ├── pages/
        │   ├── ActivityFeed.tsx                 — Audit trail
        │   ├── Calendar.tsx                     — Daily Planner: date-tagged notes, 1:1s and Google Calendar events
        │   ├── CareerPlans.tsx                  — Career plans and roadmaps; ?plan=<id>, ?tab=roadmaps&roadmap=<id>
        │   ├── Dashboard.tsx                    — Action-first dashboard: focus panel, then delivery history; widget visibility
        │   ├── DirectReports.tsx                — Team members
        │   ├── Leaves.tsx                       — Leave records
        │   ├── Logs.tsx                         — Log triage: grouped entries, filters, error report to copy, previous sessions
        │   ├── Meetings.tsx                     — 1:1s: people rail, 1:1 list, markdown editor; ?person=<id> opens one person
        │   ├── Notes.tsx                        — Notes: folder sidebar, note list, autosaving markdown editor; ?note=<id> opens one note
        │   ├── Parents.tsx                      — Parent work items
        │   ├── Products.tsx                     — Products: Jira sync, task progress, serving projects
        │   ├── ProjectKnowledge.tsx             — Knowledge matrix: manual 1–5 levels
        │   ├── Projects.tsx                     — Projects and the products each serves
        │   ├── QuarterlyPlanning.tsx            — Quarterly planning in four steps; ?step=, ?option=<id> opens an option
        │   ├── Settings.tsx                     — Runtime settings in sections (dashboard, planning, integrations, AI analysis: sentiment, task readiness and retro summary, backup); ?section= opens one
        │   ├── Sprints.tsx                      — Sprints and capacity
        │   ├── Tasks.tsx                        — Task board; ?sprint=current opens on the current sprint; ?assignee, ?type, ?label, ?component, ?unattributed, ?unestimated, ?noAcceptanceCriteria, ?search and ?filter=stale drill-downs; the AI bee badge on reviewed tasks
        │   ├── Tutorials.tsx                    — In-app documentation
        │   └── {Calendar,CareerPlans,Dashboard,Leaves,Logs,Meetings,Notes,Parents,Products,Projects,
        │       QuarterlyPlanning,Settings,Sprints,Tasks}.test.tsx
        ├── services/
        │   ├── api.ts                           — The single Axios client; all API calls live here
        │   ├── logStore.ts                      — Renderer log buffer: console, uncaught errors, API failures, main and backend; unseen and reported
        │   └── {api,logStore}.test.ts
        ├── test/
        │   ├── factories.ts                     — Test data builders
        │   ├── setup.ts                         — Vitest global setup
        │   ├── test-utils.tsx                   — Render helpers with providers
        │   └── mocks/
        │       ├── handlers.ts                  — MSW request handlers
        │       └── server.ts                    — MSW server lifecycle
        ├── types/
        │   ├── careerPlans.ts                   — Skill roadmap and career plan types
        │   ├── index.ts                         — Shared API types
        │   └── quarterlyPlanning.ts             — Planning-specific types
        └── utils/
            ├── apiErrors.ts                     — The API's message from a failed request, and the text to show for it
            ├── careerDisplay.ts                 — Skill level and status labels and colours
            ├── dateUtils.ts                     — formatDate / formatDateTime / formatTimestamp (dd/MM/yyyy), month and weekday names, date parsing and range helpers
            ├── issueTypeDisplay.ts              — One colour per Jira issue type, for the issue type badge
            ├── jiraSync.ts                      — Turns Jira sync results into the messages shown to the manager
            ├── logReport.ts                     — Log filtering, grouping of repeats, and the error report text
            ├── planningDisplay.ts               — Planning labels and colours, priority labels, the paint rule, update DTO helper
            ├── taskReadinessDisplay.ts          — Verdict labels and colours for the AI bee badge
            ├── taskStatusDisplay.ts             — One colour and label per task status, for dashboard charts
            └── {dateUtils,logReport}.test.ts

tests/
└── Hive.Tests/
    ├── Hive.Tests.csproj                        — xUnit, Moq, FluentAssertions, Coverlet; references every project
    ├── Core/Entities/                           — Domain rules; no mocks, no database
    │   └── {Activity,Allocation,AppSettings,CareerLadder,CareerPlan,CareerPlanSkill,CareerProgress,CareerStage,
    │       DirectReport,ExternalDependency,Initiative,
    │       InitiativeDependency,InitiativeMember,InitiativeProduct,Leave,ManagerNote,
    │       NoteFolder,NoteText,OneOnOneCadence,OneOnOneMeeting,Parent,Product,Project,ProjectKnowledge,ProjectProduct,
    │       Quarter,QuarterMember,RetroSummary,Retrospective,RoadmapSkill,SentimentAnalysisCache,SkillRoadmap,Sprint,SprintAvailability,SprintCapacity,SprintGoal,TaskReadiness,TaskReadinessReview,TeamTask,TenureMilestone,WorkCategory}Tests.cs
    ├── Core/Exceptions/
    │   └── ExceptionTests.cs                    — DomainException hierarchy behaviour
    ├── Application/Formatting/
    │   └── DisplayFormatTests.cs                — Date display format, culture-independent
    ├── Application/Services/                    — Service logic with mocked repositories (Moq)
    │   ├── PlanCatalogueExportTests.cs          — Plan-Catalogue sheet: colours, statuses, commit line, allocation cells, layout and print setup
    │   ├── QuarterPlanTests.cs                  — Supply, waterline, forecasts, mix, scenarios and the draft
    │   ├── RoadmapLibraryTests.cs               — Every built-in roadmap is well formed and builds valid entities
    │   ├── WorkMixTests.cs                      — Work categories by story points, and folded into the planning mix
    │   └── {Activity,AppSettings,Backup,CareerPlan,DirectReport,GoogleCalendar,JiraImport,
    │       JiraSync,Leave,ManagerNote,NoteFolder,OneOnOneMeeting,Parent,Product,
    │       ProjectKnowledge,Project,QuarterlyPlanning,Reporting,RetroSummary,
    │       SentimentAnalysis,SkillRoadmap,SprintCapacity,Sprint,TaskReadiness,TeamTask}ServiceTests.cs
    ├── Api/Authentication/
    │   └── BasicAuthenticationHandlerTests.cs   — Credential parsing and rejection
    ├── Api/Controllers/                         — Controller behaviour with mocked services
    │   └── {ActivityFeed,Backup,CareerPlans,DirectReports,
    │       GoogleCalendar,JiraImport,JiraSync,Leaves,ManagerNotes,OneOnOneMeetings,
    │       NoteFolders,Parents,Products,Projects,QuarterlyPlanning,Reports,RetroSummary,SentimentAnalysis,Settings,SkillRoadmaps,
    │       SprintCapacity,Sprints,TaskReadiness,TeamTasks}ControllerTests.cs
    ├── Infrastructure/Persistence/
    │   ├── DatabaseBackupSchedulerTests.cs      — The hourly backup runs on its tick and stops with the backend
    │   ├── DatabaseBackupServiceTests.cs        — Consistent copies, skipping unchanged, WAL, retention, status
    │   └── FixMaintenanceLanesMigrationTests.cs — The data migration that puts Maintenance lanes back, on real SQLite
    ├── Infrastructure/Services/
    │   ├── AtlassianDocumentTextTests.cs        — Rich text to Markdown
    │   ├── ClaudeApiServiceTests.cs             — Claude client against a stubbed HTTP handler
    │   ├── GoogleCalendarClientTests.cs         — iCal feed client and its 30-minute cache, against a stubbed HTTP handler
    │   ├── JiraApiClientTests.cs                — Jira client against a stubbed HTTP handler
    │   └── OllamaApiServiceTests.cs             — Ollama client against a stubbed HTTP handler
    ├── Infrastructure/Repositories/             — Repository implementations
    │   ├── SqliteTrackingTests.cs               — SQLite reads are untracked; saving a copy of a tracked row still works
    │   └── {Activity,Allocation,AppSettings,CareerPlan,CareerPlanSkill,
    │       DirectReport,ExternalDependency,Initiative,InitiativeProduct,Leave,ManagerNote,
    │       NoteFolder,OneOnOneMeeting,Parent,Product,Project,ProjectProduct,Quarter,QuarterMember,RetroSummary,RoadmapSkill,SentimentAnalysisCache,SkillRoadmap,
    │       SprintCapacity,SprintGoal,Sprint,TaskReadinessReview,
    │       TeamTask}RepositoryTests.cs
    └── Integration/                             — Real services, full DI container, in-memory database
        ├── IntegrationTestBase.cs               — Base class building the DI container
        ├── BackupIntegrationTests.cs            — Every table in the backup; export and restore round trips, in memory and SQLite
        ├── CareerPlansIntegrationTests.cs       — Library roadmap to plan, ratings and focus, one plan per person, promotion
        ├── DashboardFocusIntegrationTests.cs    — The focus panel's 1:1 cadence end to end
        ├── GoogleCalendarIntegrationTests.cs    — Google Calendar wiring, unconfigured and not-yet-synced states
        ├── JiraImportServiceIntegrationTests.cs — End-to-end CSV import
        ├── JiraSyncIntegrationTests.cs          — Jira sync wiring and configuration check
        ├── NotesIntegrationTests.cs             — End-to-end note writing, folders and pinning
        ├── OneOnOneIntegrationTests.cs          — End-to-end 1:1 writing and tag linking
        ├── ProductsIntegrationTests.cs          — Products, projects and component attribution end to end
        └── QuarterlyPlanningIntegrationTests.cs — A quarter planned end to end: suggestions, draft, scenario, commit, review, calibration

scripts/
├── build-backend.sh                             — Cross-platform .NET publish (osx-arm64, osx-x64, win-x64, linux-x64, all)
├── build-backend.bat                            — Windows batch equivalent
├── debug-prod-app.sh                            — Attach to a packaged build
├── debug-setup.sh                               — Prepare local debugging
└── generate-icons.sh                            — Generate platform icon assets

Root
├── ARCHITECTURE.md                              — Technical reference
├── BUSINESS.md                                  — Domain concepts and rules
├── CHANGELOG.md                                 — User-visible changes per release
├── CODE_OF_CONDUCT.md                           — Contributor Covenant
├── CODEBASE.md                                  — This file
├── CLAUDE.md                                    — Working agreement for AI agents
├── CONTRIBUTING.md                              — Setup, codebase rules and how to submit a change
├── LICENSE                                      — Apache License 2.0
├── NOTICE                                       — Copyright notice
├── README.md                                    — Entry point and getting started
├── SECURITY.md                                  — How to report a vulnerability; the security model
├── docs/DEBUGGING.md                            — IDE and browser debugging setups
├── docs/images/                                 — README screenshots
├── .github/workflows/ci.yml                     — Backend and frontend checks on every pull request
├── .github/workflows/release.yml                — Installers for macOS, Windows and Linux on a version tag
├── .github/ISSUE_TEMPLATE/, pull_request_template.md, dependabot.yml — Issue and PR templates, dependency updates
├── Makefile                                     — Primary developer entry point
├── Hive.sln                                     — Solution file
├── Directory.Build.props                        — Shared build settings for every project
├── Directory.Packages.props                     — Central NuGet package versions
├── global.json                                  — Pinned .NET SDK version
└── coverage.runsettings                         — Coverage collection and exclusions
```
