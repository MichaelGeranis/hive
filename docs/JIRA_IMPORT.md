# Jira Import Feature

## Overview

The Jira Import feature allows you to import tasks from Jira CSV exports directly into Hive. This is useful for migrating from Jira or keeping Hive synchronized with your Jira backlog.

## Features

- **CSV Upload**: Upload Jira CSV exports directly through the web interface
- **Preview Mode**: Preview the import before committing to see what will be imported
- **Field Mapping**: Automatic mapping of Jira fields to Hive task fields
- **Duplicate Detection**: Detect and update existing tasks based on Jira Issue Key or Title
- **Validation**: Validates CSV data and shows detailed error messages
- **Import Results**: Detailed results showing success, errors, and warnings

## How to Use

### 1. Export from Jira

1. Navigate to your Jira project
2. Go to Issues → Search for Issues (or use "All work" tab)
3. Click the "..." menu and select "Export"
4. Choose "Export CSV (all fields)" or "Export CSV (current fields)"
5. Save the CSV file

### 2. Import to Hive

1. In Hive, navigate to **Import from Jira** in the sidebar
2. Click **Select Jira CSV Export** and choose your downloaded CSV file
3. Click **Preview Import** to see what will be imported
4. Review the preview:
   - Check the number of valid/invalid rows
   - Review detected columns and mapping warnings
   - Examine sample rows to verify data
5. Configure import options:
   - **Update Existing**: Check to update existing tasks, uncheck to skip them
   - **Match Field**: Choose to match by Issue Key (recommended) or Title
6. Click **Import X Tasks** to start the import
7. Review the results showing:
   - Number of tasks imported, skipped, or failed
   - Detailed error messages for any failures
   - Warnings for any issues

## Field Mapping

The importer automatically maps Jira fields to Hive task fields:

| Jira Field | Hive Field | Notes |
|------------|------------|-------|
| Issue Key | Tags | Stored as `jira:ISSUE-123` for duplicate detection |
| Summary | Title | Required field |
| Description | Description | Optional |
| Issue Type | Type | Mapped to Task/Bug/Feature/Improvement/Research/Documentation |
| Status | Status | Mapped to Backlog/Todo/InProgress/InReview/Done/Cancelled |
| Priority | Priority | Mapped to Low/Medium/High/Critical |
| Assignee | Assignee | Matched by full name with existing Direct Reports |
| Story Points | Story Points | Numeric value |
| Project | Project | Matched by name with existing Projects |
| Due Date | Due Date | Supports multiple date formats |

### Supported Jira Statuses

The following Jira statuses are automatically mapped:

- **Backlog**: Backlog
- **Todo/To Do/Selected for Development**: Todo
- **In Progress**: InProgress
- **In Review/Review/Code Review**: InReview
- **Done/Closed/Resolved/Complete/Completed**: Done
- **Cancelled/Canceled**: Cancelled

### Supported Issue Types

- **Bug**: Bug
- **Story/Feature/Epic**: Feature
- **Improvement/Enhancement**: Improvement
- **Research/Spike**: Research
- **Documentation/Docs**: Documentation
- **Task** (default): Task

### Supported Priorities

- **Lowest/Low**: Low
- **Medium/Normal**: Medium
- **High**: High
- **Highest/Critical/Blocker**: Critical

## Duplicate Detection

The importer uses two methods to detect existing tasks:

1. **Issue Key** (Recommended): Matches tasks by Jira Issue Key stored in the `tags` field
   - More reliable for re-imports
   - Preserves relationship with Jira issues

2. **Title**: Matches tasks by exact title match
   - Useful if Issue Key is not available
   - Less reliable if titles have changed

## Update Behavior

When **Update Existing** is enabled:
- Existing tasks are updated with new data from the CSV
- All fields are updated (title, description, status, priority, etc.)
- Task status transitions follow the proper workflow

When **Update Existing** is disabled:
- Existing tasks are skipped and counted in the "Skipped" total
- Only new tasks are created

## Common Issues

### Assignee Not Found

If an assignee name in Jira doesn't match a Direct Report in Hive:
- The task will be imported without an assignee
- You'll see a warning in the results
- Solution: Add the Direct Report to Hive first, or assign manually after import

### Project Not Found

If a project name in Jira doesn't match a Project in Hive:
- The task will be imported without a project assignment
- You'll see a warning in the results
- Solution: Create the Project in Hive first, or assign manually after import

### Invalid Story Points

If story points contain non-numeric values:
- The task will be imported without story points
- No error is generated, just a warning

### Date Format Issues

The importer supports multiple date formats:
- `dd/MMM/yy` (e.g., 01/Jan/24)
- `dd/MMM/yyyy` (e.g., 01/Jan/2024)
- `yyyy-MM-dd` (e.g., 2024-01-01)
- `MM/dd/yyyy` (e.g., 01/01/2024)
- `dd/MM/yyyy` (e.g., 01/01/2024)

If dates fail to parse, tasks are imported without due dates.

## API Endpoints

The import feature exposes two REST API endpoints:

### Preview Import
```
POST /api/jiraimport/preview
Content-Type: application/json

{
  "csvContent": "Issue key,Summary,Status,..."
}
```

Returns a preview with validation results.

### Execute Import
```
POST /api/jiraimport/import
Content-Type: application/json

{
  "csvContent": "Issue key,Summary,Status,...",
  "updateExisting": true,
  "matchField": "IssueKey"
}
```

Returns import results with success/error counts.

## Architecture

### Backend Components

- **JiraImportController** (`src/Hive.Api/Controllers/JiraImportController.cs`)
  - REST API endpoints for preview and import

- **JiraImportService** (`src/Hive.Application/Services/JiraImportService.cs`)
  - Business logic for parsing CSV and mapping fields
  - Handles duplicate detection and validation

- **JiraImportDto** (`src/Hive.Application/DTOs/JiraImportDto.cs`)
  - DTOs for import request, result, and preview

### Frontend Components

- **JiraImport Page** (`src/Hive.Desktop/src/pages/JiraImport.tsx`)
  - React component for the import UI
  - Handles file upload, preview, and import execution

- **API Client** (`src/Hive.Desktop/src/services/api.ts`)
  - TypeScript API client for import endpoints

- **Types** (`src/Hive.Desktop/src/types/index.ts`)
  - TypeScript type definitions for import DTOs

## Future Enhancements

Potential improvements for future releases:

- **Jira API Integration**: Direct API integration instead of CSV import
- **Real-time Sync**: Scheduled synchronization with Jira
- **Custom Field Mapping**: Allow users to configure field mappings
- **Comments Import**: Import Jira comments as meeting notes
- **Attachments**: Import Jira attachments
- **Sprint Import**: Import sprint information
- **Two-way Sync**: Update Jira from Hive changes
