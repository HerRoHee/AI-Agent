using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAgent.Core;
using TaskAgent.Tasks.Application.DTO;
using TaskAgent.Tasks.Application.Services;

namespace TaskAgent.Tasks.Runners;

/// <summary>
/// Agent runner responsible for system adaptation and learning.
/// Implements the Sense → Think → Act → Learn loop for self-optimization.
/// Analyzes performance trends and adapts system settings accordingly.
/// </summary>
public sealed class TaskAdaptationAgentRunner : SoftwareAgent<AdaptationPercept, AdaptationAction, AdaptationResult, AdaptationExperience>
{
    private readonly LearningService _learningService;
    private readonly TimeSpan _analysisWindow;
    private Dictionary<string, double> _metricsBefore = new();
    private Dictionary<string, double> _metricsAfter = new();

    /// <summary>
    /// Initializes a new instance of the task adaptation agent runner.
    /// </summary>
    /// <param name="learningService">The learning service for performance analysis.</param>
    /// <param name="analysisWindow">Time window for performance analysis (default: 1 hour).</param>
    public TaskAdaptationAgentRunner(
        LearningService learningService,
        TimeSpan? analysisWindow = null)
        : base(
            new AdaptationPerceptionSource(learningService, analysisWindow ?? TimeSpan.FromHours(1)),
            new AdaptationPolicy(learningService),
            new AdaptationActuator(learningService),
            new AdaptationLearningComponent(learningService))
    {
        _learningService = learningService ?? throw new ArgumentNullException(nameof(learningService));
        _analysisWindow = analysisWindow ?? TimeSpan.FromHours(1);
    }

    /// <summary>
    /// Executes one atomic tick of the adaptation agent.
    /// Returns null if no work was performed.
    /// </summary>
    public new async Task<AdaptationTickResult?> StepAsync(CancellationToken cancellationToken = default)
    {
        var baseResult = await base.StepAsync(cancellationToken);

        if (baseResult is null)
            return null;

        return new AdaptationTickResult
        {
            Percept = baseResult.Percept,
            Action = baseResult.Action,
            Result = baseResult.Result,
            Experience = baseResult.Experience,
            MetricsBefore = _metricsBefore,
            MetricsAfter = _metricsAfter,
            SettingsChanged = baseResult.Result?.Success == true
        };
    }

    /// <summary>
    /// SENSE PHASE: Analyzes performance metrics and captures pre-adaptation state.
    /// </summary>
    protected override async Task<AdaptationPercept?> SenseAsync(CancellationToken cancellationToken)
    {
        var percept = await base.SenseAsync(cancellationToken);

        if (percept is not null)
        {
            // Capture metrics before adaptation
            _metricsBefore = CalculateMetrics(percept);
        }
        else
        {
            _metricsBefore = new Dictionary<string, double>();
        }

        return percept;
    }

    /// <summary>
    /// ACT PHASE: Applies adaptation and captures post-adaptation state.
    /// </summary>
    protected override async Task<AdaptationResult?> ActAsync(AdaptationAction? action, CancellationToken cancellationToken)
    {
        var result = await base.ActAsync(action, cancellationToken);

        if (result is not null && result.Success)
        {
            // Capture metrics after adaptation
            // Note: In practice, would need to re-sense the environment
            _metricsAfter = new Dictionary<string, double>(_metricsBefore);
        }
        else
        {
            _metricsAfter = new Dictionary<string, double>();
        }

        return result;
    }

    /// <summary>
    /// Calculates performance metrics from perception data.
    /// </summary>
    private Dictionary<string, double> CalculateMetrics(AdaptationPercept percept)
    {
        var metrics = new Dictionary<string, double>
        {
            ["ExperienceCount"] = percept.RecentExperiences.Count,
            ["ActiveTasks"] = percept.Statistics.ActiveTaskCount,
            ["PendingTasks"] = percept.Statistics.PendingTaskCount,
            ["OverdueTasks"] = percept.Statistics.OverdueTaskCount,
            ["EscalatedTasks"] = percept.Statistics.EscalatedTaskCount,
            ["MaxActiveTasks"] = percept.CurrentSettings.MaxActiveTasks,
            ["ConfidenceThreshold"] = percept.CurrentSettings.MinimumConfidenceThreshold,
            ["EscalationThreshold"] = percept.CurrentSettings.EscalationThresholdHours
        };

        if (percept.RecentExperiences.Count > 0)
        {
            metrics["AvgUrgency"] = percept.RecentExperiences.Average(e => e.AverageUrgencyScore);
            metrics["AvgSuccessRate"] = percept.RecentExperiences
                .Where(e => e.ActionsExecuted > 0)
                .Average(e => (double)e.SuccessfulActions / e.ActionsExecuted);
        }

        return metrics;
    }

