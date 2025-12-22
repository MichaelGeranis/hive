# Hive

Engineering Manager tool for assisting in managing projects, people, delivery and technical tasks.

## Architecture

This application follows **Clean Architecture** principles with the following layers:

```
src/
├── Hive.Core/           # Domain Layer (Entities, Interfaces, Business Rules)
├── Hive.Application/    # Application Layer (Use Cases, Services, DTOs)
├── Hive.Infrastructure/ # Infrastructure Layer (Repositories, Database)
└── Hive.Api/            # Presentation Layer (API Controllers, Authentication)
```

### SOLID Principles Applied

- **Single Responsibility**: Each class has one reason to change
- **Open/Closed**: Extensible via interfaces without modifying existing code
- **Liskov Substitution**: Repository implementations are interchangeable
- **Interface Segregation**: Small, focused interfaces (IDirectReportRepository)
- **Dependency Inversion**: High-level modules depend on abstractions

## Prerequisites

- .NET 8.0 SDK

## Getting Started

### Build the Solution

```bash
dotnet build Hive.sln
```

### Run the API

```bash
cd src/Hive.Api
dotnet run
```

The API will start at:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001

### Swagger UI

Navigate to http://localhost:5000 (or https://localhost:5001) to access Swagger UI.

## Authentication

The API uses Basic Authentication. Default credentials:

- **Username**: `admin`
- **Password**: `admin123`

To authenticate in Swagger UI:
1. Click "Authorize" button
2. Enter username and password
3. Click "Authorize"

### Using curl

```bash
# Encode credentials: admin:admin123 -> YWRtaW46YWRtaW4xMjM=
curl -X GET "http://localhost:5000/api/directreports" \
     -H "Authorization: Basic YWRtaW46YWRtaW4xMjM="
```

## API Endpoints

### Direct Reports

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/directreports | Get all direct reports |
| GET | /api/directreports/{id} | Get direct report by ID |
| POST | /api/directreports | Create new direct report |
| PUT | /api/directreports/{id} | Update direct report |
| DELETE | /api/directreports/{id} | Delete direct report |

### Example Requests

#### Create a Direct Report

```bash
curl -X POST "http://localhost:5000/api/directreports" \
     -H "Authorization: Basic YWRtaW46YWRtaW4xMjM=" \
     -H "Content-Type: application/json" \
     -d '{
       "firstName": "John",
       "lastName": "Doe",
       "email": "john.doe@company.com",
       "jobTitle": "Senior Engineer",
       "department": "Engineering",
       "hireDate": "2024-01-15"
     }'
```

#### Get All Direct Reports

```bash
curl -X GET "http://localhost:5000/api/directreports" \
     -H "Authorization: Basic YWRtaW46YWRtaW4xMjM="
```

## Project Structure

### Hive.Core (Domain Layer)
- `Entities/DirectReport.cs` - Domain entity with business rules
- `Interfaces/IDirectReportRepository.cs` - Repository contract
- `Exceptions/` - Domain-specific exceptions

### Hive.Application (Application Layer)
- `DTOs/` - Data Transfer Objects
- `Interfaces/IDirectReportService.cs` - Service contract
- `Services/DirectReportService.cs` - Use case implementation

### Hive.Infrastructure (Infrastructure Layer)
- `Persistence/InMemoryDbContext.cs` - In-memory database
- `Persistence/Repositories/` - Repository implementations

### Hive.Api (Presentation Layer)
- `Controllers/DirectReportsController.cs` - REST API endpoints
- `Authentication/BasicAuthenticationHandler.cs` - Auth handler

## Configuration

Admin credentials can be configured in `appsettings.json`:

```json
{
  "AdminCredentials": {
    "Username": "admin",
    "Password": "your-secure-password"
  }
}
```

## Future Enhancements

- Replace in-memory database with SQL Server/PostgreSQL
- Add OAuth2/OIDC authentication
- Implement additional use cases (Projects, Tasks, Delivery tracking)
- Add unit and integration tests
