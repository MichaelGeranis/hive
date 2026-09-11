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
│   ├── Entities/
│   │   ├── Activity.cs                          — Audit trail entry
│   │   ├── ActivityEnums.cs                     — ActivityType, EntityType
│   │   ├── Allocation.cs                        — Person + initiative + sprint
│   │   ├── AppSettings.cs                       — Runtime configuration, database-backed
│   │   ├── ChecklistEnums.cs                    — ChecklistType, ChecklistItemType/Status, ChecklistInstanceStatus
│   │   ├── ChecklistInstance.cs                 — One run of a checklist template
│   │   ├── ChecklistInstanceItem.cs             — One item within a running checklist
│   │   ├── ChecklistTemplate.cs                 — Reusable checklist definition
│   │   ├── ChecklistTemplateItem.cs             — Item within a template
│   │   ├── DependencyType.cs                    — Kind of initiative dependency
│   │   ├── DirectReport.cs                      — A person the manager is responsible for
│   │   ├── Document.cs                          — Reference document or link
│   │   ├── Initiative.cs                        — Quarterly body of work
│   │   ├── InitiativeDependency.cs              — Directed dependency between initiatives
│   │   ├── InitiativeMember.cs                  — Person nominally on an initiative
│   │   ├── KnowledgePoint.cs                    — Earned evidence of project knowledge
│   │   ├── Leave.cs                             — Time a person is unavailable
│   │   ├── LeaveEnums.cs                        — LeaveType, LeaveStatus
│   │   ├── ManagerNote.cs                       — Manager's note: markdown body, folder, pin, optional TODO; NotePriority, NoteSortOrder
│   │   ├── NoteFolder.cs                        — Folder grouping manager notes, nestable
│   │   ├── NoteText.cs                          — Shared markdown rules: derived titles, tag normalisation
│   │   ├── OneOnOneMeeting.cs                   — One 1:1, written as a single markdown note
│   │   ├── Parent.cs                            — Jira parent work item grouping tasks
│   │   ├── PerformanceEnums.cs                  — PerformanceRating
│   │   ├── PerformanceReview.cs                 — Formal review for a named period
│   │   ├── Project.cs                           — A named body of work
│   │   ├── ProjectKnowledge.cs                  — One person's 1–5 knowledge level on a project
│   │   ├── Quarter.cs                           — Planning period with a status
│   │   ├── QuarterEnums.cs                      — QuarterStatus
│   │   ├── SentimentAnalysisCache.cs            — Cached Claude sentiment result
│   │   ├── Skill.cs                             — A named capability
│   │   ├── SkillAssessment.cs                   — One person's level and target in one skill
│   │   ├── SkillCategory.cs                     — Grouping of skills (SkillCategoryEntity)
│   │   ├── SkillEnums.cs                        — SkillCategory, ProficiencyLevel
│   │   ├── Sprint.cs                            — Time-boxed iteration with deterministic ordering
│   │   ├── SprintCapacity.cs                    — Team capacity for one sprint
│   │   ├── SprintGoal.cs                        — Stated goal for a sprint in a quarter
│   │   ├── TaskEnums.cs                         — TaskPriority, TaskStatus, TaskType
│   │   ├── TeamTask.cs                          — Delivery work item + its state machine
│   │   └── WorkType.cs                          — Maintenance, ProductRoadmap, TechRoadmap
│   ├── Exceptions/
│   │   └── DomainException.cs                   — DomainException, NotFoundException, ConflictException
│   └── Interfaces/                              — Repository contracts, implemented twice in Infrastructure
│       ├── IActivityRepository.cs
│       ├── IAllocationRepository.cs
│       ├── IAppSettingsRepository.cs
│       ├── IChecklistInstanceItemRepository.cs
│       ├── IChecklistInstanceRepository.cs
│       ├── IChecklistTemplateItemRepository.cs
│       ├── IChecklistTemplateRepository.cs
│       ├── IDirectReportRepository.cs
│       ├── IDocumentRepository.cs
│       ├── IInitiativeDependencyRepository.cs
│       ├── IInitiativeMemberRepository.cs
│       ├── IInitiativeRepository.cs
│       ├── IKnowledgePointRepository.cs
│       ├── ILeaveRepository.cs
│       ├── IManagerNoteRepository.cs
│       ├── INoteFolderRepository.cs
│       ├── IOneOnOneMeetingRepository.cs
│       ├── IParentRepository.cs
│       ├── IPerformanceReviewRepository.cs
│       ├── IProjectKnowledgeRepository.cs
│       ├── IProjectRepository.cs
│       ├── IQuarterRepository.cs
│       ├── ISentimentAnalysisCacheRepository.cs
│       ├── ISkillAssessmentRepository.cs
│       ├── ISkillCategoryRepository.cs
│       ├── ISkillRepository.cs
│       ├── ISprintCapacityRepository.cs
│       ├── ISprintGoalRepository.cs
│       ├── ISprintRepository.cs
│       └── ITeamTaskRepository.cs
│
├── Hive.Application/                            — Services and DTOs, depends on Core only
│   ├── DependencyInjection.cs                   — Registers all application services
│   ├── DTOs/
│   │   ├── ActivityDto.cs                       — Activity feed payloads
│   │   ├── AppSettingsDto.cs                    — Settings read/update payloads
│   │   ├── BackupDto.cs                         — Full export/restore payload
│   │   ├── ChecklistDto.cs                      — Template, instance and item payloads
│   │   ├── DirectReportDto.cs                   — Team member payloads
│   │   ├── DocumentDto.cs                       — Document payloads
│   │   ├── InsightsDto.cs                       — Quarterly planning insight payloads
│   │   ├── JiraImportDto.cs                     — Import preview, request and result
│   │   ├── KnowledgePointDto.cs                 — Points and level-upgrade suggestions
│   │   ├── LeaveDto.cs                          — Leave payloads
│   │   ├── ManagerNoteDto.cs                    — Manager note payloads
│   │   ├── NoteFolderDto.cs                     — Note folder payloads
│   │   ├── OneOnOneMeetingDto.cs                — 1:1 payloads: body, tags, resolved person
│   │   ├── PaginationDto.cs                     — Shared paged-result wrapper
│   │   ├── ParentDto.cs                         — Parent work item payloads
│   │   ├── PerformanceReviewDto.cs              — Review payloads
│   │   ├── ProjectDto.cs                        — Project payloads
│   │   ├── ProjectKnowledgeDto.cs               — Knowledge level payloads
│   │   ├── QuarterlyPlanningDto.cs              — Quarter, initiative, allocation payloads
│   │   ├── ReportDto.cs                         — Dashboard, velocity, capacity, accuracy payloads
│   │   ├── SentimentAnalysisDto.cs              — Sentiment result and status payloads
│   │   ├── SkillAssessmentDto.cs                — Assessment payloads
│   │   ├── SkillCategoryDto.cs                  — Category payloads
│   │   ├── SkillDto.cs                          — Skill payloads
│   │   ├── SprintDto.cs                         — Sprint and capacity payloads
│   │   └── TeamTaskDto.cs                       — Task payloads
│   ├── Interfaces/                              — Service contracts consumed by controllers
│   │   ├── IActivityService.cs
│   │   ├── IAppSettingsService.cs
│   │   ├── IBackupService.cs
│   │   ├── IChecklistService.cs
│   │   ├── IClaudeApiService.cs                 — Claude API abstraction, implemented in Infrastructure
│   │   ├── IDirectReportService.cs
│   │   ├── IDocumentService.cs
│   │   ├── IJiraImportService.cs
│   │   ├── IKnowledgePointService.cs
│   │   ├── ILeaveService.cs
│   │   ├── IManagerNoteService.cs
│   │   ├── INoteFolderService.cs
│   │   ├── IOneOnOneMeetingService.cs
│   │   ├── IParentService.cs
│   │   ├── IPerformanceReviewService.cs
│   │   ├── IProjectKnowledgeService.cs
│   │   ├── IProjectService.cs
│   │   ├── IQuarterlyPlanningInsightsService.cs
│   │   ├── IQuarterlyPlanningService.cs
│   │   ├── IReportingService.cs
│   │   ├── ISentimentAnalysisService.cs
│   │   ├── ISkillAssessmentService.cs
│   │   ├── ISkillCategoryService.cs
│   │   ├── ISkillService.cs
│   │   ├── ISprintCapacityService.cs
│   │   ├── ISprintService.cs
│   │   └── ITeamTaskService.cs
│   └── Services/
│       ├── ActivityService.cs                   — Writes and queries the audit trail
│       ├── AppSettingsService.cs                — Runtime settings read/update
│       ├── BackupService.cs                     — Full database export and restore
│       ├── ChecklistService.cs                  — Templates, instances, item copying
│       ├── DirectReportService.cs               — Team member CRUD
│       ├── DocumentService.cs                   — Document CRUD
│       ├── JiraImportService.cs                 — CSV preview, validation and import
│       ├── KnowledgePointService.cs             — Point calculation and level suggestions
│       ├── LeaveService.cs                      — Leave CRUD and overlap queries
│       ├── ManagerNoteService.cs                — Manager notes and pending TODOs
│       ├── NoteFolderService.cs                 — Note folders: nesting, cycle checks, non-destructive delete
│       ├── OneOnOneMeetingService.cs            — 1:1 CRUD and tag-to-person resolution
│       ├── ParentService.cs                     — Parent work item CRUD
│       ├── PerformanceReviewService.cs          — Review CRUD
│       ├── ProjectKnowledgeService.cs           — Knowledge level CRUD
│       ├── ProjectService.cs                    — Project CRUD
│       ├── QuarterlyPlanningInsightsService.cs  — Over-allocation, dependency risk, estimate mismatch
│       ├── QuarterlyPlanningService.cs          — Quarters, initiatives, allocations
│       ├── ReportingService.cs                  — Dashboard, velocity, capacity, estimation accuracy, Excel export
│       ├── SentimentAnalysisService.cs          — Claude-backed sentiment with caching
│       ├── SkillAssessmentService.cs            — Assessment CRUD and heatmap data
│       ├── SkillCategoryService.cs              — Category CRUD
│       ├── SkillService.cs                      — Skill CRUD
│       ├── SprintCapacityService.cs             — Capacity CRUD
│       ├── SprintService.cs                     — Sprint CRUD and ordering
│       └── TeamTaskService.cs                   — Task CRUD and status transitions
│
├── Hive.Infrastructure/                         — Frameworks and drivers
│   ├── DependencyInjection.cs                   — AddInfrastructureServices (in-memory) + AddSqliteInfrastructureServices; DatabaseInitializer
│   ├── Migrations/                              — EF Core migrations (SQLite schema history)
│   ├── Persistence/
│   │   ├── DatabaseBackupService.cs             — File-level SQLite backup and restore
│   │   ├── HiveDbContext.cs                     — EF Core context; entity mappings in OnModelCreating
│   │   ├── HiveDbContextFactory.cs              — Design-time factory for `dotnet ef`
│   │   ├── InMemoryDbContext.cs                 — ConcurrentDictionary store; NOT the EF in-memory provider
│   │   └── Repositories/                        — In-memory repository implementations
│   │       ├── ActivityRepository.cs
│   │       ├── AllocationRepository.cs
│   │       ├── AppSettingsRepository.cs
│   │       ├── ChecklistInstanceItemRepository.cs
│   │       ├── ChecklistInstanceRepository.cs
│   │       ├── ChecklistTemplateItemRepository.cs
│   │       ├── ChecklistTemplateRepository.cs
│   │       ├── DirectReportRepository.cs
│   │       ├── DocumentRepository.cs
│   │       ├── InitiativeDependencyRepository.cs
│   │       ├── InitiativeMemberRepository.cs
│   │       ├── InitiativeRepository.cs
│   │       ├── KnowledgePointRepository.cs
│   │       ├── LeaveRepository.cs
│   │       ├── ManagerNoteRepository.cs
│   │       ├── NoteFolderRepository.cs
│   │       ├── OneOnOneMeetingRepository.cs
│   │       ├── ParentRepository.cs
│   │       ├── PerformanceReviewRepository.cs
│   │       ├── ProjectKnowledgeRepository.cs
│   │       ├── ProjectRepository.cs
│   │       ├── QuarterRepository.cs
│   │       ├── SentimentAnalysisCacheRepository.cs
│   │       ├── SkillAssessmentRepository.cs
│   │       ├── SkillCategoryRepository.cs
│   │       ├── SkillRepository.cs
│   │       ├── SprintCapacityRepository.cs
│   │       ├── SprintGoalRepository.cs
│   │       ├── SprintRepository.cs
│   │       ├── TeamTaskRepository.cs
│   │       └── Sqlite/                          — EF Core repository implementations (same interfaces)
│   │           ├── SqliteActivityRepository.cs
│   │           ├── SqliteAllocationRepository.cs
│   │           ├── SqliteAppSettingsRepository.cs
│   │           ├── SqliteChecklistInstanceItemRepository.cs
│   │           ├── SqliteChecklistInstanceRepository.cs
│   │           ├── SqliteChecklistTemplateItemRepository.cs
│   │           ├── SqliteChecklistTemplateRepository.cs
│   │           ├── SqliteDirectReportRepository.cs
│   │           ├── SqliteDocumentRepository.cs
│   │           ├── SqliteInitiativeDependencyRepository.cs
│   │           ├── SqliteInitiativeMemberRepository.cs
│   │           ├── SqliteInitiativeRepository.cs
│   │           ├── SqliteKnowledgePointRepository.cs
│   │           ├── SqliteLeaveRepository.cs
│   │           ├── SqliteManagerNoteRepository.cs
│   │           ├── SqliteNoteFolderRepository.cs
│   │           ├── SqliteOneOnOneMeetingRepository.cs
│   │           ├── SqliteParentRepository.cs
│   │           ├── SqlitePerformanceReviewRepository.cs
│   │           ├── SqliteProjectKnowledgeRepository.cs
│   │           ├── SqliteProjectRepository.cs
│   │           ├── SqliteQuarterRepository.cs
│   │           ├── SqliteSentimentAnalysisCacheRepository.cs
│   │           ├── SqliteSkillAssessmentRepository.cs
│   │           ├── SqliteSkillCategoryRepository.cs
│   │           ├── SqliteSkillRepository.cs
│   │           ├── SqliteSprintCapacityRepository.cs
│   │           ├── SqliteSprintGoalRepository.cs
│   │           ├── SqliteSprintRepository.cs
│   │           └── SqliteTeamTaskRepository.cs
│   └── Services/
│       └── ClaudeApiService.cs                  — IClaudeApiService via HttpClient; key read from AppSettings
│
├── Hive.Api/                                    — Thin HTTP layer
│   ├── Program.cs                               — Startup, DI wiring, database mode selection, CORS, Swagger, /health
│   ├── appsettings.json                         — Credentials, logging, database mode
│   ├── appsettings.Development.json             — Development overrides
│   ├── Authentication/
│   │   └── BasicAuthenticationHandler.cs        — HTTP Basic Auth against AdminCredentials
│   └── Controllers/
│       ├── ActivityFeedController.cs            — Activity feed and audit trail
│       ├── BackupController.cs                  — Export and restore
│       ├── ChecklistInstancesController.cs      — Running checklists
│       ├── ChecklistTemplatesController.cs      — Checklist templates
│       ├── DirectReportsController.cs           — Team members
│       ├── DocumentsController.cs               — Documents
│       ├── JiraImportController.cs              — CSV preview and import
│       ├── KnowledgePointsController.cs         — Points and level suggestions
│       ├── LeavesController.cs                  — Leave records
│       ├── ManagerNotesController.cs            — Manager notes and TODOs
│       ├── NoteFoldersController.cs             — Note folders
│       ├── OneOnOneMeetingsController.cs        — 1:1 meetings
│       ├── ParentsController.cs                 — Parent work items
│       ├── PerformanceReviewsController.cs      — Performance reviews
│       ├── ProjectKnowledgeController.cs        — Project knowledge levels
│       ├── ProjectsController.cs                — Projects
│       ├── QuarterlyPlanningController.cs       — Quarters, initiatives, allocations, insights
│       ├── ReportsController.cs                 — Dashboard and analytics endpoints
│       ├── SentimentAnalysisController.cs       — Sentiment analysis and key validation
│       ├── SettingsController.cs                — Runtime application settings
│       ├── SkillAssessmentsController.cs        — Skill assessments
│       ├── SkillCategoriesController.cs         — Skill categories
│       ├── SkillsController.cs                  — Skill catalog
│       ├── SprintCapacityController.cs          — Sprint capacity
│       ├── SprintsController.cs                 — Sprints
│       └── TeamTasksController.cs               — Tasks
│
└── Hive.Desktop/                                — Electron 28 + React 18 + TypeScript + Vite + Tailwind
    ├── vite.config.ts                            — Vite build and dev server config
    ├── vitest.config.ts                          — Vitest config and coverage thresholds
    ├── electron/
    │   ├── backend.ts                           — Spawns and supervises the bundled .NET backend
    │   ├── main.ts                              — Electron main process and window lifecycle
    │   └── preload.ts                           — Context-isolated renderer bridge
    ├── backend/                                 — Bundled backend binaries (build output, gitignored)
    └── src/
        ├── App.tsx                              — Route table
        ├── main.tsx                             — React entry point, mounts App
        ├── global.d.ts                          — Ambient types for the preload bridge
        ├── assets/                              — Application icons (icns, ico, png, svg)
        ├── components/
        │   ├── Card.tsx                         — Generic card + CardHeader used by dashboard widgets
        │   ├── InitiativesPanel.tsx             — Quarterly initiatives panel
        │   ├── InsightsSidebar.tsx              — Planning insights sidebar
        │   ├── KnowledgeProgressionChart.tsx    — Knowledge level and points over time
        │   ├── Layout.tsx                       — App shell and navigation
        │   ├── LoadingScreen.tsx                — Backend startup state
        │   ├── MarkdownEditor.tsx               — Shared writing surface: toolbar, shortcuts, preview
        │   ├── MarkdownPreview.tsx              — Renders note markdown (GFM: tables, task lists)
        │   ├── MeetingEditor.tsx                — 1:1 editor: tags, resolved person chip, date
        │   ├── NoteEditor.tsx                   — Note writing surface: markdown shortcuts, preview, to-do inspector
        │   ├── NoteFolderTree.tsx               — Folder sidebar with nesting, rename and drop targets
        │   ├── PlanningMatrix.tsx               — Initiative × sprint allocation matrix
        │   ├── PlanningSpreadsheet.tsx          — Spreadsheet-style planning editor
        │   ├── SentimentInsights.tsx            — Sentiment analysis display
        │   ├── SkillRadarChart.tsx              — Per-person skill radar
        │   └── SkillsHeatmap.tsx                — Team-wide skill heatmap
        ├── contexts/
        │   ├── ThemeContext.tsx                 — Dark/light theme state
        │   └── ToastContext.tsx                 — Transient notification state
        ├── hooks/
        │   └── useEscapeKey.ts                  — Escape-to-close for modals
        ├── pages/
        │   ├── ActivityFeed.tsx                 — Audit trail
        │   ├── Calendar.tsx                     — Leave and meeting calendar
        │   ├── Checklists.tsx                   — Interview and onboarding checklists
        │   ├── Dashboard.tsx                    — Main dashboard; defines ALL_TIME_BADGE
        │   ├── DirectReports.tsx                — Team members
        │   ├── Documents.tsx                    — Documents
        │   ├── Leaves.tsx                       — Leave records
        │   ├── Logs.tsx                         — Client-side log viewer
        │   ├── Meetings.tsx                     — 1:1s: people rail, 1:1 list, markdown editor
        │   ├── Notes.tsx                        — Notes: folder sidebar, note list, autosaving markdown editor
        │   ├── Parents.tsx                      — Parent work items
        │   ├── ProjectKnowledge.tsx             — Knowledge levels and suggestions
        │   ├── Projects.tsx                     — Projects
        │   ├── QuarterlyPlanning.tsx            — Quarters, initiatives, allocations
        │   ├── Reviews.tsx                      — Performance reviews
        │   ├── Settings.tsx                     — Runtime settings, Claude key, Jira import
        │   ├── Skills.tsx                       — Skill catalog and heatmap
        │   ├── Sprints.tsx                      — Sprints and capacity
        │   ├── Tasks.tsx                        — Task board
        │   └── Tutorials.tsx                    — In-app documentation
        ├── services/
        │   ├── api.ts                           — The single Axios client; all API calls live here
        │   └── logStore.ts                      — In-renderer log buffer for the Logs page
        ├── test/
        │   ├── factories.ts                     — Test data builders
        │   ├── setup.ts                         — Vitest global setup
        │   ├── test-utils.tsx                   — Render helpers with providers
        │   └── mocks/
        │       ├── handlers.ts                  — MSW request handlers
        │       └── server.ts                    — MSW server lifecycle
        ├── types/
        │   ├── index.ts                         — Shared API types
        │   └── quarterlyPlanning.ts             — Planning-specific types
        └── utils/
            └── dateUtils.ts                     — Date formatting and range helpers

