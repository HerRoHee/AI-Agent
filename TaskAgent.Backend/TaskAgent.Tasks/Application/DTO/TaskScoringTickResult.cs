using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAgent.Core;
using TaskAgent.Tasks.Domain.Entities;

namespace TaskAgent.Tasks.Application.DTO;

/// <summary>
/// Represents the input for task evaluation (perception).
/// </summary>
public sealed record TaskPercept
{
    /// <summary>
    /// Collection of tasks to be evaluated.
    /// </summary>
    public required IReadOnlyCollection<TaskItem> Tasks { get; init; }

    /// <summary>
    /// Current system settings at the time of perception.
    /// </summary>
    public required SystemSettings Settings { get; init; }

    /// <summary>
    /// Current UTC timestamp of the perception.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents a scored task with evaluation metrics.
/// </summary>
public sealed record ScoredTask
{
    /// <summary>
    /// The task being scored.
    /// </summary>
    public required TaskItem Task { get; init; }

    /// <summary>
    /// Overall urgency score (0.0 to 1.0).
    /// </summary>
    public required double UrgencyScore { get; init; }

    /// <summary>
    /// Priority contribution to the score.
    /// </summary>
    public required double PriorityWeight { get; init; }

    /// <summary>
    /// Time-based contribution to the score.
    /// </summary>
    public required double TimeWeight { get; init; }

    /// <summary>
    /// Status-based contribution to the score.
    /// </summary>
    public required double StatusWeight { get; init; }

    /// <summary>
    /// Indicates if this task should be escalated.
    /// </summary>
    public bool ShouldEscalate { get; init; }

    /// <summary>
    /// Indicates if this task should be awakened from snooze.
    /// </summary>
    public bool ShouldAwaken { get; init; }

    /// <summary>
    /// Reasoning for the score.
    /// </summary>
    public string? Reasoning { get; init; }
}

/// <summary>
/// Represents an action to take on tasks (policy decision).
/// </summary>
public sealed record TaskAction
{
    /// <summary>
    /// The task to act upon.
    /// </summary>
    public required TaskItem Task { get; init; }

    /// <summary>
    /// The type of action to perform.
    /// </summary>
    public required string ActionType { get; init; }

    /// <summary>
    /// The recommendation associated with this action.
    /// </summary>
    public TaskRecommendation? Recommendation { get; init; }
}

/// <summary>
/// Represents the result of executing an action (actuator output).
/// </summary>
public sealed record TaskActionResult
{
    /// <summary>
    /// The action that was executed.
    /// </summary>
    public required TaskAction Action { get; init; }

    /// <summary>
    /// Whether the action was successfully executed.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Error message if the action failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The updated task after action execution.
    /// </summary>
    public TaskItem? UpdatedTask { get; init; }
}

/// <summary>
/// Represents learning data from a task scoring tick.
/// </summary>
public sealed record TaskScoringExperience
{
    /// <summary>
    /// Number of tasks processed.
    /// </summary>
    public int TasksProcessed { get; init; }

    /// <summary>
    /// Number of tasks scored.
    /// </summary>
    public int TasksScored { get; init; }

    /// <summary>
    /// Number of actions executed.
    /// </summary>
    public int ActionsExecuted { get; init; }

    /// <summary>
    /// Number of successful actions.
    /// </summary>
    public int SuccessfulActions { get; init; }

    /// <summary>
    /// Average urgency score across all tasks.
    /// </summary>
    public double AverageUrgencyScore { get; init; }

    /// <summary>
    /// Highest urgency score encountered.
    /// </summary>
    public double MaxUrgencyScore { get; init; }

    /// <summary>
    /// Performance metrics for learning.
    /// </summary>
    public string? PerformanceSummary { get; init; }
}

/// <summary>
/// Result of a task scoring and evaluation tick.
/// </summary>
public sealed class TaskScoringTickResult : TickResult<TaskPercept, TaskAction, TaskActionResult, TaskScoringExperience>
{
    /// <summary>
    /// Collection of all scored tasks from this tick.
    /// </summary>
    public IReadOnlyCollection<ScoredTask> ScoredTasks { get; init; } = Array.Empty<ScoredTask>();

    /// <summary>
    /// Number of tasks that were evaluated.
    /// </summary>
    public int TasksEvaluated => ScoredTasks.Count;
}