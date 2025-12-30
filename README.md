# Hive

Engineering Manager tool for assisting in managing projects, people, delivery and technical tasks.

## Table of Contents

- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
  - [Backend Installation](#backend-installation)
  - [Frontend Installation](#frontend-installation)
- [Running the Application](#running-the-application)
  - [Running the Backend](#running-the-backend)
  - [Running the Frontend](#running-the-frontend)
- [Testing](#testing)
  - [Backend Tests](#backend-tests)
  - [Frontend Tests](#frontend-tests)
- [Debugging](#debugging)
  - [Backend Debugging](#backend-debugging)
  - [Frontend Debugging](#frontend-debugging)
- [API Documentation](#api-documentation)
- [Authentication](#authentication)
- [Project Structure](#project-structure)
- [Configuration](#configuration)

## Architecture

This application follows **Clean Architecture** principles with the following layers:

```
src/
├── Hive.Core/           # Domain Layer (Entities, Interfaces, Business Rules)
├── Hive.Application/    # Application Layer (Use Cases, Services, DTOs)
├── Hive.Infrastructure/ # Infrastructure Layer (Repositories, Database)
├── Hive.Api/            # Presentation Layer (API Controllers, Authentication)
└── Hive.Desktop/        # Desktop Application (Electron + React)
```

### SOLID Principles Applied

- **Single Responsibility**: Each class has one reason to change
- **Open/Closed**: Extensible via interfaces without modifying existing code
- **Liskov Substitution**: Repository implementations are interchangeable
- **Interface Segregation**: Small, focused interfaces (IDirectReportRepository)
- **Dependency Inversion**: High-level modules depend on abstractions

## Prerequisites

### Backend
- .NET 8.0 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))

### Frontend
- Node.js 18+ ([Download](https://nodejs.org/))
- npm 9+ (comes with Node.js)

## Installation

### Backend Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd hive
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore Hive.sln
   ```

3. **Build the solution**
   ```bash
   dotnet build Hive.sln
   ```

### Frontend Installation

1. **Navigate to the Desktop project**
   ```bash
   cd src/Hive.Desktop
   ```

2. **Install npm dependencies**
   ```bash
   npm install
   ```

## Running the Application

### Running the Backend

1. **Start the API server**
   ```bash
   cd src/Hive.Api
   dotnet run
   ```

   The API will start at:
   - HTTP: http://localhost:5000
   - HTTPS: https://localhost:5001

2. **Access Swagger UI**

   Navigate to http://localhost:5000 to access the interactive API documentation.

### Running the Frontend

> **Note**: The backend API must be running before starting the frontend.

1. **Development mode** (with hot reload)
   ```bash
   cd src/Hive.Desktop
   npm run electron:dev
   ```
   This starts both the Vite dev server and Electron concurrently.

2. **Build for production**
   ```bash
   cd src/Hive.Desktop
   npm run electron:build
   ```
   Production builds are output to the `release/` directory.

### Running Both Together

For development, run in two separate terminal windows:

**Terminal 1 - Backend:**
```bash
cd src/Hive.Api
dotnet run
```

**Terminal 2 - Frontend:**
```bash
cd src/Hive.Desktop
npm run electron:dev
```

## Testing

### Backend Tests

The backend uses **xUnit** as the testing framework with **Moq** for mocking and **FluentAssertions** for readable assertions.

1. **Run all tests**
   ```bash
   dotnet test Hive.sln
   ```

2. **Run tests with verbose output**
   ```bash
   dotnet test Hive.sln --verbosity normal
   ```

3. **Run tests with code coverage**
   ```bash
   dotnet test Hive.sln --collect:"XPlat Code Coverage"
   ```

4. **Run specific test project**
   ```bash
   dotnet test tests/Hive.Tests/Hive.Tests.csproj
   ```

5. **Run tests matching a filter**
   ```bash
   # Run only DirectReport tests
   dotnet test Hive.sln --filter "FullyQualifiedName~DirectReport"

   # Run only service tests
   dotnet test Hive.sln --filter "FullyQualifiedName~ServiceTests"
   ```

6. **Watch mode** (re-run tests on file changes)
   ```bash
   dotnet watch test --project tests/Hive.Tests/Hive.Tests.csproj
   ```

#### Test Structure

```
tests/Hive.Tests/
├── Api/
│   └── Controllers/          # Controller tests
├── Application/
│   └── Services/             # Service layer tests
├── Core/
│   ├── Entities/             # Domain entity tests
│   └── Exceptions/           # Exception tests
└── Infrastructure/
    └── Repositories/         # Repository tests
```

### Frontend Tests

Currently, the frontend does not have a test suite configured. To add tests:

1. **Install testing dependencies**
   ```bash
   cd src/Hive.Desktop
   npm install --save-dev vitest @testing-library/react @testing-library/jest-dom jsdom
   ```

2. **Add test script to package.json**
   ```json
   {
     "scripts": {
       "test": "vitest",
       "test:coverage": "vitest --coverage"
     }
   }
   ```

## Debugging

### Backend Debugging

#### Using Visual Studio Code

1. **Install the C# extension** (C# Dev Kit recommended)

2. **Create launch configuration** (`.vscode/launch.json`):
   ```json
   {
     "version": "0.2.0",
     "configurations": [
       {
         "name": ".NET Core Launch (API)",
         "type": "coreclr",
         "request": "launch",
         "preLaunchTask": "build",
         "program": "${workspaceFolder}/src/Hive.Api/bin/Debug/net8.0/Hive.Api.dll",
         "args": [],
         "cwd": "${workspaceFolder}/src/Hive.Api",
         "stopAtEntry": false,
         "env": {
           "ASPNETCORE_ENVIRONMENT": "Development"
         }
       },
       {
         "name": ".NET Core Attach",
         "type": "coreclr",
         "request": "attach"
       }
     ]
   }
   ```

3. **Create build task** (`.vscode/tasks.json`):
   ```json
   {
     "version": "2.0.0",
     "tasks": [
       {
         "label": "build",
         "command": "dotnet",
         "type": "process",
         "args": [
           "build",
           "${workspaceFolder}/Hive.sln",
           "/property:GenerateFullPaths=true",
           "/consoleloggerparameters:NoSummary"
         ],
         "problemMatcher": "$msCompile"
       }
     ]
   }
   ```

4. **Set breakpoints** and press `F5` to start debugging.

#### Using Visual Studio

1. Open `Hive.sln` in Visual Studio
2. Set `Hive.Api` as the startup project
3. Set breakpoints in your code
4. Press `F5` to start debugging

#### Using JetBrains Rider

1. Open the solution in Rider
2. Select the `Hive.Api` run configuration
3. Click the Debug button or press `Shift+F9`

#### Command-line Debugging

For quick debugging without an IDE:

```bash
# Enable detailed logging
cd src/Hive.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run --verbosity detailed
```

#### Debugging Tests

```bash
# Debug a specific test
dotnet test --filter "FullyQualifiedName~TestMethodName" --logger "console;verbosity=detailed"
```

### Frontend Debugging

#### Using Visual Studio Code

1. **Install the recommended extensions**:
   - JavaScript Debugger (built-in)
   - Electron Debug

2. **Create launch configuration** (`.vscode/launch.json`):
   ```json
   {
     "version": "0.2.0",
     "configurations": [
       {
         "name": "Debug Electron Main",
         "type": "node",
         "request": "launch",
         "cwd": "${workspaceFolder}/src/Hive.Desktop",
         "runtimeExecutable": "${workspaceFolder}/src/Hive.Desktop/node_modules/.bin/electron",
         "args": ["."],
         "env": {
           "NODE_ENV": "development"
         }
       },
       {
         "name": "Debug Electron Renderer",
         "type": "chrome",
         "request": "attach",
         "port": 9222,
         "webRoot": "${workspaceFolder}/src/Hive.Desktop/src"
       }
     ]
   }
   ```

#### Using Chrome DevTools

1. **Start the app in development mode**
   ```bash
   cd src/Hive.Desktop
   npm run electron:dev
   ```

2. **Open DevTools** in the Electron window:
   - Press `Ctrl+Shift+I` (Windows/Linux) or `Cmd+Option+I` (macOS)
   - Or use the menu: View > Toggle Developer Tools

3. **Debug React components**:
   - Use the Sources tab to set breakpoints
   - Use the React Developer Tools extension for component inspection

#### Debugging Network Requests

1. Open Chrome DevTools in the Electron window
2. Go to the **Network** tab
3. Monitor API calls to `http://localhost:5000/api/*`
4. Check request/response payloads and headers

#### Common Debugging Tips

- **API Connection Issues**: Ensure the backend is running on port 5000
- **CORS Errors**: Check that the API allows requests from localhost:5173
- **Authentication Errors**: Verify Basic Auth credentials in `src/services/api.ts`

## API Documentation

### Swagger UI

Navigate to http://localhost:5000 (or https://localhost:5001) to access Swagger UI.

### API Endpoints

| Resource | Method | Endpoint | Description |
|----------|--------|----------|-------------|
| Direct Reports | GET | /api/directreports | Get all direct reports |
| Direct Reports | GET | /api/directreports/{id} | Get direct report by ID |
| Direct Reports | POST | /api/directreports | Create new direct report |
| Direct Reports | PUT | /api/directreports/{id} | Update direct report |
| Direct Reports | DELETE | /api/directreports/{id} | Delete direct report |
| Reviews | GET | /api/performancereviews | Get all performance reviews |
| Reviews | POST | /api/performancereviews | Create performance review |
| Meetings | GET | /api/oneonoremeetings | Get all 1:1 meetings |
| Meetings | POST | /api/oneonoremeetings | Schedule 1:1 meeting |
| Projects | GET | /api/projects | Get all projects |
| Projects | POST | /api/projects | Create new project |
| Tasks | GET | /api/teamtasks | Get all tasks |
| Tasks | POST | /api/teamtasks | Create new task |
| Reports | GET | /api/reports/dashboard | Get dashboard overview |

## Authentication

The API uses Basic Authentication. Default credentials:

- **Username**: `admin`
- **Password**: `admin123`

### Authenticating in Swagger UI

1. Click "Authorize" button
2. Enter username and password
3. Click "Authorize"

### Using curl

```bash
# Encode credentials: admin:admin123 -> YWRtaW46YWRtaW4xMjM=
curl -X GET "http://localhost:5000/api/directreports" \
     -H "Authorization: Basic YWRtaW46YWRtaW4xMjM="
```

## Project Structure

### Backend

```
src/
├── Hive.Core/                    # Domain Layer
│   ├── Entities/                 # Domain entities
│   ├── Interfaces/               # Repository contracts
│   └── Exceptions/               # Domain exceptions
├── Hive.Application/             # Application Layer
│   ├── DTOs/                     # Data Transfer Objects
│   ├── Interfaces/               # Service contracts
│   └── Services/                 # Business logic
├── Hive.Infrastructure/          # Infrastructure Layer
│   └── Persistence/
│       ├── InMemoryDbContext.cs  # In-memory database
│       └── Repositories/         # Repository implementations
└── Hive.Api/                     # Presentation Layer
    ├── Controllers/              # REST API endpoints
    └── Authentication/           # Auth handlers
```

### Frontend

```
src/Hive.Desktop/
├── electron/
│   ├── main.ts                   # Electron main process
│   └── preload.ts                # Preload script
├── src/
│   ├── components/               # Reusable React components
│   ├── pages/                    # Page components
│   ├── services/                 # API client
│   └── types/                    # TypeScript types
├── package.json                  # npm dependencies
├── vite.config.ts                # Vite configuration
└── tailwind.config.js            # Tailwind CSS configuration
```

## Configuration

### Backend Configuration

Admin credentials can be configured in `src/Hive.Api/appsettings.json`:

```json
{
  "AdminCredentials": {
    "Username": "admin",
    "Password": "your-secure-password"
  }
}
```

### Frontend Configuration

API base URL is configured in `src/Hive.Desktop/src/services/api.ts`:

```typescript
const api = axios.create({
  baseURL: 'http://localhost:5000/api',
  // ...
})
```

## Future Enhancements
Top Feature Suggestions
High Impact (Core EM Responsibilities)
1. Career Development Plans 🎯
    Track promotion readiness and career progression paths
    Link skill gaps to development goals
    Integration with existing performance reviews and skill assessments
    Why: Critical for retention and addressing "what's next?" conversations in 1:1s

2. Team Goals & OKRs
    Quarterly/annual objective and key result tracking
    Team vs individual goals with progress tracking
    Why: Your current system has tasks and reviews but no structured goal framework

3. Hiring & Recruiting Pipeline
    Job requisitions, candidate tracking, interview scheduling
    Interview feedback collection and pipeline analytics
    Why: Hiring is typically 30-40% of an EM's time, currently not tracked

4. Compensation Management
    Salary bands, equity/RSU tracking, compensation review cycles
    Budget planning for raises and promotions
    Why: Sensitive but crucial EM responsibility, currently no visibility

5. Team Health & Engagement Surveys
    Pulse surveys, eNPS tracking, anonymous feedback
    Trend analysis to spot morale issues early
    Why: Proactive team wellness monitoring vs reactive 1:1s

Medium Impact (Operational Excellence)
6. Sprint/Iteration Management
    Link tasks to sprints, burndown charts, velocity trends
    Why: You have tasks but no sprint planning or velocity tracking

7. Task Dependencies
    Blocking/blocked relationships, critical path analysis
    Why: Complex projects need dependency visualization

8. Training & Certifications
    Track courses, certifications, learning budgets
    Recommendations based on skill gap analysis
    Why: Natural extension of your skill assessment system

9. On-call & Incident Management
    Rotation scheduling, incident tracking, post-mortems
    Why: Common for engineering teams, impacts work-life balance

10. Team Calendar & Availability
    Unified view of schedules, leave, meeting load analysis
    Why: Better visibility than just leave requests

Quick Wins
11. Notifications & Reminders
    Alerts for upcoming reviews, overdue action items, milestones
    Why: Low effort, high value for preventing missed deadlines

12. Enhanced Export & Reporting
    PDF generation for reviews, CSV exports, executive summaries
    Why: Needed for HR compliance and stakeholder updates
