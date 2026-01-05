using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskAgent.Tasks.Application.Interfaces;
using TaskAgent.Tasks.Domain.Entities;
using TaskAgent.Tasks.Domain.Enums;
using TaskAgent.Tasks.Infrastructure.Persistence;
using TaskStatus = TaskAgent.Tasks.Domain.Enums.TaskStatus;

namespace TaskAgent.Tasks.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of the task repository.
/// Handles persistence operations for tasks and recommendations.
/// </summary>
public sealed class TaskRepository : ITaskRepository
{
    private readonly TaskAgentDbContext _context;

    public TaskRepository(TaskAgentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<TaskItem>> GetByStatusAsync(
        TaskStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<TaskItem>> GetOverdueTasksAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _context.Tasks
            .AsNoTracking()
            .Where(t => t.DueDate.HasValue
                     && t.DueDate.Value < now
                     && t.Status != TaskStatus.Completed)
            .OrderBy(t => t.DueDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<TaskItem>> GetTasksToAwakenAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _context.Tasks
            .AsNoTracking()
            .Where(t => t.Status == TaskStatus.Snoozed
                     && t.SnoozedUntil.HasValue
                     && t.SnoozedUntil.Value <= now)
            .OrderBy(t => t.SnoozedUntil)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<TaskItem>> GetByPriorityAsync(
        TaskPriority priority,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Where(t => t.Priority == priority)
            .OrderBy(t => t.Status)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<TaskItem>> GetAllAsync(
        TaskStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Tasks.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        return await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountByStatusAsync(
        TaskStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .Where(t => t.Status == status)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await _context.Tasks.AddAsync(task, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks.FindAsync(new object[] { id }, cancellationToken);
        if (task is not null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<TaskRecommendation?> GetLatestRecommendationAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Recommendations
            .AsNoTracking()
            .Where(r => r.TaskId == taskId)
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddRecommendationAsync(
        TaskRecommendation recommendation,
        CancellationToken cancellationToken = default)
    {
        await _context.Recommendations.AddAsync(recommendation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateRecommendationAsync(
        TaskRecommendation recommendation,
        CancellationToken cancellationToken = default)
    {
        _context.Recommendations.Update(recommendation);
        await _context.SaveChangesAsync(cancellationToken);
    }
}