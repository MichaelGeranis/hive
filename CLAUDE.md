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
│   │   └── Tasks.tsx            # Task management
│   ├── components/       # Reusable components
│   │   ├── Card.tsx             # Generic card component
│   │   ├── InitiativesPanel.tsx # Quarterly initiatives panel
│   │   ├── InsightsSidebar.tsx  # Planning insights sidebar
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