    /// <summary>
    /// Creates the tick result with metrics included.
    /// </summary>
    protected override TickResult<AdaptationPercept, AdaptationAction, AdaptationResult, AdaptationExperience> CreateTickResult(
        AdaptationPercept? percept,
        AdaptationAction? action,
        AdaptationResult? result,
        AdaptationExperience? experience)
    {
        return new AdaptationTickResult
        {
            Percept = percept,
            Action = action,
            Result = result,
            Experience = experience,
            MetricsBefore = _metricsBefore,
            MetricsAfter = _metricsAfter,
            SettingsChanged = result?.Success == true
        };
    }

    // ============================================================================
    // INNER CLASSES: Component Implementations
    // ============================================================================

    /// <summary>
    /// Perception source for adaptation (SENSE phase).
    /// Analyzes recent performance experiences.
    /// </summary>
    private sealed class AdaptationPerceptionSource : IPerceptionSource<AdaptationPercept>
    {
        private readonly LearningService _learningService;
        private readonly TimeSpan _analysisWindow;

        public AdaptationPerceptionSource(
            LearningService learningService,
            TimeSpan analysisWindow)
        {
            _learningService = learningService;
            _analysisWindow = analysisWindow;
        }

        /// <summary>
        /// SENSE: Gather performance data and system statistics.
        /// Returns null if insufficient data for analysis (no-work scenario).
        /// </summary>
        public async Task<AdaptationPercept?> SenseAsync(CancellationToken cancellationToken = default)
        {
            return await _learningService.AnalyzePerformanceAsync(_analysisWindow, cancellationToken);
        }
    }

    /// <summary>
    /// Policy for adaptation decisions (THINK phase).
    /// Determines if and how to adapt system settings.
    /// </summary>
    private sealed class AdaptationPolicy : IPolicy<AdaptationPercept, AdaptationAction>
    {
        private readonly LearningService _learningService;

        public AdaptationPolicy(LearningService learningService)
        {
            _learningService = learningService;
        }

        /// <summary>
        /// THINK: Analyze performance trends and decide on adaptation strategy.
        /// Returns null if no adaptation is needed (no-work scenario).
        /// </summary>
        public async Task<AdaptationAction?> DecideAsync(
            AdaptationPercept? percept,
            CancellationToken cancellationToken = default)
        {
            if (percept is null)
                return null;

            return await _learningService.DecideAdaptationAsync(percept, cancellationToken);
        }
    }

    /// <summary>
    /// Actuator for adaptation actions (ACT phase).
    /// Applies setting changes to the system.
    /// </summary>
    private sealed class AdaptationActuator : IActuator<AdaptationAction, AdaptationResult>
    {
        private readonly LearningService _learningService;

        public AdaptationActuator(LearningService learningService)
        {
            _learningService = learningService;
        }

        /// <summary>
        /// ACT: Apply the adaptation by updating system settings.
        /// Returns null if no adaptation to apply (no-work scenario).
        /// </summary>
        public async Task<AdaptationResult?> ExecuteAsync(
            AdaptationAction? action,
            CancellationToken cancellationToken = default)
        {
            if (action is null)
                return null;

            return await _learningService.ApplyAdaptationAsync(action, cancellationToken);
        }
    }

    /// <summary>
    /// Learning component for adaptation (LEARN phase).
    /// Records adaptation experiences for future improvement.
    /// </summary>
    private sealed class AdaptationLearningComponent : ILearningComponent<AdaptationExperience>
    {
        private readonly LearningService _learningService;

        public AdaptationLearningComponent(LearningService learningService)
        {
            _learningService = learningService;
        }

        /// <summary>
        /// LEARN: Record the outcome of the adaptation attempt.
        /// Returns null if no learning occurred (no-work scenario).
        /// </summary>
        public async Task<AdaptationExperience?> LearnAsync<TPercept, TAction, TResult>(
            TPercept? percept,
            TAction? action,
            TResult? result,
            CancellationToken cancellationToken = default)
        {
            // Type-safe casting
            var adaptationPercept = percept as AdaptationPercept;
            var adaptationAction = action as AdaptationAction;
            var adaptationResult = result as AdaptationResult;

            return await _learningService.RecordAdaptationExperienceAsync(
                adaptationPercept,
                adaptationAction,
                adaptationResult,
                cancellationToken);
        }
    }
}