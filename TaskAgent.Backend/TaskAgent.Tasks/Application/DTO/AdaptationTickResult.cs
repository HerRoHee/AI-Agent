using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAgent.Core;
using TaskAgent.Tasks.Application.DTO;
using TaskAgent.Tasks.Domain.Entities;

namespace TaskAgent.Tasks.Application.DTO;

/// <summary>
/// Represents the input for system adaptation (perception).
/// </summary>
public sealed record AdaptationPercept
{
    /// <summary>
    /// Recent task scoring experiences for analysis.
    /// </summary>
    public required IReadOnlyCollection<TaskScoringExperience> RecentExperiences { get; init; }

    /// <summary>
    /// Current system settings.
    /// </summary>
    public required SystemSettings CurrentSettings { get; init; }

    /// <summary>
    /// Current system statistics.
    /// </summary>
    public required SystemStatistics Statistics { get; init; }

    /// <summary>
    /// Time window for the experiences.
    /// </summary>
    public TimeSpan AnalysisWindow { get; init; }
}

/// <summary>
/// Represents current system statistics.
/// </summary>
public sealed record SystemStatistics
{
    /// <summary>
    /// Total number of active tasks.
    /// </summary>
    public int ActiveTaskCount { get; init; }

    /// <summary>
    /// Total number of pending tasks.
    /// </summary>
    public int PendingTaskCount { get; init; }

    /// <summary>
    /// Total number of snoozed tasks.
    /// </summary>
    public int SnoozedTaskCount { get; init; }

    /// <summary>
    /// Total number of escalated tasks.
    /// </summary>
    public int EscalatedTaskCount { get; init; }

    /// <summary>
    /// Total number of completed tasks.
    /// </summary>
    public int CompletedTaskCount { get; init; }

    /// <summary>
    /// Average task completion time.
    /// </summary>
    public TimeSpan? AverageCompletionTime { get; init; }

    /// <summary>
    /// Number of overdue tasks.
    /// </summary>
    public int OverdueTaskCount { get; init; }
}

/// <summary>
/// Represents an adaptation decision (policy output).
/// </summary>
public sealed record AdaptationAction
{
    /// <summary>
    /// Type of adaptation to perform.
    /// </summary>
    public required string AdaptationType { get; init; }

    /// <summary>
    /// Updated settings to apply.
    /// </summary>
    public SystemSettings? UpdatedSettings { get; init; }

    /// <summary>
    /// Reasoning for the adaptation.
    /// </summary>
    public required string Reasoning { get; init; }

    /// <summary>
    /// Confidence in this adaptation (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; init; }
}

/// <summary>
/// Represents the result of applying an adaptation (actuator output).
/// </summary>
public sealed record AdaptationResult
{
    /// <summary>
    /// The action that was executed.
    /// </summary>
    public required AdaptationAction Action { get; init; }

    /// <summary>
    /// Whether the adaptation was successfully applied.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Error message if the adaptation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The settings before adaptation.
    /// </summary>
    public SystemSettings? PreviousSettings { get; init; }

    /// <summary>
    /// The settings after adaptation.
    /// </summary>
    public SystemSettings? NewSettings { get; init; }
}

/// <summary>
/// Represents learning data from an adaptation tick.
/// </summary>
public sealed record AdaptationExperience
{
    /// <summary>
    /// Number of experiences analyzed.
    /// </summary>
    public int ExperiencesAnalyzed { get; init; }

    /// <summary>
    /// Whether adaptation was triggered.
    /// </summary>
    public bool AdaptationTriggered { get; init; }

    /// <summary>
    /// Performance trend detected (Improving, Stable, Degrading).
    /// </summary>
    public string? PerformanceTrend { get; init; }

    /// <summary>
    /// Metrics that triggered adaptation.
    /// </summary>
    public Dictionary<string, double>? TriggerMetrics { get; init; }

    /// <summary>
    /// Summary of the adaptation learning.
    /// </summary>
    public string? Summary { get; init; }
}

/// <summary>
/// Result of a system adaptation tick.
/// </summary>
public sealed class AdaptationTickResult : TickResult<AdaptationPercept, AdaptationAction, AdaptationResult, AdaptationExperience>
{
    /// <summary>
    /// Performance metrics before adaptation.
    /// </summary>
    public Dictionary<string, double> MetricsBefore { get; init; } = new();

    /// <summary>
    /// Performance metrics after adaptation.
    /// </summary>
    public Dictionary<string, double> MetricsAfter { get; init; } = new();

    /// <summary>
    /// Whether settings were changed during this tick.
    /// </summary>
    public bool SettingsChanged { get; init; }
}