using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for DirectReport management.
/// Following Single Responsibility Principle (SRP) - handles only DirectReport-related use cases.
/// </summary>
public class DirectReportService : IDirectReportService
{
    private readonly IDirectReportRepository _repository;
    private readonly IActivityService _activityService;

    public DirectReportService(IDirectReportRepository repository, IActivityService activityService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    public async Task<DirectReportDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<IReadOnlyList<DirectReportDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<DirectReportDto> CreateAsync(CreateDirectReportDto dto, CancellationToken cancellationToken = default)
    {
        // Business rule: Email must be unique
        if (await _repository.EmailExistsAsync(dto.Email, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A direct report with email '{dto.Email}' already exists.");
        }

        var entity = new DirectReport(
            dto.FirstName,
            dto.LastName,
            dto.Email,
            dto.JobTitle,
            dto.Department,
            dto.HireDate,
            dto.IsDirect);

        var created = await _repository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.DirectReport,
            created.Id,
            created.FullName,
            $"Team member {created.FullName} was added",
            cancellationToken);

        return MapToDto(created);
    }

    public async Task<DirectReportDto> UpdateAsync(Guid id, UpdateDirectReportDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(DirectReport), id);
        }

        // Business rule: Email must be unique (excluding current entity)
        if (await _repository.EmailExistsAsync(dto.Email, id, cancellationToken))
        {
            throw new ConflictException($"A direct report with email '{dto.Email}' already exists.");
        }

        entity.Update(
            dto.FirstName,
            dto.LastName,
            dto.Email,
            dto.JobTitle,
            dto.Department,
            dto.HireDate,
            dto.IsDirect);

        await _repository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.DirectReport,
            entity.Id,
            entity.FullName,
            $"Team member {entity.FullName} was updated",
            cancellationToken);

        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(DirectReport), id);
        }

        await _repository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.DirectReport,
            id,
            entity.FullName,
            $"Team member {entity.FullName} was removed",
            cancellationToken);
    }

    public async Task<BulkImportResultDto> BulkImportAsync(BulkImportDirectReportsDto dto, CancellationToken cancellationToken = default)
    {
        var results = new List<BulkImportRowResult>();
        var errors = new List<string>();
        int successCount = 0;
        int skippedCount = 0;
        int errorCount = 0;

        if (string.IsNullOrWhiteSpace(dto.CsvContent))
        {
            return new BulkImportResultDto
            {
                TotalRows = 0,
                SuccessCount = 0,
                SkippedCount = 0,
                ErrorCount = 1,
                Results = results,
                Errors = new List<string> { "CSV content is empty" }
            };
        }

        var lines = dto.CsvContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return new BulkImportResultDto
            {
                TotalRows = 0,
                SuccessCount = 0,
                SkippedCount = 0,
                ErrorCount = 1,
                Results = results,
                Errors = new List<string> { "CSV must have a header row and at least one data row" }
            };
        }

        // Parse header
        var header = ParseCsvLine(lines[0]);
        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Length; i++)
        {
            columnMap[header[i].Trim()] = i;
        }

        // Validate required columns
        var requiredColumns = new[] { "FirstName", "LastName", "Email" };
        var missingColumns = requiredColumns.Where(c => !columnMap.ContainsKey(c)).ToList();
        if (missingColumns.Any())
        {
            return new BulkImportResultDto
            {
                TotalRows = 0,
                SuccessCount = 0,
                SkippedCount = 0,
                ErrorCount = 1,
                Results = results,
                Errors = new List<string> { $"Missing required columns: {string.Join(", ", missingColumns)}" }
            };
        }

        // Process rows
        for (int i = 1; i < lines.Length; i++)
        {
            var rowNumber = i + 1;
            var values = ParseCsvLine(lines[i]);

            try
            {
                var firstName = GetColumnValue(values, columnMap, "FirstName");
                var lastName = GetColumnValue(values, columnMap, "LastName");
                var email = GetColumnValue(values, columnMap, "Email");
                var jobTitle = GetColumnValue(values, columnMap, "JobTitle") ?? "";
                var department = GetColumnValue(values, columnMap, "Department") ?? "";
                var hireDateStr = GetColumnValue(values, columnMap, "HireDate");
                var isDirectStr = GetColumnValue(values, columnMap, "IsDirect");

                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
                {
                    results.Add(new BulkImportRowResult
                    {
                        RowNumber = rowNumber,
                        Email = email ?? "",
                        Status = "Error",
                        Message = "FirstName, LastName, and Email are required"
                    });
                    errorCount++;
                    continue;
                }

                // Check for existing email
                if (await _repository.EmailExistsAsync(email, cancellationToken: cancellationToken))
                {
                    if (dto.SkipDuplicates)
                    {
                        results.Add(new BulkImportRowResult
                        {
                            RowNumber = rowNumber,
                            Email = email,
                            Status = "Skipped",
                            Message = "Email already exists"
                        });
                        skippedCount++;
                        continue;
                    }
                    else
                    {
                        results.Add(new BulkImportRowResult
                        {
                            RowNumber = rowNumber,
                            Email = email,
                            Status = "Error",
                            Message = "Email already exists"
                        });
                        errorCount++;
                        continue;
                    }
                }

                // Parse hire date
                DateTime hireDate = DateTime.Today;
                if (!string.IsNullOrWhiteSpace(hireDateStr))
                {
                    if (!DateTime.TryParse(hireDateStr, out hireDate))
                    {
                        hireDate = DateTime.Today;
                    }
                }

                // Parse IsDirect (default to true if not specified)
                bool isDirect = true;
                if (!string.IsNullOrWhiteSpace(isDirectStr))
                {
                    isDirect = isDirectStr.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                               isDirectStr.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                               isDirectStr.Equals("1", StringComparison.OrdinalIgnoreCase);
                }

                var entity = new DirectReport(firstName, lastName, email, jobTitle, department, hireDate, isDirect);
                var created = await _repository.AddAsync(entity, cancellationToken);

                results.Add(new BulkImportRowResult
                {
                    RowNumber = rowNumber,
                    Email = email,
                    Status = "Created",
                    DirectReport = MapToDto(created)
                });
                successCount++;
            }
            catch (Exception ex)
            {
                results.Add(new BulkImportRowResult
                {
                    RowNumber = rowNumber,
                    Email = "",
                    Status = "Error",
                    Message = ex.Message
                });
                errorCount++;
            }
        }

        return new BulkImportResultDto
        {
            TotalRows = lines.Length - 1,
            SuccessCount = successCount,
            SkippedCount = skippedCount,
            ErrorCount = errorCount,
            Results = results,
            Errors = errors
        };
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString().Trim());

        return result.ToArray();
    }

    private static string? GetColumnValue(string[] values, Dictionary<string, int> columnMap, string columnName)
    {
        if (columnMap.TryGetValue(columnName, out int index) && index < values.Length)
        {
            var value = values[index].Trim();
            return string.IsNullOrEmpty(value) ? null : value;
        }
        return null;
    }

    private static DirectReportDto MapToDto(DirectReport entity) => new()
    {
        Id = entity.Id,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        FullName = entity.FullName,
        Email = entity.Email,
        JobTitle = entity.JobTitle,
        Department = entity.Department,
        HireDate = entity.HireDate,
        IsDirect = entity.IsDirect,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
