using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAgent.Tasks.Domain.Entities;
using TaskAgent.Tasks.Domain.Enums;
using TaskStatus = TaskAgent.Tasks.Domain.Enums.TaskStatus;

namespace TaskAgent.Tasks.Application.Interfaces;

/// <summary>
/// Repository interface for task persistence operations.
/// Implementations are provided by the Infrastructure layer.
/// </summary>
public interface ITaskRepository
{
    /// <summary>
    /// Retrieves a task by its unique identifier.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The task, or null if not found.</returns>
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tasks with the specified status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of tasks matching the status.</returns>
    Task<IReadOnlyCollection<TaskItem>> GetByStatusAsync(TaskStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tasks that are overdue.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of overdue tasks.</returns>
    Task<IReadOnlyCollection<TaskItem>> GetOverdueTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tasks that should be awakened from snooze.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of tasks ready to be awakened.</returns>
    Task<IReadOnlyCollection<TaskItem>> GetTasksToAwakenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tasks with the specified priority.
    /// </summary>
    /// <param name="priority">The priority to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of tasks matching the priority.</returns>
    Task<IReadOnlyCollection<TaskItem>> GetByPriorityAsync(TaskPriority priority, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tasks, optionally filtered by status.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of tasks.</returns>
    Task<IReadOnlyCollection<TaskItem>> GetAllAsync(TaskStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts tasks by status.
    /// </summary>
    /// <param name="status">The status to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of tasks with the specified status.</returns>
    Task<int> CountByStatusAsync(TaskStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new task to the repository.
    /// </summary>
    /// <param name="task">The task to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing task in the repository.
    /// </summary>
    /// <param name="task">The task to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a task from the repository.
    /// </summary>
    /// <param name="id">The ID of the task to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent recommendation for a task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The most recent recommendation, or null if none exists.</returns>
    Task<TaskRecommendation?> GetLatestRecommendationAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new recommendation to the repository.
    /// </summary>
    /// <param name="recommendation">The recommendation to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddRecommendationAsync(TaskRecommendation recommendation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing recommendation in the repository.
    /// </summary>
    /// <param name="recommendation">The recommendation to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateRecommendationAsync(TaskRecommendation recommendation, CancellationToken cancellationToken = default);
}