using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAgent.Tasks.Application.DTO;
using TaskAgent.Tasks.Application.Interfaces;
using TaskAgent.Tasks.Domain.Entities;

namespace TaskAgent.Tasks.Application.Services;

/// <summary>
/// Service for agent learning and system adaptation.
/// Implements the Learn phase by analyzing experiences and adapting settings.
/// </summary>
public sealed class LearningService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly List<TaskScoringExperience> _experienceHistory = new();
    private readonly object _lock = new();

    public LearningService(
        ITaskRepository taskRepository,
        ISettingsRepository settingsRepository)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
    }

    /// <summary>
    /// Records a learning experience from a task scoring tick.
    /// </summary>
    /// <param name="percept">The perception data.</param>
    /// <param name="action">The action taken.</param>
    /// <param name="result">The result of the action.</param>
    /// <param name="scoredTasks">The tasks that were scored.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The learning experience, or null if no learning occurred.</returns>
    public Task<TaskScoringExperience?> RecordExperienceAsync(
        TaskPercept? percept,
        TaskAction? action,
        TaskActionResult? result,
        IReadOnlyCollection<ScoredTask> scoredTasks,
        CancellationToken cancellationToken = default)
    {
        if (percept is null || scoredTasks.Count == 0)
            return Task.FromResult<TaskScoringExperience?>(null);

        var experience = new TaskScoringExperience
        {
            TasksProcessed = percept.Tasks.Count,
            TasksScored = scoredTasks.Count,
            ActionsExecuted = action is not null ? 1 : 0,
            SuccessfulActions = result?.Success == true ? 1 : 0,
            AverageUrgencyScore = scoredTasks.Average(st => st.UrgencyScore),
            MaxUrgencyScore = scoredTasks.Max(st => st.UrgencyScore),
            PerformanceSummary = BuildPerformanceSummary(scoredTasks, action, result)
        };

        // Store experience in history for adaptation analysis
        lock (_lock)
        {
            _experienceHistory.Add(experience);

            // Keep only last 100 experiences to prevent unbounded growth
            if (_experienceHistory.Count > 100)
                _experienceHistory.RemoveAt(0);
        }

        return Task.FromResult<TaskScoringExperience?>(experience);
    }

    /// <summary>
    /// Analyzes recent experiences to determine if system adaptation is needed.
    /// </summary>
    /// <param name="analysisWindow">Time window to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Adaptation perception data, or null if insufficient data.</returns>
    public async Task<AdaptationPercept?> AnalyzePerformanceAsync(
        TimeSpan analysisWindow,
        CancellationToken cancellationToken = default)
    {
        List<TaskScoringExperience> recentExperiences;

        lock (_lock)
        {
            recentExperiences = _experienceHistory.ToList();
        }

        if (recentExperiences.Count < 5) // Need minimum data for analysis
            return null;

        var settings = await _settingsRepository.EnsureExistsAsync(cancellationToken);
        var statistics = await GatherStatisticsAsync(cancellationToken);

        return new AdaptationPercept
        {
            RecentExperiences = recentExperiences,
            CurrentSettings = settings,
            Statistics = statistics,
            AnalysisWindow = analysisWindow
        };
    }

    /// <summary>
    /// Decides if settings should be adapted based on performance analysis.
    /// </summary>
    /// <param name="percept">The adaptation perception data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An adaptation action, or null if no adaptation needed.</returns>
    public Task<AdaptationAction?> DecideAdaptationAsync(
        AdaptationPercept percept,
        CancellationToken cancellationToken = default)
    {
        var metrics = CalculatePerformanceMetrics(percept);
        var trend = DeterminePerformanceTrend(percept.RecentExperiences);

        // Check if adaptation is needed based on performance
        var adaptationType = DetermineAdaptationType(metrics, percept.Statistics, percept.CurrentSettings);

        if (adaptationType is null)
            return Task.FromResult<AdaptationAction?>(null);

        var (updatedSettings, reasoning, confidence) = CreateAdaptedSettings(
            adaptationType,
            percept.CurrentSettings,
            metrics,
            percept.Statistics
        );

        return Task.FromResult<AdaptationAction?>(new AdaptationAction
        {
            AdaptationType = adaptationType,
            UpdatedSettings = updatedSettings,
            Reasoning = reasoning,
            Confidence = confidence
        });
    }

    /// <summary>
    /// Applies an adaptation action by updating system settings.
    /// </summary>
    /// <param name="action">The adaptation action to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the adaptation.</returns>
    public async Task<AdaptationResult> ApplyAdaptationAsync(
        AdaptationAction action,
        CancellationToken cancellationToken = default)
    {
        var previousSettings = await _settingsRepository.GetAsync(cancellationToken);

        if (action.UpdatedSettings is null)
        {
            return new AdaptationResult
            {
                Action = action,
                Success = false,
                ErrorMessage = "No settings to apply"
            };
        }

        try
        {
            // Validate settings consistency before applying
            action.UpdatedSettings.ValidateConsistency();

            await _settingsRepository.SaveAsync(action.UpdatedSettings, cancellationToken);

            return new AdaptationResult
            {
                Action = action,
                Success = true,
                PreviousSettings = previousSettings,
                NewSettings = action.UpdatedSettings
            };
        }
        catch (Exception ex)
        {
            return new AdaptationResult
            {
                Action = action,
                Success = false,
                ErrorMessage = ex.Message,
                PreviousSettings = previousSettings
            };
        }
    }

    /// <summary>
    /// Records an adaptation experience for future learning.
    /// </summary>
    /// <param name="percept">The adaptation perception.</param>
    /// <param name="action">The adaptation action taken.</param>
    /// <param name="result">The result of the adaptation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The adaptation experience, or null if no learning occurred.</returns>
    public Task<AdaptationExperience?> RecordAdaptationExperienceAsync(
        AdaptationPercept? percept,
        AdaptationAction? action,
        AdaptationResult? result,
        CancellationToken cancellationToken = default)
    {
        if (percept is null)
            return Task.FromResult<AdaptationExperience?>(null);

        var metrics = CalculatePerformanceMetrics(percept);
        var trend = DeterminePerformanceTrend(percept.RecentExperiences);

        var experience = new AdaptationExperience
        {
            ExperiencesAnalyzed = percept.RecentExperiences.Count,
            AdaptationTriggered = action is not null,
            PerformanceTrend = trend,
            TriggerMetrics = metrics,
            Summary = BuildAdaptationSummary(action, result, trend)
        };

        return Task.FromResult<AdaptationExperience?>(experience);
    }

    /// <summary>
    /// Gathers current system statistics for adaptation analysis.
    /// </summary>
    private async Task<SystemStatistics> GatherStatisticsAsync(CancellationToken cancellationToken)
    {
        var activeTasks = await _taskRepository.GetByStatusAsync(Domain.Enums.TaskStatus.Active, cancellationToken);
        var pendingTasks = await _taskRepository.GetByStatusAsync(Domain.Enums.TaskStatus.Pending, cancellationToken);
        var snoozedTasks = await _taskRepository.GetByStatusAsync(Domain.Enums.TaskStatus.Snoozed, cancellationToken);
        var escalatedTasks = await _taskRepository.GetByStatusAsync(Domain.Enums.TaskStatus.Escalated, cancellationToken);
        var completedTasks = await _taskRepository.GetByStatusAsync(Domain.Enums.TaskStatus.Completed, cancellationToken);
        var overdueTasks = await _taskRepository.GetOverdueTasksAsync(cancellationToken);

        return new SystemStatistics
        {
            ActiveTaskCount = activeTasks.Count,
            PendingTaskCount = pendingTasks.Count,
            SnoozedTaskCount = snoozedTasks.Count,
            EscalatedTaskCount = escalatedTasks.Count,
            CompletedTaskCount = completedTasks.Count,
            OverdueTaskCount = overdueTasks.Count
        };
    }

    /// <summary>
    /// Calculates performance metrics from experiences.
    /// </summary>
    private Dictionary<string, double> CalculatePerformanceMetrics(AdaptationPercept percept)
    {
        var experiences = percept.RecentExperiences;

        return new Dictionary<string, double>
        {
            ["AverageUrgency"] = experiences.Average(e => e.AverageUrgencyScore),
            ["MaxUrgency"] = experiences.Max(e => e.MaxUrgencyScore),
            ["SuccessRate"] = experiences.Average(e =>
                e.ActionsExecuted > 0 ? (double)e.SuccessfulActions / e.ActionsExecuted : 1.0),
            ["TaskLoad"] = experiences.Average(e => e.TasksProcessed),
            ["ActionRate"] = experiences.Average(e => e.ActionsExecuted)
        };
    }

    /// <summary>
    /// Determines the performance trend from experience history.
    /// </summary>
    private string DeterminePerformanceTrend(IReadOnlyCollection<TaskScoringExperience> experiences)
    {
        if (experiences.Count < 5)
            return "Insufficient data";

        var recent = experiences.TakeLast(3).Average(e => e.AverageUrgencyScore);
        var older = experiences.Take(experiences.Count - 3).Average(e => e.AverageUrgencyScore);

        var change = recent - older;

        if (Math.Abs(change) < 0.05)
            return "Stable";

        return change > 0 ? "Degrading" : "Improving";
    }

    /// <summary>
    /// Determines what type of adaptation is needed.
    /// </summary>
    private string? DetermineAdaptationType(
        Dictionary<string, double> metrics,
        SystemStatistics statistics,
        SystemSettings settings)
    {
        // High urgency and at capacity -> increase capacity
        if (metrics["AverageUrgency"] > 0.7 && statistics.ActiveTaskCount >= settings.MaxActiveTasks * 0.9)
            return "IncreaseCapacity";

        // Low urgency and underutilized -> decrease capacity
        if (metrics["AverageUrgency"] < 0.3 && statistics.ActiveTaskCount < settings.MaxActiveTasks * 0.5)
            return "DecreaseCapacity";

        // Many overdue tasks -> reduce escalation threshold
        if (statistics.OverdueTaskCount > settings.MaxActiveTasks * 0.5)
            return "ReduceEscalationThreshold";

        // Low action rate -> increase confidence threshold
        if (metrics["ActionRate"] < 0.2)
            return "IncreaseConfidenceThreshold";

        return null;
    }

    /// <summary>
    /// Creates adapted settings based on adaptation type.
    /// </summary>
    private (SystemSettings settings, string reasoning, double confidence) CreateAdaptedSettings(
        string adaptationType,
        SystemSettings current,
        Dictionary<string, double> metrics,
        SystemStatistics statistics)
    {
        var adapted = current.Clone();
        string reasoning;
        double confidence;

        switch (adaptationType)
        {
            case "IncreaseCapacity":
                var newMax = Math.Min(current.MaxActiveTasks + 5, 100);
                adapted.UpdateMaxActiveTasks(newMax);
                reasoning = $"High urgency ({metrics["AverageUrgency"]:F2}) at capacity. Increasing from {current.MaxActiveTasks} to {newMax}.";
                confidence = 0.80;
                break;

            case "DecreaseCapacity":
                var reducedMax = Math.Max(current.MaxActiveTasks - 3, 1);
                adapted.UpdateMaxActiveTasks(reducedMax);
                reasoning = $"Low urgency ({metrics["AverageUrgency"]:F2}) underutilized. Reducing from {current.MaxActiveTasks} to {reducedMax}.";
                confidence = 0.70;
                break;

            case "ReduceEscalationThreshold":
                var newThreshold = Math.Max(current.EscalationThresholdHours - 6, 1);
                adapted.UpdateEscalationThreshold(newThreshold);
                reasoning = $"High overdue count ({statistics.OverdueTaskCount}). Reducing threshold from {current.EscalationThresholdHours}h to {newThreshold}h.";
                confidence = 0.85;
                break;

            case "IncreaseConfidenceThreshold":
                var newConfidence = Math.Min(current.MinimumConfidenceThreshold + 0.05, 1.0);
                adapted.UpdateMinimumConfidenceThreshold(newConfidence);
                reasoning = $"Low action rate ({metrics["ActionRate"]:F2}). Increasing confidence threshold from {current.MinimumConfidenceThreshold:F2} to {newConfidence:F2}.";
                confidence = 0.75;
                break;

            default:
                reasoning = "No adaptation applied.";
                confidence = 0.0;
                break;
        }

        return (adapted, reasoning, confidence);
    }

    /// <summary>
    /// Builds a performance summary string.
    /// </summary>
    private string BuildPerformanceSummary(
        IReadOnlyCollection<ScoredTask> scoredTasks,
        TaskAction? action,
        TaskActionResult? result)
    {
        var parts = new List<string>
        {
            $"Scored {scoredTasks.Count} tasks",
            $"Avg urgency: {scoredTasks.Average(st => st.UrgencyScore):F2}",
            $"Max urgency: {scoredTasks.Max(st => st.UrgencyScore):F2}"
        };

        if (action is not null)
            parts.Add($"Action: {action.ActionType}");

        if (result is not null)
            parts.Add($"Result: {(result.Success ? "Success" : "Failed")}");

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Builds an adaptation summary string.
    /// </summary>
    private string BuildAdaptationSummary(
        AdaptationAction? action,
        AdaptationResult? result,
        string trend)
    {
        if (action is null)
            return $"No adaptation needed. Trend: {trend}";

        var success = result?.Success == true ? "succeeded" : "failed";
        return $"Adaptation ({action.AdaptationType}) {success}. Trend: {trend}. {action.Reasoning}";
    }
}