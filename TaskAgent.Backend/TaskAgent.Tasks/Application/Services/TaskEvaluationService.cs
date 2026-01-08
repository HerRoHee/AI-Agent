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
/// Service for evaluating and scoring tasks based on urgency and priority heuristics.
/// Implements the agent's perception and thinking logic for task management.
/// </summary>
public sealed class TaskEvaluationService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ISettingsRepository _settingsRepository;

    public TaskEvaluationService(
        ITaskRepository taskRepository,
        ISettingsRepository settingsRepository)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
    }

    /// <summary>
    /// Gathers perception data for task evaluation.
    /// FILTERS OUT terminal tasks (Completed, Rejected).
    /// </summary>
    public async Task<TaskPercept?> GatherPerceptionAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.EnsureExistsAsync(cancellationToken);
        var tasks = await _taskRepository.GetAllAsync(cancellationToken: cancellationToken);

        // Filter out TERMINAL tasks (Completed + Rejected)
        // Agent should NEVER process these
        var activeTasks = tasks
            .Where(t => !t.IsTerminal()) // ← Key change
            .ToList();

        if (activeTasks.Count == 0)
            return null;

        return new TaskPercept
        {
            Tasks = activeTasks,
            Settings = settings,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Scores a collection of tasks based on urgency heuristics.
    /// </summary>
    /// <param name="percept">The perception data containing tasks to score.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of scored tasks ordered by urgency.</returns>
    public Task<IReadOnlyCollection<ScoredTask>> ScoreTasksAsync(
        TaskPercept percept,
        CancellationToken cancellationToken = default)
    {
        var scoredTasks = percept.Tasks
            .Select(task => ScoreTask(task, percept.Settings))
            .OrderByDescending(st => st.UrgencyScore)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<ScoredTask>>(scoredTasks);
    }

    /// <summary>
    /// Scores an individual task using a weighted heuristic algorithm.
    /// </summary>
    /// <param name="task">The task to score.</param>
    /// <param name="settings">Current system settings.</param>
    /// <returns>A scored task with urgency metrics.</returns>
    private ScoredTask ScoreTask(TaskItem task, SystemSettings settings)
    {
        // Calculate priority weight (0.0 to 1.0)
        var priorityWeight = task.Priority switch
        {
            TaskPriority.Critical => 1.0,
            TaskPriority.High => 0.75,
            TaskPriority.Medium => 0.5,
            TaskPriority.Low => 0.25,
            _ => 0.5
        };

        // Calculate time-based weight (0.0 to 1.0)
        var timeWeight = CalculateTimeWeight(task);

        // Calculate status-based weight (0.0 to 1.0)
        var statusWeight = task.Status switch
        {
            TaskStatus.Escalated => 1.0,
            TaskStatus.Active => 0.8,
            TaskStatus.Pending => 0.6,
            TaskStatus.Snoozed => 0.2,
            TaskStatus.Completed => 0.0,
            _ => 0.5
        };

        // Weighted urgency score with configurable weights
        // Priority: 40%, Time: 35%, Status: 25%
        var urgencyScore = (priorityWeight * 0.40) + (timeWeight * 0.35) + (statusWeight * 0.25);

        // Determine if task should be escalated
        var shouldEscalate = ShouldEscalateTask(task, settings);

        // Determine if task should be awakened
        var shouldAwaken = task.ShouldAwaken();

        var reasoning = BuildScoreReasoning(task, priorityWeight, timeWeight, statusWeight, shouldEscalate, shouldAwaken);

        return new ScoredTask
        {
            Task = task,
            UrgencyScore = urgencyScore,
            PriorityWeight = priorityWeight,
            TimeWeight = timeWeight,
            StatusWeight = statusWeight,
            ShouldEscalate = shouldEscalate,
            ShouldAwaken = shouldAwaken,
            Reasoning = reasoning
        };
    }

    /// <summary>
    /// Calculates a time-based weight factor for task urgency.
    /// </summary>
    /// <param name="task">The task to evaluate.</param>
    /// <returns>A weight between 0.0 and 1.0 based on time factors.</returns>
    private double CalculateTimeWeight(TaskItem task)
    {
        if (!task.DueDate.HasValue)
            return 0.3; // Base weight for tasks without due dates

        var now = DateTimeOffset.UtcNow;
        var timeUntilDue = task.DueDate.Value - now;

        // Overdue tasks get maximum weight
        if (timeUntilDue <= TimeSpan.Zero)
        {
            var overdueBy = now - task.DueDate.Value;
            // Exponentially increase weight for overdue tasks
            var overdueDays = overdueBy.TotalDays;
            return Math.Min(1.0, 0.8 + (overdueDays * 0.05));
        }

        // Tasks due soon get higher weight
        var hoursUntilDue = timeUntilDue.TotalHours;

        if (hoursUntilDue <= 1)
            return 0.95;
        if (hoursUntilDue <= 4)
            return 0.85;
        if (hoursUntilDue <= 12)
            return 0.70;
        if (hoursUntilDue <= 24)
            return 0.55;
        if (hoursUntilDue <= 72)
            return 0.40;

        // Tasks with distant due dates get lower weight
        return 0.25;
    }

    /// <summary>
    /// Determines if a task should be escalated based on business rules.
    /// </summary>
    /// <param name="task">The task to evaluate.</param>
    /// <param name="settings">Current system settings.</param>
    /// <returns>True if the task should be escalated.</returns>
    private bool ShouldEscalateTask(TaskItem task, SystemSettings settings)
    {
        // Already escalated tasks don't need re-escalation
        if (task.Status == TaskStatus.Escalated)
            return false;

        // Check if auto-escalation is enabled
        if (!settings.AutoEscalateOverdueTasks)
            return false;

        // Escalate if overdue by threshold
        if (task.IsOverdue() && task.DueDate.HasValue)
        {
            var overdueBy = DateTimeOffset.UtcNow - task.DueDate.Value;
            var escalationThreshold = TimeSpan.FromHours(settings.EscalationThresholdHours);
            return overdueBy >= escalationThreshold;
        }

        return false;
    }

    /// <summary>
    /// Builds a human-readable reasoning string for the score.
    /// </summary>
    private string BuildScoreReasoning(
        TaskItem task,
        double priorityWeight,
        double timeWeight,
        double statusWeight,
        bool shouldEscalate,
        bool shouldAwaken)
    {
        var parts = new List<string>
        {
            $"Priority: {task.Priority} (weight: {priorityWeight:F2})",
            $"Time factor: {timeWeight:F2}",
            $"Status: {task.Status} (weight: {statusWeight:F2})"
        };

        if (task.IsOverdue())
            parts.Add("OVERDUE");

        if (shouldEscalate)
            parts.Add("Needs escalation");

        if (shouldAwaken)
            parts.Add("Ready to awaken");

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Identifies the highest priority task that needs immediate action.
    /// </summary>
    /// <param name="scoredTasks">Collection of scored tasks.</param>
    /// <returns>The task requiring most urgent attention, or null.</returns>
    public ScoredTask? GetMostUrgentTask(IReadOnlyCollection<ScoredTask> scoredTasks)
    {
        return scoredTasks
            .Where(st => st.Task.Status != TaskStatus.Completed)
            .OrderByDescending(st => st.UrgencyScore)
            .ThenByDescending(st => st.Task.Priority)
            .ThenBy(st => st.Task.CreatedAt)
            .FirstOrDefault();
    }

    /// <summary>
    /// Filters scored tasks by a minimum urgency threshold.
    /// </summary>
    /// <param name="scoredTasks">Collection of scored tasks.</param>
    /// <param name="minimumScore">Minimum urgency score threshold.</param>
    /// <returns>Tasks meeting the threshold.</returns>
    public IReadOnlyCollection<ScoredTask> FilterByUrgency(
        IReadOnlyCollection<ScoredTask> scoredTasks,
        double minimumScore)
    {
        return scoredTasks
            .Where(st => st.UrgencyScore >= minimumScore)
            .OrderByDescending(st => st.UrgencyScore)
            .ToList();
    }
}