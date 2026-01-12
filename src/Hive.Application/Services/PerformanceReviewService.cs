using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for PerformanceReview management.
/// </summary>
public class PerformanceReviewService : IPerformanceReviewService
{
    private readonly IPerformanceReviewRepository _reviewRepository;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IActivityService _activityService;

    public PerformanceReviewService(
        IPerformanceReviewRepository reviewRepository,
        IDirectReportRepository directReportRepository,
        IActivityService activityService)
    {
        _reviewRepository = reviewRepository ?? throw new ArgumentNullException(nameof(reviewRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    public async Task<PerformanceReviewDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _reviewRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);
        return MapToDto(entity, directReport?.FullName ?? "Unknown");
    }

    public async Task<IReadOnlyList<PerformanceReviewDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _reviewRepository.GetAllAsync(cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<PerformanceReviewDto>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var entities = await _reviewRepository.GetByDirectReportIdAsync(directReportId, cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<PerformanceReviewDto>> GetByStatusAsync(ReviewStatus status, CancellationToken cancellationToken = default)
    {
        var entities = await _reviewRepository.GetByStatusAsync(status, cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<PerformanceReviewDto> CreateAsync(CreatePerformanceReviewDto dto, CancellationToken cancellationToken = default)
    {
        // Validate direct report exists
        var directReport = await _directReportRepository.GetByIdAsync(dto.DirectReportId, cancellationToken);
        if (directReport is null)
        {
            throw new NotFoundException(nameof(DirectReport), dto.DirectReportId);
        }

        // Business rule: No duplicate reviews for same period
        if (await _reviewRepository.HasReviewForPeriodAsync(dto.DirectReportId, dto.ReviewPeriod, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A performance review for '{dto.ReviewPeriod}' already exists for this direct report.");
        }

        var entity = new PerformanceReview(dto.DirectReportId, dto.ReviewPeriod, dto.ReviewDate);
        var created = await _reviewRepository.AddAsync(entity, cancellationToken);

        // Log activity
        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Review,
            created.Id,
            $"Performance Review - {created.ReviewPeriod}",
            "New performance review created",
            cancellationToken);

        return MapToDto(created, directReport.FullName);
    }

    public async Task<PerformanceReviewDto> UpdateContentAsync(Guid id, UpdatePerformanceReviewContentDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.UpdateContent(
            dto.Strengths,
            dto.AreasForImprovement,
            dto.GoalsForNextPeriod,
            dto.ManagerNotes,
            dto.Rating);

        await _reviewRepository.UpdateAsync(entity, cancellationToken);

        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);
        return MapToDto(entity, directReport?.FullName ?? "Unknown");
    }

    public async Task<PerformanceReviewDto> UpdateSelfAssessmentAsync(Guid id, UpdateSelfAssessmentDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.UpdateSelfAssessment(dto.SelfAssessment);
        await _reviewRepository.UpdateAsync(entity, cancellationToken);

        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);
        return MapToDto(entity, directReport?.FullName ?? "Unknown");
    }

    public async Task<PerformanceReviewDto> SubmitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Submit();
        await _reviewRepository.UpdateAsync(entity, cancellationToken);

        // Log activity
        await _activityService.LogActivityAsync(
            ActivityType.StatusChanged,
            EntityType.Review,
            entity.Id,
            $"Performance Review - {entity.ReviewPeriod}",
            "Performance review submitted",
            cancellationToken);

        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);
        return MapToDto(entity, directReport?.FullName ?? "Unknown");
    }

    public async Task<PerformanceReviewDto> AcknowledgeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Acknowledge();
        await _reviewRepository.UpdateAsync(entity, cancellationToken);

        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);
        return MapToDto(entity, directReport?.FullName ?? "Unknown");
    }

    public async Task<PerformanceReviewDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Complete();
        await _reviewRepository.UpdateAsync(entity, cancellationToken);

        // Log activity
        await _activityService.LogActivityAsync(
            ActivityType.Completed,
            EntityType.Review,
            entity.Id,
            $"Performance Review - {entity.ReviewPeriod}",
            "Performance review completed",
            cancellationToken);

        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);
        return MapToDto(entity, directReport?.FullName ?? "Unknown");
    }

    public async Task<PerformanceReviewDto> ReopenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Reopen();
        await _reviewRepository.UpdateAsync(entity, cancellationToken);

        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);
        return MapToDto(entity, directReport?.FullName ?? "Unknown");
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _reviewRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(PerformanceReview), id);
        }

        await _reviewRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.Review,
            id,
            $"Performance Review - {entity.ReviewPeriod}",
            $"Performance review was deleted",
            cancellationToken);
    }

    private async Task<PerformanceReview> GetEntityOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _reviewRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(PerformanceReview), id);
        }
        return entity;
    }

    private async Task<IReadOnlyList<PerformanceReviewDto>> MapToDtosAsync(
        IReadOnlyList<PerformanceReview> entities,
        CancellationToken cancellationToken)
    {
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var directReportNames = directReports.ToDictionary(dr => dr.Id, dr => dr.FullName);

        return entities.Select(e => MapToDto(e, directReportNames.GetValueOrDefault(e.DirectReportId, "Unknown"))).ToList();
    }

    private static PerformanceReviewDto MapToDto(PerformanceReview entity, string directReportName) => new()
    {
        Id = entity.Id,
        DirectReportId = entity.DirectReportId,
        DirectReportName = directReportName,
        ReviewPeriod = entity.ReviewPeriod,
        ReviewDate = entity.ReviewDate,
        Rating = entity.Rating,
        RatingDescription = GetRatingDescription(entity.Rating),
        Status = entity.Status,
        StatusDescription = GetStatusDescription(entity.Status),
        Strengths = entity.Strengths,
        AreasForImprovement = entity.AreasForImprovement,
        GoalsForNextPeriod = entity.GoalsForNextPeriod,
        ManagerNotes = entity.ManagerNotes,
        EmployeeSelfAssessment = entity.EmployeeSelfAssessment,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        SubmittedAt = entity.SubmittedAt,
        AcknowledgedAt = entity.AcknowledgedAt
    };

    private static string GetRatingDescription(PerformanceRating rating) => rating switch
    {
        PerformanceRating.NotRated => "Not Rated",
        PerformanceRating.NeedsImprovement => "Needs Improvement",
        PerformanceRating.MeetsExpectations => "Meets Expectations",
        PerformanceRating.ExceedsExpectations => "Exceeds Expectations",
        PerformanceRating.Outstanding => "Outstanding",
        _ => "Unknown"
    };

    private static string GetStatusDescription(ReviewStatus status) => status switch
    {
        ReviewStatus.Draft => "Draft",
        ReviewStatus.Submitted => "Submitted",
        ReviewStatus.Acknowledged => "Acknowledged",
        ReviewStatus.Completed => "Completed",
        _ => "Unknown"
    };
}
