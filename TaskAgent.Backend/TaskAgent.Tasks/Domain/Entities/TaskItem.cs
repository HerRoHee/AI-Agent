using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAgent.Tasks.Domain.Enums;
using TaskAgent.Tasks.Domain.Exceptions;
using TaskStatus = TaskAgent.Tasks.Domain.Enums.TaskStatus;

namespace TaskAgent.Tasks.Domain.Entities;

/// <summary>
/// Represents a task item in the system.
/// Encapsulates all task state and enforces domain rules.
/// </summary>
public sealed class TaskItem
{
    private TaskStatus _status;
    private TaskPriority _priority;

    /// <summary>
    /// Gets the unique identifier for this task.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the title of the task.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Gets the optional description of the task.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the current status of the task.
    /// </summary>
    public TaskStatus Status
    {
        get => _status;
        private set => _status = value;
    }

    /// <summary>
    /// Gets the priority level of the task.
    /// </summary>
    public TaskPriority Priority
    {
        get => _priority;
        private set => _priority = value;
    }

    /// <summary>
    /// Gets the timestamp when the task was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last update to the task.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the optional due date for the task.
    /// </summary>
    public DateTimeOffset? DueDate { get; private set; }

    /// <summary>
    /// Gets the timestamp when the task was completed, if applicable.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp until which the task is snoozed, if applicable.
    /// </summary>
    public DateTimeOffset? SnoozedUntil { get; private set; }

    /// <summary>
    /// Gets the number of times this task has been escalated.
    /// </summary>
    public int EscalationCount { get; private set; }

    /// <summary>
    /// Private constructor for ORM/serialization use.
    /// </summary>
    private TaskItem()
    {
        Title = string.Empty;
    }

    /// <summary>
    /// Creates a new task item with the specified details.
    /// </summary>
    /// <param name="title">The task title. Cannot be null or whitespace.</param>
    /// <param name="description">Optional task description.</param>
    /// <param name="priority">The initial priority level.</param>
    /// <param name="dueDate">Optional due date.</param>
    /// <exception cref="ArgumentException">Thrown when title is null or whitespace.</exception>
    public TaskItem(string title, string? description, TaskPriority priority, DateTimeOffset? dueDate = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title cannot be null or whitespace.", nameof(title));

        Id = Guid.NewGuid();
        Title = title.Trim();
        Description = description?.Trim();
        _status = TaskStatus.Pending;
        _priority = priority;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        DueDate = dueDate;
        EscalationCount = 0;
    }

