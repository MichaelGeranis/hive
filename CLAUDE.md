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
npm run electron:build               # Build for production
```

## Architecture

The backend follows **Clean Architecture** with four layers:

```
src/
├── Hive.Core/           # Domain Layer - Entities, interfaces, exceptions
├── Hive.Application/    # Application Layer - Services, DTOs, use cases
├── Hive.Infrastructure/ # Infrastructure Layer - Repositories, in-memory DB
├── Hive.Api/            # Presentation Layer - Controllers, authentication
└── Hive.Desktop/        # Electron + React desktop application
```

### Key Architectural Patterns
- **Repository Pattern**: All data access goes through repository interfaces defined in Core, implemented in Infrastructure
- **Dependency Injection**: Services and repositories are registered in `Hive.Api/Program.cs`
- **DTOs**: Application layer uses DTOs to transfer data between layers (never expose entities directly)

### Backend Technology Stack
- .NET 8.0, ASP.NET Core Web API
- xUnit + Moq + FluentAssertions for testing
- In-memory database (ready for SQL Server/PostgreSQL migration)
- Basic Authentication (credentials in appsettings.json: admin/admin123)

### Frontend Technology Stack
- React 18 + TypeScript + Vite
- Electron 28 for desktop
- Tailwind CSS for styling
- Axios for API calls (base URL: http://localhost:5000/api)

## Domain Entities

- **DirectReport** - Team members/employees
- **PerformanceReview** - Annual reviews with ratings (NotRated to Outstanding)
- **OneOnOneMeeting** - 1:1 meetings with MeetingNotes
- **Skill/SkillAssessment** - Skills and their assessments per direct report
- **Project** - Work projects (Planning, Active, OnHold, Completed, Cancelled)
- **TeamTask** - Individual tasks with assignments

## Test Structure

```
tests/Hive.Tests/
├── Api/Controllers/          # Controller/endpoint tests
├── Application/Services/     # Service layer tests
├── Core/Entities/            # Domain entity tests
├── Core/Exceptions/          # Exception handling tests
└── Infrastructure/Repositories/  # Repository tests
```

## Development Notes

- Backend must be running before starting the frontend
- API documentation available at http://localhost:5000 (Swagger UI)
- Frontend dev server runs on port 5173, proxies API calls to 5000
- TypeScript uses path alias `@/*` mapping to `src/*`
