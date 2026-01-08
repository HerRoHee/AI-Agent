using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAgent.Tasks.Application.Interfaces;
using TaskAgent.Tasks.Domain.Entities;
using TaskAgent.Tasks.Domain.Enums;
using TaskAgent.Tasks.Domain.Exceptions;

namespace TaskAgent.Tasks.Application.Services;

/// <summary>
/// Service for handling user-initiated actions on tasks.
/// These are INTENTS from users, not agent decisions.
/// </summary>
public sealed class UserActionService
{
    private readonly ITaskRepository _taskRepository;

    public UserActionService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
    }

    /// <summary>
    /// Handles user intent to complete a task.
    /// Validates the task exists and transition is allowed.
    /// </summary>
    /// <param name="taskId">The task to complete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false if task not found.</returns>
    /// <exception cref="InvalidStateTransitionException">If transition is not allowed.</exception>
    /// <exception cref="TaskAlreadyCompletedException">If task is already completed.</exception>
    public async Task<bool> RequestCompleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
            return false;

        // Domain method enforces transition rules
        task.CompleteByUser();

        await _taskRepository.UpdateAsync(task, cancellationToken);
        return true;
    }

    /// <summary>
    /// Handles user intent to snooze a task.
    /// Validates the task exists, transition is allowed, and snooze time is valid.
    /// </summary>
    /// <param name="taskId">The task to snooze.</param>
    /// <param name="snoozeDuration">How long to snooze (defaults to 4 hours).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false if task not found.</returns>
    /// <exception cref="InvalidStateTransitionException">If transition is not allowed.</exception>
    /// <exception cref="ArgumentException">If snooze time is invalid.</exception>
    public async Task<bool> RequestSnoozeTaskAsync(
        Guid taskId,
        TimeSpan? snoozeDuration = null,
        CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
            return false;

        // Default snooze: 4 hours
        var duration = snoozeDuration ?? TimeSpan.FromHours(4);
        var snoozeUntil = DateTimeOffset.UtcNow.Add(duration);

        // Domain method enforces transition rules
        task.SnoozeByUser(snoozeUntil);

        await _taskRepository.UpdateAsync(task, cancellationToken);
        return true;
    }

    /// <summary>
    /// Handles user intent to reject a task.
    /// Validates the task exists and transition is allowed.
    /// Rejected tasks are terminal - agent will never process them.
    /// </summary>
    /// <param name="taskId">The task to reject.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false if task not found.</returns>
    /// <exception cref="InvalidStateTransitionException">If transition is not allowed.</exception>
    public async Task<bool> RequestRejectTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
            return false;

        // Domain method enforces transition rules
        task.RejectByUser();

        await _taskRepository.UpdateAsync(task, cancellationToken);
        return true;
    }
}