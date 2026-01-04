using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAgent.Tasks.Application.DTO;
using TaskAgent.Tasks.Application.Interfaces;
using TaskAgent.Tasks.Domain.Entities;
using TaskAgent.Tasks.Domain.Enums;
using TaskStatus = TaskAgent.Tasks.Domain.Enums.TaskStatus;

namespace TaskAgent.Tasks.Application.Services;

/// <summary>
/// Service for generating and managing intelligent task recommendations.
/// Implements the agent's policy decision-making for task actions.
/// </summary>
public sealed class RecommendationService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ISettingsRepository _settingsRepository;

    public RecommendationService(
        ITaskRepository taskRepository,
        ISettingsRepository settingsRepository)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
    }

    /// <summary>
    /// Generates a recommendation for the most urgent task based on scoring.
    /// </summary>
    /// <param name="scoredTasks">Collection of scored tasks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task action recommendation, or null if no action needed.</returns>
    public async Task<TaskAction?> GenerateRecommendationAsync(
        IReadOnlyCollection<ScoredTask> scoredTasks,
        CancellationToken cancellationToken = default)
    {
        if (scoredTasks.Count == 0)
            return null;

        var settings = await _settingsRepository.EnsureExistsAsync(cancellationToken);

        // Find the most urgent task that needs action
        var urgentTask = scoredTasks
            .Where(st => st.Task.Status != TaskStatus.Completed)
            .OrderByDescending(st => st.UrgencyScore)
            .FirstOrDefault();

        if (urgentTask is null)
            return null;

        // Generate recommendation based on task state and urgency
        var recommendation = await CreateRecommendationAsync(urgentTask, settings, cancellationToken);

        if (recommendation is null)
            return null;

        // Store the recommendation
        await _taskRepository.AddRecommendationAsync(recommendation, cancellationToken);

        return new TaskAction
        {
            Task = urgentTask.Task,
            ActionType = recommendation.RecommendedAction,
            Recommendation = recommendation
        };
    }

    /// <summary>
    /// Creates a recommendation based on task scoring and heuristics.
    /// </summary>
    private async Task<TaskRecommendation?> CreateRecommendationAsync(
        ScoredTask scoredTask,
        SystemSettings settings,
        CancellationToken cancellationToken)
    {
        var task = scoredTask.Task;

        // Check for escalation need
        if (scoredTask.ShouldEscalate && task.Status != TaskStatus.Escalated)
        {
            return new TaskRecommendation(
                taskId: task.Id,
                recommendedAction: "Escalate",
                reasoning: $"Task is overdue by escalation threshold. Urgency score: {scoredTask.UrgencyScore:F2}",
                confidenceScore: 0.95,
                validityDuration: settings.RecommendationValidityDuration,
                recommendedPriority: TaskPriority.Critical
            );
        }

        // Check for awakening from snooze
        if (scoredTask.ShouldAwaken)
        {
            return new TaskRecommendation(
                taskId: task.Id,
                recommendedAction: "Awaken",
                reasoning: $"Snooze period has ended. Ready to return to pending status.",
                confidenceScore: 1.0,
                validityDuration: settings.RecommendationValidityDuration
            );
        }

        // Check if task should be activated
        if (task.Status == TaskStatus.Pending && scoredTask.UrgencyScore >= 0.7)
        {
            var activeCount = await _taskRepository.CountByStatusAsync(TaskStatus.Active, cancellationToken);
            if (activeCount < settings.MaxActiveTasks)
            {
                return new TaskRecommendation(
                    taskId: task.Id,
                    recommendedAction: "Activate",
                    reasoning: $"High urgency score ({scoredTask.UrgencyScore:F2}) and capacity available for active tasks.",
                    confidenceScore: scoredTask.UrgencyScore,
                    validityDuration: settings.RecommendationValidityDuration
                );
            }
        }

        // Check if low-urgency task should be snoozed
        if ((task.Status == TaskStatus.Pending || task.Status == TaskStatus.Active)
            && scoredTask.UrgencyScore < 0.3
            && !task.IsOverdue())
        {
            var activeCount = await _taskRepository.CountByStatusAsync(TaskStatus.Active, cancellationToken);
            if (activeCount >= settings.MaxActiveTasks * 0.8) // At 80% capacity
            {
                return new TaskRecommendation(
                    taskId: task.Id,
                    recommendedAction: "Snooze",
                    reasoning: $"Low urgency score ({scoredTask.UrgencyScore:F2}) and system near capacity. Defer to reduce load.",
                    confidenceScore: 0.70,
                    validityDuration: settings.RecommendationValidityDuration,
                    recommendedSnoozeDuration: settings.DefaultSnoozeDuration
                );
            }
        }

        // Check if active task should be returned to pending
        if (task.Status == TaskStatus.Active && scoredTask.UrgencyScore < 0.4)
        {
            return new TaskRecommendation(
                taskId: task.Id,
                recommendedAction: "ReturnToPending",
                reasoning: $"Urgency decreased ({scoredTask.UrgencyScore:F2}). Consider deprioritizing to free capacity.",
                confidenceScore: 0.65,
                validityDuration: settings.RecommendationValidityDuration
            );
        }

        // No action needed
        return null;
    }

    /// <summary>
    /// Evaluates if a recommendation should be auto-applied based on confidence.
    /// </summary>
    /// <param name="recommendation">The recommendation to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the recommendation should be auto-applied.</returns>
    public async Task<bool> ShouldAutoApplyAsync(
        TaskRecommendation recommendation,
        CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.EnsureExistsAsync(cancellationToken);

        return settings.AutoApplyRecommendations
            && recommendation.IsValid()
            && recommendation.MeetsConfidenceThreshold(settings.MinimumConfidenceThreshold);
    }

    /// <summary>
    /// Retrieves the latest valid recommendation for a task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest valid recommendation, or null if none exists.</returns>
    public async Task<TaskRecommendation?> GetLatestRecommendationAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var recommendation = await _taskRepository.GetLatestRecommendationAsync(taskId, cancellationToken);

        if (recommendation is null || !recommendation.IsValid())
            return null;

        return recommendation;
    }

    /// <summary>
    /// Marks a recommendation as applied.
    /// </summary>
    /// <param name="recommendation">The recommendation to mark.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task MarkRecommendationAppliedAsync(
        TaskRecommendation recommendation,
        CancellationToken cancellationToken = default)
    {
        recommendation.MarkAsApplied();
        await _taskRepository.UpdateRecommendationAsync(recommendation, cancellationToken);
    }

    /// <summary>
    /// Generates recommendations for multiple high-urgency tasks.
    /// </summary>
    /// <param name="scoredTasks">Collection of scored tasks.</param>
    /// <param name="maxRecommendations">Maximum number of recommendations to generate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of task actions with recommendations.</returns>
    public async Task<IReadOnlyCollection<TaskAction>> GenerateBatchRecommendationsAsync(
        IReadOnlyCollection<ScoredTask> scoredTasks,
        int maxRecommendations,
        CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.EnsureExistsAsync(cancellationToken);
        var actions = new List<TaskAction>();

        var urgentTasks = scoredTasks
            .Where(st => st.Task.Status != TaskStatus.Completed)
            .OrderByDescending(st => st.UrgencyScore)
            .Take(maxRecommendations);

        foreach (var scoredTask in urgentTasks)
        {
            var recommendation = await CreateRecommendationAsync(scoredTask, settings, cancellationToken);
            if (recommendation is not null)
            {
                await _taskRepository.AddRecommendationAsync(recommendation, cancellationToken);

                actions.Add(new TaskAction
                {
                    Task = scoredTask.Task,
                    ActionType = recommendation.RecommendedAction,
                    Recommendation = recommendation
                });
            }
        }

        return actions;
    }
}