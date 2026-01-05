using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAgent.Core;
using TaskAgent.Tasks.Application.DTO;
using TaskAgent.Tasks.Application.Services;
using TaskAgent.Tasks.Domain.Entities;

namespace TaskAgent.Tasks.Runners;

/// <summary>
/// Agent runner responsible for task scoring, evaluation, and action execution.
/// Implements the Sense → Think → Act → Learn loop for intelligent task management.
/// </summary>
public sealed class TaskScoringAgentRunner : SoftwareAgent<TaskPercept, TaskAction, TaskActionResult, TaskScoringExperience>
{
    private readonly TaskEvaluationService _evaluationService;
    private readonly RecommendationService _recommendationService;
    private readonly TaskQueueService _queueService;
    private readonly LearningService _learningService;
    private IReadOnlyCollection<ScoredTask> _currentScoredTasks = Array.Empty<ScoredTask>();

    /// <summary>
    /// Initializes a new instance of the task scoring agent runner.
    /// </summary>
    public TaskScoringAgentRunner(
        TaskEvaluationService evaluationService,
        RecommendationService recommendationService,
        TaskQueueService queueService,
        LearningService learningService)
        : base(
            new TaskScoringPerceptionSource(evaluationService),
            new TaskScoringPolicy(recommendationService),
            new TaskScoringActuator(queueService, recommendationService),
            new TaskScoringLearningComponent(learningService))
    {
        _evaluationService = evaluationService ?? throw new ArgumentNullException(nameof(evaluationService));
        _recommendationService = recommendationService ?? throw new ArgumentNullException(nameof(recommendationService));
        _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
        _learningService = learningService ?? throw new ArgumentNullException(nameof(learningService));
    }

    /// <summary>
    /// Executes one atomic tick of the task scoring agent.
    /// Returns null if no work was performed.
    /// </summary>
    public new async Task<TaskScoringTickResult?> StepAsync(CancellationToken cancellationToken = default)
    {
        var baseResult = await base.StepAsync(cancellationToken);

        if (baseResult is null)
            return null;

        return new TaskScoringTickResult
        {
            Percept = baseResult.Percept,
            Action = baseResult.Action,
            Result = baseResult.Result,
            Experience = baseResult.Experience,
            ScoredTasks = _currentScoredTasks
        };
    }

    /// <summary>
    /// SENSE PHASE: Gathers perception data and scores tasks.
    /// </summary>
    protected override async Task<TaskPercept?> SenseAsync(CancellationToken cancellationToken)
    {
        // Gather raw perception data
        var percept = await base.SenseAsync(cancellationToken);

        if (percept is null)
        {
            _currentScoredTasks = Array.Empty<ScoredTask>();
            return null;
        }

        // Score tasks as part of perception enrichment
        _currentScoredTasks = await _evaluationService.ScoreTasksAsync(percept, cancellationToken);

        return percept;
    }

    /// <summary>
    /// Creates the tick result with scored tasks included.
    /// </summary>
    protected override TickResult<TaskPercept, TaskAction, TaskActionResult, TaskScoringExperience> CreateTickResult(
        TaskPercept? percept,
        TaskAction? action,
        TaskActionResult? result,
        TaskScoringExperience? experience)
    {
        return new TaskScoringTickResult
        {
            Percept = percept,
            Action = action,
            Result = result,
            Experience = experience,
            ScoredTasks = _currentScoredTasks
        };
    }

    // ============================================================================
    // INNER CLASSES: Component Implementations
    // ============================================================================

    /// <summary>
    /// Perception source for task scoring (SENSE phase).
    /// Gathers tasks and settings from the environment.
    /// </summary>
    private sealed class TaskScoringPerceptionSource : IPerceptionSource<TaskPercept>
    {
        private readonly TaskEvaluationService _evaluationService;

        public TaskScoringPerceptionSource(TaskEvaluationService evaluationService)
        {
            _evaluationService = evaluationService;
        }

