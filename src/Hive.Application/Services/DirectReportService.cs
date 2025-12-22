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

    public DirectReportService(IDirectReportRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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
            dto.HireDate);

        var created = await _repository.AddAsync(entity, cancellationToken);
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
            dto.HireDate);

        await _repository.UpdateAsync(entity, cancellationToken);
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await _repository.ExistsAsync(id, cancellationToken))
        {
            throw new NotFoundException(nameof(DirectReport), id);
        }

        await _repository.DeleteAsync(id, cancellationToken);
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
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
