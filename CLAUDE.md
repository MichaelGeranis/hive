# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Hive is an Engineering Manager tool for managing projects, people, delivery, and technical tasks. It's a full-stack application with a .NET 9 backend API and an Electron + React desktop frontend.

## Common Commands

### Using Make (Recommended)
```bash
make help                            # Show all available commands
make install                         # Install all dependencies (backend + frontend)
make dev                             # Run backend and frontend in development mode
make backend-sqlite                  # Run backend with SQLite (production mode)
make backend-inmemory                # Run backend with in-memory database (development)
make frontend                        # Run frontend only (port 5173)
make test                            # Run all tests (backend + frontend)
make test-coverage                   # Run tests with code coverage
make coverage-report                 # Generate HTML coverage report
make build                           # Build backend and frontend
make clean                           # Clean build artifacts
make kill-backend                    # Kill process using backend port 5002
```

### Database Migrations
```bash
make migration-add                   # Create a new migration (prompts for name)
make migration-remove                # Remove the last migration
make migration-update                # Apply migrations to database
make migration-list                  # List all migrations
```

### Backend (Direct)
```bash
dotnet restore Hive.sln              # Restore packages
dotnet build Hive.sln                # Build solution
dotnet run --project src/Hive.Api    # Run API (port 5002)
dotnet test Hive.sln                 # Run all tests
dotnet test --filter "FullyQualifiedName~DirectReport"  # Run filtered tests
dotnet watch test --project tests/Hive.Tests/Hive.Tests.csproj  # Watch mode
```

### Frontend
```bash
cd src/Hive.Desktop
npm install                          # Install dependencies
npm run electron:dev                 # Run dev mode (Vite + Electron)
npm run electron:build               # Build for production (all platforms)
npm run electron:build:mac           # Build for macOS (ARM64 + x64)
npm run electron:build:win           # Build for Windows (x64)
npm run build:backend:all            # Build backend for all platforms
```

### Build Scripts
The project includes platform-specific build scripts in `scripts/`:
- `build-backend.sh` - Cross-platform .NET backend builder (supports osx-arm64, osx-x64, win-x64, linux-x64)
- `build-backend.bat` - Windows batch script for backend builds
- `generate-icons.sh` - Generate platform-specific app icons

## Architecture

The backend follows **Clean Architecture** with four layers:

```
src/
├── Hive.Core/           # Domain Layer - Entities, interfaces, exceptions
├── Hive.Application/    # Application Layer - Services, DTOs, use cases
├── Hive.Infrastructure/ # Infrastructure Layer - Repositories, database
├── Hive.Api/            # Presentation Layer - Controllers, authentication
└── Hive.Desktop/        # Electron + React desktop application
```

### Key Architectural Patterns
- **Repository Pattern**: All data access goes through repository interfaces defined in Core, implemented in Infrastructure
- **Dependency Injection**: Services and repositories are registered in `Hive.Api/Program.cs`
- **DTOs**: Application layer uses DTOs to transfer data between layers (never expose entities directly)
- **Service Layer**: Business logic isolated in Application layer services
- **Domain-Driven Design**: Rich domain entities with validation and business rules

### Backend Technology Stack
- .NET 9.0, ASP.NET Core Web API
- xUnit + Moq + FluentAssertions for testing
- SQLite database (production) / In-memory database (development)
- Basic Authentication (credentials in appsettings.json: admin/admin123)
- Swagger/OpenAPI for API documentation
- CORS enabled for localhost:5173

