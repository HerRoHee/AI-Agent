namespace TaskAgent.Web.DTO;

/// <summary>
/// Request DTO for creating a new task.
/// </summary>
public sealed record CreateTaskRequest
{
    /// <summary>
    /// Task title (required).
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Optional task description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Task priority: "Low", "Medium", "High", "Critical".
    /// </summary>
    public required string Priority { get; init; }

    /// <summary>
    /// Optional due date in ISO 8601 format.
    /// </summary>
    public DateTimeOffset? DueDate { get; init; }
}

/// <summary>
/// Response DTO for task operations.
/// </summary>
public sealed record TaskResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Status { get; init; }
    public required string Priority { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? SnoozedUntil { get; init; }
    public required int EscalationCount { get; init; }
}

/// <summary>
/// Response DTO for agent tick results.
/// </summary>
public sealed record TickResultResponse
{
    public required string AgentType { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required bool HasWork { get; init; }
    public string? PerceptSummary { get; init; }
    public string? ActionSummary { get; init; }
    public string? ResultSummary { get; init; }
    public string? ExperienceSummary { get; init; }
}

/// <summary>
/// Response DTO for system statistics.
/// </summary>
public sealed record SystemStatsResponse
{
    public required int TotalTasks { get; init; }
    public required int PendingTasks { get; init; }
    public required int ActiveTasks { get; init; }
    public required int SnoozedTasks { get; init; }
    public required int EscalatedTasks { get; init; }
    public required int CompletedTasks { get; init; }
    public required int RejectedTasks { get; init; }
    public required int OverdueTasks { get; init; }
}

/// <summary>
/// Request DTO for snoozing a task.
/// </summary>
public sealed record SnoozeTaskRequest
{
    /// <summary>
    /// How many hours to snooze (optional, defaults to 4 hours).
    /// </summary>
    public double? SnoozeDurationHours { get; init; }
}

