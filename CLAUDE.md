# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Hive is an Engineering Manager tool for managing projects, people, delivery, and technical tasks. It's a full-stack application with a .NET 8 backend API and an Electron + React desktop frontend.

## Common Commands

### Backend
```bash
dotnet restore Hive.sln              # Restore packages
dotnet build Hive.sln                # Build solution
dotnet run --project src/Hive.Api    # Run API (ports 5000/5001)
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
- Axios for API calls (base URL: http://localhost:5000/api)
- React Router 6 for navigation
- Lucide React for icons
- Recharts for data visualization
- ThemeContext for dark/light mode support

## Domain Entities

### People Management
- **DirectReport** - Team members/employees with name, role, hire date
- **PerformanceReview** - Annual reviews with ratings (NotRated, NeedsImprovement, MeetsExpectations, ExceedsExpectations, Outstanding)
- **OneOnOneMeeting** - 1:1 meetings with agenda, date, duration
- **MeetingNote** - Notes associated with 1:1 meetings
- **Skill** - Skills tracked across the team
- **SkillAssessment** - Individual skill assessments per direct report

### Leave Management
- **Leave** - Leave/PTO requests with type (Vacation, Sick, Personal, Parental, Unpaid), status (Pending, Approved, Rejected, Cancelled)
- Tracks start/end dates, business days calculation, approvals
- Prevents modification of approved leaves

### Project & Task Management
- **Project** - Work projects with status (Planning, Active, OnHold, Completed, Cancelled)
- **TeamTask** - Individual tasks with priority, status, assignments

### Configuration
- **AppSettings** - Application settings including story point mappings
- Stored in database for runtime configuration

## API Controllers & Endpoints

All controllers use `/api` prefix and require Basic Authentication:

- **DirectReportsController** - CRUD operations for team members
- **PerformanceReviewsController** - Performance review management
- **OneOnOneMeetingsController** - 1:1 meeting scheduling and tracking
- **MeetingNotesController** - Notes for 1:1 meetings
- **SkillsController** - Skill catalog management
- **SkillAssessmentsController** - Individual skill assessments
- **LeavesController** - Leave request management (approve/reject/cancel)
- **ProjectsController** - Project management
- **TeamTasksController** - Task management
- **SettingsController** - Application settings (story points, etc.)
- **ReportsController** - Reporting and analytics endpoints

## Frontend Structure

```
src/Hive.Desktop/
├── electron/              # Electron main process
├── src/
│   ├── pages/            # Page components
│   │   ├── Dashboard.tsx
│   │   ├── DirectReports.tsx
│   │   ├── Leaves.tsx
│   │   ├── Meetings.tsx
│   │   ├── Projects.tsx
│   │   ├── Reviews.tsx
│   │   ├── Settings.tsx
│   │   └── Tasks.tsx
│   ├── components/       # Reusable components
│   │   ├── Card.tsx
│   │   ├── Layout.tsx
│   │   └── LoadingScreen.tsx
│   ├── contexts/         # React contexts
│   │   └── ThemeContext.tsx   # Dark/light theme management
│   ├── services/         # API service layer
│   │   └── api.ts        # Axios API client
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
├── Api/Controllers/          # Controller/endpoint tests
├── Application/Services/     # Service layer tests
├── Core/Entities/            # Domain entity tests
├── Core/Exceptions/          # Exception handling tests
└── Infrastructure/Repositories/  # Repository tests
```

### Testing Conventions
- Use xUnit for test framework
- Moq for mocking dependencies
- FluentAssertions for readable assertions
- Follow naming: `MethodName_Scenario_ExpectedBehavior`
- Use `[Fact]` for single test cases, `[Theory]` for parameterized tests

## Development Notes

### Getting Started
1. Backend must be running before starting the frontend
2. API documentation available at http://localhost:5000 (Swagger UI)
3. Frontend dev server runs on port 5173, proxies API calls to 5000
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