### Frontend Technology Stack
- React 18 + TypeScript + Vite
- Electron 28 for desktop
- Tailwind CSS for styling
- Axios for API calls (base URL: http://localhost:5002/api)
- React Router 6 for navigation
- Lucide React for icons
- Recharts for data visualization
- ThemeContext for dark/light mode support

## Domain Entities

### People Management
- **DirectReport** - Team members/employees with name, role, hire date
- **Parent** - Parent organization/manager entity for hierarchical structure
- **PerformanceReview** - Annual reviews with ratings (NotRated, NeedsImprovement, MeetsExpectations, ExceedsExpectations, Outstanding)
- **OneOnOneMeeting** - 1:1 meetings with agenda, date, duration
- **MeetingNote** - Notes associated with 1:1 meetings
- **ManagerNote** - Manager's private notes about direct reports
- **Skill** - Skills tracked across the team
- **SkillCategory** - Categories for organizing skills
- **SkillAssessment** - Individual skill assessments per direct report

### Leave Management
- **Leave** - Leave/PTO requests with type (Vacation, Sick, Personal, Parental, Unpaid), status (Pending, Approved, Rejected, Cancelled)
- Tracks start/end dates, business days calculation, approvals
- Prevents modification of approved leaves

### Project & Task Management
- **Project** - Work projects with status (Planning, Active, OnHold, Completed, Cancelled)
- **ProjectKnowledge** - Knowledge base entries for projects
- **TeamTask** - Individual tasks with priority, status, assignments
- **Document** - Document management for teams/projects

### Sprint & Quarterly Planning
- **Sprint** - Sprint tracking with goals and capacity
- **SprintGoal** - Individual goals within a sprint
- **SprintCapacity** - Team member capacity per sprint
- **Quarter** - Quarterly planning periods
- **Initiative** - Quarterly initiatives with T-shirt size estimates
- **Allocation** - Resource allocation to initiatives
- **InitiativeDependency** - Dependencies between initiatives

### Checklists
- **ChecklistTemplate** - Reusable checklist templates
- **ChecklistTemplateItem** - Items within a checklist template
- **ChecklistInstance** - Instance of a checklist (e.g., for onboarding)
- **ChecklistInstanceItem** - Individual items in a checklist instance

### Activity & Analytics
- **Activity** - Activity feed entries for audit/tracking
- **SentimentAnalysisCache** - Cached sentiment analysis results

### Configuration
- **AppSettings** - Application settings including story point mappings, T-shirt size mappings
- Stored in database for runtime configuration

## API Controllers & Endpoints

All controllers use `/api` prefix and require Basic Authentication:

### People Management
- **DirectReportsController** - CRUD operations for team members
- **ParentsController** - Parent organization management
- **PerformanceReviewsController** - Performance review management
- **OneOnOneMeetingsController** - 1:1 meeting scheduling and tracking
- **MeetingNotesController** - Notes for 1:1 meetings
- **ManagerNotesController** - Manager's private notes
- **SkillsController** - Skill catalog management
- **SkillCategoriesController** - Skill category management
- **SkillAssessmentsController** - Individual skill assessments
- **LeavesController** - Leave request management (approve/reject/cancel)

### Project & Task Management
- **ProjectsController** - Project management
- **ProjectKnowledgeController** - Project knowledge base
- **TeamTasksController** - Task management
- **DocumentsController** - Document management

### Sprint & Planning
- **SprintsController** - Sprint management
- **SprintCapacityController** - Team capacity per sprint
- **QuarterlyPlanningController** - Quarterly planning and initiatives

### Checklists
- **ChecklistTemplatesController** - Checklist template management
- **ChecklistInstancesController** - Checklist instance management

### Analytics & Integration
- **ActivityFeedController** - Activity feed and audit trail
- **SentimentAnalysisController** - Sentiment analysis for notes/feedback
- **ReportsController** - Reporting and analytics endpoints
- **JiraImportController** - Import data from Jira

### System
- **SettingsController** - Application settings (story points, T-shirt sizes, etc.)
- **BackupController** - Database backup and restore

## Frontend Structure

```
src/Hive.Desktop/
├── electron/              # Electron main process
├── src/
│   ├── pages/            # Page components
│   │   ├── ActivityFeed.tsx     # Activity/audit trail
│   │   ├── Calendar.tsx         # Calendar view
│   │   ├── Checklists.tsx       # Checklist management
│   │   ├── Dashboard.tsx        # Main dashboard
│   │   ├── DirectReports.tsx    # Team member management
│   │   ├── Documents.tsx        # Document management
│   │   ├── Leaves.tsx           # Leave/PTO management
│   │   ├── Logs.tsx             # System logs
│   │   ├── Meetings.tsx         # 1:1 meeting management
│   │   ├── Notes.tsx            # Manager notes
│   │   ├── Parents.tsx          # Parent organization management
│   │   ├── ProjectKnowledge.tsx # Project knowledge base
│   │   ├── Projects.tsx         # Project management
│   │   ├── QuarterlyPlanning.tsx # Quarterly planning & initiatives
│   │   ├── Reviews.tsx          # Performance reviews
│   │   ├── Settings.tsx         # Application settings
│   │   ├── Skills.tsx           # Skills management & heatmap
│   │   ├── Sprints.tsx          # Sprint management
│   │   ├── Tasks.tsx            # Task management
│   │   └── Tutorials.tsx        # In-app tutorials & documentation
│   ├── components/       # Reusable components
│   │   ├── Card.tsx             # Generic card component
│   │   ├── InitiativesPanel.tsx # Quarterly initiatives panel
│   │   ├── InsightsSidebar.tsx  # Planning insights sidebar
│   │   ├── KnowledgeProgressionChart.tsx # Knowledge level & points progression chart
│   │   ├── Layout.tsx           # Main app layout
│   │   ├── LoadingScreen.tsx    # Loading state component
│   │   ├── PlanningMatrix.tsx   # Priority/effort matrix
│   │   ├── SentimentInsights.tsx # Sentiment analysis display
│   │   ├── SkillRadarChart.tsx  # Radar chart for skills
│   │   └── SkillsHeatmap.tsx    # Skills heatmap visualization
│   ├── contexts/         # React contexts
│   │   └── ThemeContext.tsx     # Dark/light theme management
│   ├── services/         # API service layer
│   │   └── api.ts               # Axios API client
│   ├── types/            # TypeScript type definitions
│   └── assets/           # Icons, images
└── backend/              # Embedded .NET backend (built output)
```

## Dashboard Sprint History Filter

The Dashboard page has a "Sprint History" dropdown filter that limits data to the last N sprints. Not all widgets are affected by this filter. Widgets showing "all time" data are marked with an `ALL_TIME_BADGE` pill in their header (defined in `Dashboard.tsx`).

### Not affected by sprint filter (show all-time data)

| Widget | Data Source | Why Unaffected |
|--------|-------------|----------------|
| Team Members (stat) | `dashboard.team.totalReports` | `GetTeamOverviewAsync()` takes no sprint param |
| Projects (stat) | `dashboard.tasks.projects.totalProjects` | Backend uses `projects.Count` (all projects) |
| 1:1 Action Items (stat) | `meetingNotesApi.getOpenActionItems()` | Independent API, no sprint param |
| TODOs (stat) | `notesApi.getPending()` | Independent API, no sprint param |
| Projects Distribution | Local computation from `tasksApi.getAll()` | `tasksApi.getAll()` fetches all tasks |
| Members by Project | Local computation from `tasksApi.getAll()` | Same as above |
| Knowledge Level Suggestions | `knowledgePointsApi.getSuggestions()` | Independent API, no sprint param |
| Team Sentiment | `sentimentApi` (own component) | Independent API, no sprint param |

### Affected by sprint filter

| Widget | Data Source |
|--------|-------------|
| Warnings (stat) | Mixed — depends on filtered capacity, velocity, accuracy |
| Sprint & Tasks Overview (bar) | `capacityAnalysis` + `dashboard.tasks.tasks` |
| Tasks Distribution | `dashboard.tasks.tasksByType` |
| Tasks Distribution (SP) | `dashboard.tasks.tasksByTypeSP` |
| Tasks Distribution (Hours) | `dashboard.tasks.tasksByTypeHours` |
| Members Workload | `dashboard.tasks.tasksByAssignee` |
| Capacity Analysis | `reportsApi.getCapacityAnalysis(sprintFilter)` |
| Sprint Capacity Suggestions | Uses `sprintFilter` to limit upcoming sprints shown |
| Team Velocity | `reportsApi.getTeamVelocity(sprintFilter)` |
| Estimation Accuracy | `reportsApi.getEstimationAccuracy(sprintFilter)` |
| Support Hours | `dashboard.tasks.supportDistribution` |
| Support Hours by Assignee | `dashboard.tasks.supportDistribution` |
| Components Distribution | `dashboard.tasks.tasksByComponent` |

### Key backend detail

In `ReportingService.GetTasksAnalyticsAsync(sprintCount)`:
- The `tasks` variable is filtered by sprint (lines 362-389)
- The `projects` variable is **not** filtered — `TotalProjects = projects.Count` uses all projects
- Support and maintenance distribution now use filtered `tasks` variable (respects sprint filter)
- The `allSprints`, `directReports`, `parents` variables are never sprint-filtered

### Adding new dashboard widgets

When adding a new widget to the Dashboard, determine whether its data source is affected by the sprint filter. If not, add `badge={ALL_TIME_BADGE}` to its `CardHeader` (or `badge` prop on `StatCard`).

## Database Configuration

The application supports two database modes:

### Development (In-Memory)
- Configured via `UseInMemoryDatabase: true` in appsettings.json
- Data is lost on restart
- Automatically seeds sample data
- Fast for testing

### Production (SQLite)
- Configured via `UseInMemoryDatabase: false`
- Persistent storage in platform-specific locations:
  - **macOS**: `~/Library/Application Support/Hive/hive.db`
  - **Windows**: `%APPDATA%/Hive/hive.db`
  - **Linux**: `~/.local/share/Hive/hive.db`
- Override with `HIVE_DATABASE_PATH` environment variable
- Automatically seeds initial data on first run

## Test Structure

```
tests/Hive.Tests/
├── Api/Controllers/              # Controller/endpoint tests
├── Application/Services/         # Service layer tests
├── Core/Entities/                # Domain entity tests
├── Core/Exceptions/              # Exception handling tests
├── Infrastructure/Persistence/   # Database/persistence tests
├── Infrastructure/Repositories/  # Repository tests
└── Integration/                  # Integration tests with real services
    ├── IntegrationTestBase.cs    # Base class for integration tests
    └── *IntegrationTests.cs      # Integration test classes
```

### Testing Conventions
- Use xUnit for test framework
- Moq for mocking dependencies (unit tests)
- FluentAssertions for readable assertions
- Follow naming: `MethodName_Scenario_ExpectedBehavior`
- Use `[Fact]` for single test cases, `[Theory]` for parameterized tests

### Integration Tests
- Use `IntegrationTestBase` class for tests that need real services
- Integration tests use in-memory database with full DI container
- Test cross-service interactions without mocking

### Code Coverage
- Run `make test-coverage` to generate coverage report
- Run `make coverage-report` to generate and open HTML report
- Coverage excludes:
  - Test files (`Hive.Tests` assembly)
  - Database migrations (`**/Migrations/**`)
  - Interfaces (`**/Interfaces/**`)
- Configuration in `tests/Hive.Tests/Hive.Tests.csproj` and `coverage.runsettings`

## Development Notes

### Getting Started
1. Backend must be running before starting the frontend
2. API documentation available at http://localhost:5002 (Swagger UI)
3. Frontend dev server runs on port 5173, proxies API calls to 5002
4. TypeScript uses path alias `@/*` mapping to `src/*`

### Desktop Application
- The desktop app bundles the .NET backend inside the Electron app
- Backend API runs as a subprocess managed by Electron main process
- Platform-specific backends are built and bundled in `backend/` directory
- Release builds are output to `src/Hive.Desktop/release/`

### Code Style & Conventions
- Backend: Use C# naming conventions (PascalCase for public members)
- Frontend: Use TypeScript with strict mode enabled
- Components: Functional components with hooks
- State management: React Context for global state (theme, auth)
- API calls: Centralized in `services/api.ts`

### Authentication
- Basic Authentication required for all API endpoints
- Default credentials: `admin/admin123` (configured in appsettings.json)
- Frontend stores credentials for API calls

### Environment Variables
- `HIVE_DATABASE_PATH` - Override default SQLite database location
- `UseInMemoryDatabase` - Set to `true` for in-memory database (appsettings.json)

## Common Development Tasks

### Adding a New Entity
1. Create entity in `src/Hive.Core/Entities/`
2. Add repository interface in `src/Hive.Core/Interfaces/`
3. Implement repository in `src/Hive.Infrastructure/Persistence/`
4. Create DTOs in `src/Hive.Application/DTOs/`
5. Create service in `src/Hive.Application/Services/`
6. Add service interface in `src/Hive.Application/Interfaces/`
7. Create controller in `src/Hive.Api/Controllers/`
8. Register services in `DependencyInjection.cs` files
9. Add tests for entity, repository, service, and controller

### Adding a New Page
1. Create page component in `src/Hive.Desktop/src/pages/`
2. Add route in `App.tsx`
3. Create types in `src/types/` if needed
4. Add API calls to `services/api.ts`
5. Update Layout navigation if needed

## Figma MCP Integration Rules

These rules define how to translate Figma inputs into code for this project and must be followed for every Figma-driven change.

### Required Flow (do not skip)

1. Run `get_design_context` first to fetch the structured representation for the exact node(s)
2. If the response is too large or truncated, run `get_metadata` to get the high-level node map, then re-fetch only the required node(s)
3. Run `get_screenshot` for a visual reference of the node variant being implemented
4. Only after you have both `get_design_context` and `get_screenshot`, download any assets needed and start implementation
5. Translate the output into this project's conventions (see rules below)
6. Validate against the Figma screenshot for 1:1 visual parity before marking complete

### Component Organization

- Reusable UI components go in `src/Hive.Desktop/src/components/` (flat structure, named exports)
- Page-level components go in `src/Hive.Desktop/src/pages/` (default exports)
- IMPORTANT: Always check `src/Hive.Desktop/src/components/` for existing primitives before creating new ones
- New shared components must use **named exports** (e.g., `export function MyComponent`)
- New pages must use **default exports** (e.g., `export default function MyPage`)

### Card Primitives (use these first)

The `Card` component family in `src/Hive.Desktop/src/components/Card.tsx` covers the majority of UI containers:

- `<Card className="">` — white/slate-800 container with rounded-xl, shadow-sm, slate border
- `<CardHeader title="" subtitle="" badge={} action={}>` — section header with optional badge and action slot
- `<CardContent className="">` — padded content area (px-6 py-4)
- `<StatCard title="" value="" icon={} color="blue|green|amber|red|purple|slate" badge={} trend={} onClick={}>` — metric card with icon, trend indicator, and click handler

IMPORTANT: Use `StatCard` for all metric/KPI displays. Do not build custom stat cards.

### Styling Rules

- IMPORTANT: Use **Tailwind CSS utility classes** for all styling — no inline styles, no CSS modules
- Dark mode is `darkMode: 'class'` — always include `dark:` variants for every color/background/border class
- IMPORTANT: Never hardcode hex colors. Use Tailwind semantic classes or the CSS variables defined in `src/Hive.Desktop/src/index.css`

**Color palette patterns:**

| Use case | Light class | Dark class |
|---|---|---|
| Page background | `bg-slate-50` | `dark:bg-slate-900` |
| Card/surface background | `bg-white` | `dark:bg-slate-800` |
| Sidebar/nav background | `bg-slate-900` | `dark:bg-slate-950` |
| Primary border | `border-slate-200` | `dark:border-slate-700` |
| Secondary border | `border-slate-100` | `dark:border-slate-700` |
| Primary text | `text-slate-900` | `dark:text-slate-100` |
| Secondary text | `text-slate-500` | `dark:text-slate-400` |
| Brand accent (active/CTA) | `bg-amber-500` | same |
| Brand accent text | `text-amber-400` | same |

**Custom Tailwind colors** (defined in `tailwind.config.js`):
- `hive-*` scale (50–950): amber/yellow brand palette
- `primary-*` scale (50–950): orange brand palette

### Icon Rules

- IMPORTANT: Use **Lucide React** exclusively for all icons — do not install new icon packages
- Standard icon size: `w-5 h-5` (navigation, buttons); small: `w-4 h-4` (inline); large: `w-8 h-8` (hero/logo)
- Icons inside colored pill/badge backgrounds use the `StatCard` `color` prop pattern: `p-3 rounded-lg bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400`
- Import icons from `lucide-react` directly: `import { Plus, Trash2, Edit } from 'lucide-react'`

### Typography Scale

- Page heading: `text-2xl font-bold text-slate-900 dark:text-slate-100`
- Card/section heading: `text-lg font-semibold text-slate-900 dark:text-slate-100`
- Label/small text: `text-sm font-medium text-slate-500 dark:text-slate-400`
- Metric value: `text-2xl font-bold text-slate-900 dark:text-slate-100`
- Body: `text-sm text-slate-700 dark:text-slate-300`

### Spacing & Layout

- Page content padding: `p-6` (applied by `<Layout>` — do not add outer padding to page components)
- Card internal padding: `px-6 py-4` (handled by `<CardContent>`)
- Standard gap between cards in a grid: `gap-6`
- Standard stat card grid: `grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6`

### Modal / Form Overlay Pattern

All create/edit modals follow this pattern:
1. Controlled by `showForm` state (`useState(false)`)
2. Closed on Escape via `useEscapeKey(closeCallback, isOpen)` from `src/Hive.Desktop/src/hooks/useEscapeKey`
3. Overlay: `fixed inset-0 bg-black/50 flex items-center justify-center z-50`
4. Panel: `bg-white dark:bg-slate-800 rounded-xl p-6 w-full max-w-md`

### API Integration

- IMPORTANT: All API calls go through `src/Hive.Desktop/src/services/api.ts` — never call the backend directly from components
- API base URL is `http://localhost:5002/api` with Basic Auth (`admin/admin123`)
- Each entity has its own API object (e.g., `projectsApi`, `tasksApi`, `directReportsApi`)
- Add new endpoints to the relevant entity section in `api.ts`
- Types for all API responses are in `src/Hive.Desktop/src/types/`

### Toast Notifications

- Use `useToast()` from `src/Hive.Desktop/src/contexts/ToastContext` for user feedback
- `showError(message)` for failures, `showSuccess(message)` for confirmations
- Wrap error messages with `getErrorMessage(err)` helper for consistent error extraction

### Data Fetching Pattern

```tsx
const [data, setData] = useState<EntityType[]>([])
const [loading, setLoading] = useState(true)
const { showError } = useToast()

const loadData = async () => {
  try {
    setLoading(true)
    const result = await entityApi.getAll()
    setData(result)
  } catch (err) {
    showError(getErrorMessage(err))
  } finally {
    setLoading(false)
  }
}

useEffect(() => { loadData() }, [])
```

### Asset Handling

- IMPORTANT: If the Figma MCP server returns a localhost source for an image or SVG asset, use that source directly
- IMPORTANT: Do NOT install new icon packages — use Lucide React
- IMPORTANT: Do NOT use placeholder images if a real asset source is provided
- Store downloaded static assets in `src/Hive.Desktop/src/assets/`

### Electron / Desktop Considerations

- The app runs in Electron — no browser-specific APIs that require a server environment
- macOS traffic light buttons occupy the top-left area; use `titlebar-drag` / `titlebar-no-drag` CSS classes on elements in the title bar region (defined in `index.css`)
- Do not use `window.location` for navigation — use React Router's `useNavigate()` hook

### Charts

- Use **Recharts** for all data visualizations (already installed)
- Do not install D3, Chart.js, or other charting libraries
- Wrap charts in a `<Card>` with `<CardHeader>` for consistent styling