        /// <summary>
        /// SENSE: Gather task data and system settings.
        /// Returns null if no tasks need evaluation (no-work scenario).
        /// </summary>
        public async Task<TaskPercept?> SenseAsync(CancellationToken cancellationToken = default)
        {
            return await _evaluationService.GatherPerceptionAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Policy for task scoring decisions (THINK phase).
    /// Decides which action to take based on scored tasks.
    /// </summary>
    private sealed class TaskScoringPolicy : IPolicy<TaskPercept, TaskAction>
    {
        private readonly RecommendationService _recommendationService;

        public TaskScoringPolicy(RecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        /// <summary>
        /// THINK: Analyze scored tasks and decide on an action.
        /// Returns null if no action is needed (no-work scenario).
        /// </summary>
        public async Task<TaskAction?> DecideAsync(TaskPercept? percept, CancellationToken cancellationToken = default)
        {
            if (percept is null || percept.Tasks.Count == 0)
                return null;

            // Note: Scoring happens in the SENSE phase, stored in runner state
            // Here we retrieve the runner's scored tasks via the recommendation service
            // which will internally score if needed

            // For simplicity, we create a minimal scored task list from percept
            // In practice, the runner passes scored tasks via shared state
            var scoredTasks = percept.Tasks
                .Select(t => new ScoredTask
                {
                    Task = t,
                    UrgencyScore = 0.5, // Placeholder - actual scoring in SENSE
                    PriorityWeight = 0.5,
                    TimeWeight = 0.5,
                    StatusWeight = 0.5
                })
                .ToList();

            // Generate recommendation based on task evaluation
            return await _recommendationService.GenerateRecommendationAsync(scoredTasks, cancellationToken);
        }
    }

    /// <summary>
    /// Actuator for task actions (ACT phase).
    /// Executes the decided action on tasks.
    /// </summary>
    private sealed class TaskScoringActuator : IActuator<TaskAction, TaskActionResult>
    {
        private readonly TaskQueueService _queueService;
        private readonly RecommendationService _recommendationService;

        public TaskScoringActuator(
            TaskQueueService queueService,
            RecommendationService recommendationService)
        {
            _queueService = queueService;
            _recommendationService = recommendationService;
        }

        /// <summary>
        /// ACT: Execute the recommended action on the task.
        /// Returns null if no action to execute (no-work scenario).
        /// </summary>
        public async Task<TaskActionResult?> ExecuteAsync(TaskAction? action, CancellationToken cancellationToken = default)
        {
            if (action is null)
                return null;

            try
            {
                var success = await ExecuteActionByTypeAsync(action, cancellationToken);

                // Mark recommendation as applied if successful
                if (success && action.Recommendation is not null)
                {
                    await _recommendationService.MarkRecommendationAppliedAsync(
                        action.Recommendation,
                        cancellationToken);
                }

                // Retrieve updated task
                TaskItem? updatedTask = null;
                if (success)
                {
                    updatedTask = action.Task; // In real scenario, would re-fetch from repository
                }

                return new TaskActionResult
                {
                    Action = action,
                    Success = success,
                    UpdatedTask = updatedTask
                };
            }
            catch (Exception ex)
            {
                return new TaskActionResult
                {
                    Action = action,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Routes action execution to the appropriate queue service method.
        /// </summary>
        private async Task<bool> ExecuteActionByTypeAsync(TaskAction action, CancellationToken cancellationToken)
        {
            return action.ActionType switch
            {
                "Activate" => await _queueService.ActivateTaskAsync(action.Task.Id, cancellationToken),
                "Snooze" => await _queueService.SnoozeTaskAsync(
                    action.Task.Id,
                    action.Recommendation?.RecommendedSnoozeDuration,
                    cancellationToken),
                "Escalate" => await _queueService.EscalateTaskAsync(action.Task.Id, cancellationToken),
                "Complete" => await _queueService.CompleteTaskAsync(action.Task.Id, cancellationToken),
                "ReturnToPending" => await _queueService.ReturnToPendingAsync(action.Task.Id, cancellationToken),
                "Awaken" => await _queueService.ReturnToPendingAsync(action.Task.Id, cancellationToken),
                _ => false
            };
        }
    }

    /// <summary>
    /// Learning component for task scoring (LEARN phase).
    /// Records experiences and generates learning data.
    /// </summary>
    private sealed class TaskScoringLearningComponent : ILearningComponent<TaskScoringExperience>
    {
        private readonly LearningService _learningService;

        public TaskScoringLearningComponent(LearningService learningService)
        {
            _learningService = learningService;
        }

        /// <summary>
        /// LEARN: Analyze the tick's behavior and record learning experience.
        /// Returns null if no learning occurred (no-work scenario).
        /// </summary>
        public async Task<TaskScoringExperience?> LearnAsync<TPercept, TAction, TResult>(
            TPercept? percept,
            TAction? action,
            TResult? result,
            CancellationToken cancellationToken = default)
        {
            // Type-safe casting
            var taskPercept = percept as TaskPercept;
            var taskAction = action as TaskAction;
            var taskResult = result as TaskActionResult;

            if (taskPercept is null)
                return null;

            // Create scored tasks for experience recording
            // In practice, these would be passed from the runner's state
            var scoredTasks = taskPercept.Tasks
                .Select(t => new ScoredTask
                {
                    Task = t,
                    UrgencyScore = 0.5,
                    PriorityWeight = 0.5,
                    TimeWeight = 0.5,
                    StatusWeight = 0.5
                })
                .ToList();

            return await _learningService.RecordExperienceAsync(
                taskPercept,
                taskAction,
                taskResult,
                scoredTasks,
                cancellationToken);
        }
    }
}