tests/
└── Hive.Tests/
    ├── Core/Entities/                           — Domain rules; no mocks, no database
    │   └── {Activity,Allocation,AppSettings,ChecklistInstance,ChecklistInstanceItem,
    │       ChecklistTemplate,ChecklistTemplateItem,DirectReport,Document,Initiative,
    │       InitiativeDependency,InitiativeMember,KnowledgePoint,Leave,ManagerNote,
    │       NoteFolder,NoteText,OneOnOneMeeting,Parent,PerformanceReview,Project,ProjectKnowledge,
    │       Quarter,SentimentAnalysisCache,Skill,SkillAssessment,SkillCategoryEntity,
    │       Sprint,SprintCapacity,SprintGoal,TeamTask}Tests.cs
    ├── Core/Exceptions/
    │   └── ExceptionTests.cs                    — DomainException hierarchy behaviour
    ├── Application/Services/                    — Service logic with mocked repositories (Moq)
    │   └── {Activity,AppSettings,Backup,Checklist,DirectReport,Document,JiraImport,
    │       KnowledgePoint,Leave,ManagerNote,NoteFolder,OneOnOneMeeting,Parent,
    │       PerformanceReview,ProjectKnowledge,Project,QuarterlyPlanning,Reporting,
    │       SentimentAnalysis,SkillAssessment,Skill,SprintCapacity,Sprint,TeamTask}ServiceTests.cs
    ├── Api/Authentication/
    │   └── BasicAuthenticationHandlerTests.cs   — Credential parsing and rejection
    ├── Api/Controllers/                         — Controller behaviour with mocked services
    │   └── {ActivityFeed,Backup,ChecklistInstances,ChecklistTemplates,DirectReports,
    │       Documents,JiraImport,Leaves,ManagerNotes,OneOnOneMeetings,
    │       NoteFolders,Parents,PerformanceReviews,Projects,QuarterlyPlanning,Reports,Settings,
    │       SkillAssessments,SprintCapacity,Sprints,TeamTasks}ControllerTests.cs
    ├── Infrastructure/Persistence/
    │   └── DatabaseBackupServiceTests.cs        — File-level backup behaviour
    ├── Infrastructure/Repositories/             — Repository implementations
    │   └── {Activity,Allocation,AppSettings,ChecklistInstance,ChecklistTemplate,
    │       DirectReport,Document,Initiative,KnowledgePoint,Leave,ManagerNote,
    │       NoteFolder,OneOnOneMeeting,Parent,PerformanceReview,Project,Quarter,SentimentAnalysisCache,
    │       SkillAssessment,SkillCategory,Skill,SprintCapacity,SprintGoal,Sprint,
    │       TeamTask}RepositoryTests.cs
    └── Integration/                             — Real services, full DI container, in-memory database
        ├── IntegrationTestBase.cs               — Base class building the DI container
        ├── JiraImportServiceIntegrationTests.cs — End-to-end CSV import
        ├── NotesIntegrationTests.cs             — End-to-end note writing, folders and pinning
        └── OneOnOneIntegrationTests.cs          — End-to-end 1:1 writing and tag linking

scripts/
├── build-backend.sh                             — Cross-platform .NET publish (osx-arm64, osx-x64, win-x64, linux-x64, all)
├── build-backend.bat                            — Windows batch equivalent
├── debug-prod-app.sh                            — Attach to a packaged build
├── debug-setup.sh                               — Prepare local debugging
└── generate-icons.sh                            — Generate platform icon assets

Root
├── ARCHITECTURE.md                              — Technical reference
├── BUSINESS.md                                  — Domain concepts and rules
├── CODEBASE.md                                  — This file
├── CLAUDE.md                                    — Working agreement for AI agents
├── README.md                                    — Entry point and getting started
├── docs/DEBUGGING.md                            — IDE and browser debugging setups
├── Makefile                                     — Primary developer entry point
├── Hive.sln                                     — Solution file
├── global.json                                  — Pinned .NET SDK version
└── coverage.runsettings                         — Coverage collection and exclusions
```
