using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for importing tasks from Jira CSV exports.
/// </summary>
public interface IJiraImportService
{
    /// <summary>
    /// Preview a Jira CSV import without making changes.
    /// </summary>
    Task<JiraImportPreviewDto> PreviewImportAsync(string csvContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Import tasks from a Jira CSV export.
    /// </summary>
    Task<JiraImportResultDto> ImportAsync(JiraImportRequestDto request, CancellationToken cancellationToken = default);
}