    /// <summary>
    /// Updates the task title.
    /// </summary>
    /// <param name="title">The new title. Cannot be null or whitespace.</param>
    /// <exception cref="TaskAlreadyCompletedException">Thrown when task is completed.</exception>
    /// <exception cref="ArgumentException">Thrown when title is invalid.</exception>
    public void UpdateTitle(string title)
    {
        EnsureNotCompleted();

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title cannot be null or whitespace.", nameof(title));

        Title = title.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the task description.
    /// </summary>
    /// <param name="description">The new description.</param>
    /// <exception cref="TaskAlreadyCompletedException">Thrown when task is completed.</exception>
    public void UpdateDescription(string? description)
    {
        EnsureNotCompleted();
        Description = description?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the task priority.
    /// </summary>
    /// <param name="priority">The new priority level.</param>
    /// <exception cref="TaskAlreadyCompletedException">Thrown when task is completed.</exception>
    public void UpdatePriority(TaskPriority priority)
    {
        EnsureNotCompleted();
        _priority = priority;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the task due date.
    /// </summary>
    /// <param name="dueDate">The new due date, or null to clear it.</param>
    /// <exception cref="TaskAlreadyCompletedException">Thrown when task is completed.</exception>
    public void UpdateDueDate(DateTimeOffset? dueDate)
    {
        EnsureNotCompleted();
        DueDate = dueDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Transitions the task to Active status.
    /// Valid from: Pending, Snoozed
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">Thrown when transition is invalid.</exception>
    public void Activate()
    {
        EnsureValidTransition(TaskStatus.Active);
        _status = TaskStatus.Active;
        SnoozedUntil = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Snoozes the task until the specified time.
    /// Valid from: Pending, Active
    /// </summary>
    /// <param name="until">The time until which the task should be snoozed.</param>
    /// <exception cref="InvalidStateTransitionException">Thrown when transition is invalid.</exception>
    /// <exception cref="ArgumentException">Thrown when snooze time is in the past.</exception>
    public void Snooze(DateTimeOffset until)
    {
        EnsureValidTransition(TaskStatus.Snoozed);

        if (until <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Snooze time must be in the future.", nameof(until));

        _status = TaskStatus.Snoozed;
        SnoozedUntil = until;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Escalates the task to Critical priority and Escalated status.
    /// Valid from: Pending, Active, Snoozed
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">Thrown when transition is invalid.</exception>
    public void Escalate()
    {
        EnsureValidTransition(TaskStatus.Escalated);
        _status = TaskStatus.Escalated;
        _priority = TaskPriority.Critical;
        EscalationCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the task as completed.
    /// Valid from: any non-Completed status
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">Thrown when already completed.</exception>
    public void Complete()
    {
        EnsureValidTransition(TaskStatus.Completed);
        _status = TaskStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        SnoozedUntil = null;
    }

    /// <summary>
    /// Returns the task to Pending status.
    /// Valid from: Active, Snoozed, Escalated
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">Thrown when transition is invalid.</exception>
    public void ReturnToPending()
    {
        EnsureValidTransition(TaskStatus.Pending);
        _status = TaskStatus.Pending;
        SnoozedUntil = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Checks if the task is overdue based on the current time.
    /// </summary>
    public bool IsOverdue()
    {
        return DueDate.HasValue
            && DueDate.Value < DateTimeOffset.UtcNow
            && Status != TaskStatus.Completed;
    }

    /// <summary>
    /// Checks if the task should be awakened from snooze.
    /// </summary>
    public bool ShouldAwaken()
    {
        return Status == TaskStatus.Snoozed
            && SnoozedUntil.HasValue
            && SnoozedUntil.Value <= DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Ensures the task is not in a completed state.
    /// </summary>
    private void EnsureNotCompleted()
    {
        if (Status == TaskStatus.Completed)
            throw new TaskAlreadyCompletedException(Id);
    }

    /// <summary>
    /// Validates that a transition to the target status is allowed.
    /// </summary>
    private void EnsureValidTransition(TaskStatus targetStatus)
    {
        if (!IsValidTransition(Status, targetStatus))
            throw new InvalidStateTransitionException(Status.ToString(), targetStatus.ToString());
    }

    /// <summary>
    /// Determines if a transition from one status to another is valid.
    /// Encapsulates all state transition rules.
    /// </summary>
    private static bool IsValidTransition(TaskStatus from, TaskStatus to)
    {
        // Completed is a terminal state
        if (from == TaskStatus.Completed)
            return false;

        return (from, to) switch
        {
            // From Pending
            (TaskStatus.Pending, TaskStatus.Active) => true,
            (TaskStatus.Pending, TaskStatus.Snoozed) => true,
            (TaskStatus.Pending, TaskStatus.Escalated) => true,
            (TaskStatus.Pending, TaskStatus.Completed) => true,

            // From Active
            (TaskStatus.Active, TaskStatus.Pending) => true,
            (TaskStatus.Active, TaskStatus.Snoozed) => true,
            (TaskStatus.Active, TaskStatus.Escalated) => true,
            (TaskStatus.Active, TaskStatus.Completed) => true,

            // From Snoozed
            (TaskStatus.Snoozed, TaskStatus.Pending) => true,
            (TaskStatus.Snoozed, TaskStatus.Active) => true,
            (TaskStatus.Snoozed, TaskStatus.Escalated) => true,
            (TaskStatus.Snoozed, TaskStatus.Completed) => true,

            // From Escalated
            (TaskStatus.Escalated, TaskStatus.Pending) => true,
            (TaskStatus.Escalated, TaskStatus.Active) => true,
            (TaskStatus.Escalated, TaskStatus.Completed) => true,

            // All other transitions are invalid
            _ => false
        };
    }
}