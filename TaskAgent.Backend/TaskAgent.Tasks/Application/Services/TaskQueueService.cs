using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAgent.Tasks.Application.Interfaces;
using TaskAgent.Tasks.Domain.Entities;
using TaskAgent.Tasks.Domain.Enums;
using TaskStatus = TaskAgent.Tasks.Domain.Enums.TaskStatus;

namespace TaskAgent.Tasks.Application.Services;

/// <summary>
/// Service for managing the task queue and task lifecycle operations.
/// Handles task creation, updates, and status transitions.
/// </summary>
public sealed class TaskQueueService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ISettingsRepository _settingsRepository;

    public TaskQueueService(
        ITaskRepository taskRepository,
        ISettingsRepository settingsRepository)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
    }

    /// <summary>
    /// Creates and enqueues a new task.
    /// </summary>
    /// <param name="title">Task title.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="priority">Task priority.</param>
    /// <param name="dueDate">Optional due date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created task.</returns>
    public async Task<TaskItem> EnqueueTaskAsync(
        string title,
        string? description,
        TaskPriority priority,
        DateTimeOffset? dueDate,
        CancellationToken cancellationToken = default)
    {
        var task = new TaskItem(title, description, priority, dueDate);
        await _taskRepository.AddAsync(task, cancellationToken);
        return task;
    }

    /// <summary>
    /// Activates a task from pending or snoozed state.
    /// Enforces maximum active task limit.
    /// </summary>
    /// <param name="taskId">The task ID to activate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if activated successfully, false otherwise.</returns>
    public async Task<bool> ActivateTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
            return false;

        var settings = await _settingsRepository.EnsureExistsAsync(cancellationToken);
        var activeCount = await _taskRepository.CountByStatusAsync(TaskStatus.Active, cancellationToken);

        // Enforce maximum active tasks limit
        if (activeCount >= settings.MaxActiveTasks)
            return false;

        task.Activate();
        await _taskRepository.UpdateAsync(task, cancellationToken);
        return true;
    }

    /// <summary>
    /// Snoozes a task for a specified duration.
    /// </summary>
    /// <param name="taskId">The task ID to snooze.</param>
    /// <param name="duration">Optional duration (uses default if not specified).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if snoozed successfully, false otherwise.</returns>
    public async Task<bool> SnoozeTaskAsync(
        Guid taskId,
        TimeSpan? duration = null,
        CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
            return false;

        var settings = await _settingsRepository.EnsureExistsAsync(cancellationToken);
        var snoozeDuration = duration ?? settings.DefaultSnoozeDuration;
        var snoozeUntil = DateTimeOffset.UtcNow.Add(snoozeDuration);

        task.Snooze(snoozeUntil);
        await _taskRepository.UpdateAsync(task, cancellationToken);
        return true;
    }

    /// <summary>
    /// Escalates a task to critical priority.
    /// </summary>
    /// <param name="taskId">The task ID to escalate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if escalated successfully, false otherwise.</returns>
    public async Task<bool> EscalateTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
            return false;

        task.Escalate();
        await _taskRepository.UpdateAsync(task, cancellationToken);
        return true;
    }

    /// <summary>
    /// Completes a task.
    /// </summary>
    /// <param name="taskId">The task ID to complete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if completed successfully, false otherwise.</returns>
    public async Task<bool> CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
            return false;

        task.Complete();
        await _taskRepository.UpdateAsync(task, cancellationToken);
        return true;
    }

    /// <summary>
    /// Awakens all snoozed tasks whose snooze period has ended.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of tasks awakened.</returns>
    public async Task<int> AwakenSnoozedTasksAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.EnsureExistsAsync(cancellationToken);
        if (!settings.AutoAwakenSnoozedTasks)
            return 0;

        var tasksToAwaken = await _taskRepository.GetTasksToAwakenAsync(cancellationToken);
        var awakenedCount = 0;

        foreach (var task in tasksToAwaken)
        {
            if (task.ShouldAwaken())
            {
                task.ReturnToPending();
                await _taskRepository.UpdateAsync(task, cancellationToken);
                awakenedCount++;
            }
        }

        return awakenedCount;
    }

    /// <summary>
    /// Automatically escalates overdue tasks based on system settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of tasks escalated.</returns>
    public async Task<int> AutoEscalateOverdueTasksAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.EnsureExistsAsync(cancellationToken);
        if (!settings.AutoEscalateOverdueTasks)
            return 0;

        var overdueTasks = await _taskRepository.GetOverdueTasksAsync(cancellationToken);
        var escalationThreshold = TimeSpan.FromHours(settings.EscalationThresholdHours);
        var escalatedCount = 0;

        foreach (var task in overdueTasks)
        {
            if (task.DueDate.HasValue && task.Status != TaskStatus.Escalated)
            {
                var overdueBy = DateTimeOffset.UtcNow - task.DueDate.Value;
                if (overdueBy >= escalationThreshold)
                {
                    task.Escalate();
                    await _taskRepository.UpdateAsync(task, cancellationToken);
                    escalatedCount++;
                }
            }
        }

        return escalatedCount;
    }

    /// <summary>
    /// Retrieves the next best task to work on based on priority and status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The highest priority pending or escalated task, or null if none available.</returns>
    public async Task<TaskItem?> GetNextTaskAsync(CancellationToken cancellationToken = default)
    {
        // First check for escalated tasks
        var escalated = await _taskRepository.GetByStatusAsync(TaskStatus.Escalated, cancellationToken);
        if (escalated.Any())
            return escalated.OrderByDescending(t => t.Priority).ThenBy(t => t.CreatedAt).First();

        // Then check for pending tasks by priority
        var pending = await _taskRepository.GetByStatusAsync(TaskStatus.Pending, cancellationToken);
        if (pending.Any())
            return pending.OrderByDescending(t => t.Priority).ThenBy(t => t.CreatedAt).First();

        return null;
    }

    /// <summary>
    /// Returns a task to pending status.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> ReturnToPendingAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
            return false;

        task.ReturnToPending();
        await _taskRepository.UpdateAsync(task, cancellationToken);
        return true;
    }
}