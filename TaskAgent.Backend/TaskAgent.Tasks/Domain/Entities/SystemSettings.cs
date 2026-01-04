using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAgent.Tasks.Domain.Exceptions;

namespace TaskAgent.Tasks.Domain.Entities;

/// <summary>
/// Represents system-wide configuration settings for the task agent.
/// Enforces valid configuration bounds and rules.
/// </summary>
public sealed class SystemSettings
{
    private int _maxActiveTasks;
    private int _escalationThresholdHours;
    private double _minimumConfidenceThreshold;
    private TimeSpan _defaultSnoozeDuration;
    private TimeSpan _recommendationValidityDuration;

    /// <summary>
    /// Gets the unique identifier for this settings instance.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the maximum number of tasks that can be in Active status simultaneously.
    /// Valid range: 1 to 100.
    /// </summary>
    public int MaxActiveTasks
    {
        get => _maxActiveTasks;
        private set
        {
            if (value < 1 || value > 100)
                throw new InvariantViolationException("MaxActiveTasks must be between 1 and 100.");
            _maxActiveTasks = value;
        }
    }

    /// <summary>
    /// Gets the number of hours after due date before a task is automatically escalated.
    /// Valid range: 1 to 168 (1 week).
    /// </summary>
    public int EscalationThresholdHours
    {
        get => _escalationThresholdHours;
        private set
        {
            if (value < 1 || value > 168)
                throw new InvariantViolationException("EscalationThresholdHours must be between 1 and 168.");
            _escalationThresholdHours = value;
        }
    }

    /// <summary>
    /// Gets the minimum confidence score required for a recommendation to be auto-applied.
    /// Valid range: 0.0 to 1.0.
    /// </summary>
    public double MinimumConfidenceThreshold
    {
        get => _minimumConfidenceThreshold;
        private set
        {
            if (value < 0.0 || value > 1.0)
                throw new InvariantViolationException("MinimumConfidenceThreshold must be between 0.0 and 1.0.");
            _minimumConfidenceThreshold = value;
        }
    }

    /// <summary>
    /// Gets the default duration for which tasks are snoozed when no specific duration is provided.
    /// Valid range: 1 minute to 30 days.
    /// </summary>
    public TimeSpan DefaultSnoozeDuration
    {
        get => _defaultSnoozeDuration;
        private set
        {
            if (value < TimeSpan.FromMinutes(1) || value > TimeSpan.FromDays(30))
                throw new InvariantViolationException("DefaultSnoozeDuration must be between 1 minute and 30 days.");
            _defaultSnoozeDuration = value;
        }
    }

    /// <summary>
    /// Gets the duration for which recommendations remain valid before expiring.
    /// Valid range: 1 minute to 24 hours.
    /// </summary>
    public TimeSpan RecommendationValidityDuration
    {
        get => _recommendationValidityDuration;
        private set
        {
            if (value < TimeSpan.FromMinutes(1) || value > TimeSpan.FromHours(24))
                throw new InvariantViolationException("RecommendationValidityDuration must be between 1 minute and 24 hours.");
            _recommendationValidityDuration = value;
        }
    }

    /// <summary>
    /// Gets whether the agent should automatically apply high-confidence recommendations.
    /// </summary>
    public bool AutoApplyRecommendations { get; private set; }

    /// <summary>
    /// Gets whether overdue tasks should be automatically escalated.
    /// </summary>
    public bool AutoEscalateOverdueTasks { get; private set; }

    /// <summary>
    /// Gets whether snoozed tasks should be automatically awakened when the snooze period ends.
    /// </summary>
    public bool AutoAwakenSnoozedTasks { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last update to these settings.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a new system settings instance with default values.
    /// </summary>
    public SystemSettings()
    {
        Id = Guid.NewGuid();
        _maxActiveTasks = 10;
        _escalationThresholdHours = 24;
        _minimumConfidenceThreshold = 0.75;
        _defaultSnoozeDuration = TimeSpan.FromHours(4);
        _recommendationValidityDuration = TimeSpan.FromHours(1);
        AutoApplyRecommendations = false;
        AutoEscalateOverdueTasks = true;
        AutoAwakenSnoozedTasks = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the maximum number of active tasks.
    /// </summary>
    /// <param name="maxActiveTasks">The new maximum (1 to 100).</param>
    public void UpdateMaxActiveTasks(int maxActiveTasks)
    {
        MaxActiveTasks = maxActiveTasks;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the escalation threshold in hours.
    /// </summary>
    /// <param name="hours">The new threshold in hours (1 to 168).</param>
    public void UpdateEscalationThreshold(int hours)
    {
        EscalationThresholdHours = hours;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the minimum confidence threshold for recommendations.
    /// </summary>
    /// <param name="threshold">The new threshold (0.0 to 1.0).</param>
    public void UpdateMinimumConfidenceThreshold(double threshold)
    {
        MinimumConfidenceThreshold = threshold;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the default snooze duration.
    /// </summary>
    /// <param name="duration">The new duration (1 minute to 30 days).</param>
    public void UpdateDefaultSnoozeDuration(TimeSpan duration)
    {
        DefaultSnoozeDuration = duration;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the recommendation validity duration.
    /// </summary>
    /// <param name="duration">The new duration (1 minute to 24 hours).</param>
    public void UpdateRecommendationValidityDuration(TimeSpan duration)
    {
        RecommendationValidityDuration = duration;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Enables or disables automatic application of recommendations.
    /// </summary>
    public void SetAutoApplyRecommendations(bool enabled)
    {
        AutoApplyRecommendations = enabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Enables or disables automatic escalation of overdue tasks.
    /// </summary>
    public void SetAutoEscalateOverdueTasks(bool enabled)
    {
        AutoEscalateOverdueTasks = enabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Enables or disables automatic awakening of snoozed tasks.
    /// </summary>
    public void SetAutoAwakenSnoozedTasks(bool enabled)
    {
        AutoAwakenSnoozedTasks = enabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Validates that the current settings are internally consistent.
    /// </summary>
    public void ValidateConsistency()
    {
        // If auto-apply is enabled, the confidence threshold should be reasonably high
        if (AutoApplyRecommendations && MinimumConfidenceThreshold < 0.5)
        {
            throw new InvariantViolationException(
                "When AutoApplyRecommendations is enabled, MinimumConfidenceThreshold should be at least 0.5 to ensure quality.");
        }

        // Recommendation validity should be reasonable relative to default snooze duration
        if (RecommendationValidityDuration > DefaultSnoozeDuration)
        {
            throw new InvariantViolationException(
                "RecommendationValidityDuration should not exceed DefaultSnoozeDuration to prevent stale recommendations.");
        }
    }

    /// <summary>
    /// Creates a copy of the current settings.
    /// Useful for creating backups before modifications.
    /// </summary>
    public SystemSettings Clone()
    {
        return new SystemSettings
        {
            Id = Guid.NewGuid(), // New ID for the clone
            _maxActiveTasks = this._maxActiveTasks,
            _escalationThresholdHours = this._escalationThresholdHours,
            _minimumConfidenceThreshold = this._minimumConfidenceThreshold,
            _defaultSnoozeDuration = this._defaultSnoozeDuration,
            _recommendationValidityDuration = this._recommendationValidityDuration,
            AutoApplyRecommendations = this.AutoApplyRecommendations,
            AutoEscalateOverdueTasks = this.AutoEscalateOverdueTasks,
            AutoAwakenSnoozedTasks = this.AutoAwakenSnoozedTasks,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}