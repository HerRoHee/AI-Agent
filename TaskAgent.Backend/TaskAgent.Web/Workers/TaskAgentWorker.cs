using TaskAgent.Tasks.Runners;
using TaskAgent.Tasks.Application.Services;

namespace TaskAgent.Web.Workers;

/// <summary>
/// Background worker that continuously runs agent tick loops.
/// Orchestrates both TaskScoringAgent and TaskAdaptationAgent.
/// </summary>
public sealed class TaskAgentWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TaskAgentWorker> _logger;
    private readonly TimeSpan _scoringInterval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _adaptationInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _noWorkDelay = TimeSpan.FromSeconds(10);

    public TaskAgentWorker(
        IServiceProvider serviceProvider,
        ILogger<TaskAgentWorker> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TaskAgentWorker started at {Time}", DateTimeOffset.UtcNow);

        // Run both agent loops concurrently
        await Task.WhenAll(
            RunScoringAgentLoopAsync(stoppingToken),
            RunAdaptationAgentLoopAsync(stoppingToken)
        );

        _logger.LogInformation("TaskAgentWorker stopped at {Time}", DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Continuously runs the task scoring agent loop.
    /// Sense → Think → Act → Learn for task evaluation and action.
    /// </summary>
    private async Task RunScoringAgentLoopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Task Scoring Agent loop started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Create a new scope for each tick to ensure fresh dependencies
                using var scope = _serviceProvider.CreateScope();

                // Resolve dependencies for this tick
                var evaluationService = scope.ServiceProvider.GetRequiredService<TaskEvaluationService>();
                var recommendationService = scope.ServiceProvider.GetRequiredService<RecommendationService>();
                var queueService = scope.ServiceProvider.GetRequiredService<TaskQueueService>();
                var learningService = scope.ServiceProvider.GetRequiredService<LearningService>();

                // Create runner instance
                var runner = new TaskScoringAgentRunner(
                    evaluationService,
                    recommendationService,
                    queueService,
                    learningService);

                // Execute one atomic tick: Sense → Think → Act → Learn
                var result = await runner.StepAsync(stoppingToken);

                if (result is not null)
                {
                    // Work was performed - log and emit result
                    _logger.LogInformation(
                        "Task Scoring Tick completed: {TasksEvaluated} tasks evaluated, Action: {Action}, Success: {Success}",
                        result.TasksEvaluated,
                        result.Action?.ActionType ?? "None",
                        result.Result?.Success ?? false);

                    // Immediate next tick if work was done
                    await Task.Delay(_scoringInterval, stoppingToken);
                }
                else
                {
                    // No work available - back off longer
                    _logger.LogDebug("Task Scoring: No work available, backing off");
                    await Task.Delay(_noWorkDelay, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Task Scoring Agent loop");
                await Task.Delay(_noWorkDelay, stoppingToken);
            }
        }

        _logger.LogInformation("Task Scoring Agent loop stopped");
    }

    /// <summary>
    /// Continuously runs the adaptation agent loop.
    /// Sense → Think → Act → Learn for system self-optimization.
    /// </summary>
    private async Task RunAdaptationAgentLoopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Task Adaptation Agent loop started");

        // Wait before first adaptation to gather initial experiences
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Create a new scope for each tick
                using var scope = _serviceProvider.CreateScope();

                // Resolve dependencies for this tick
                var learningService = scope.ServiceProvider.GetRequiredService<LearningService>();

                // Create runner instance with 1-hour analysis window
                var runner = new TaskAdaptationAgentRunner(
                    learningService,
                    analysisWindow: TimeSpan.FromHours(1));

                // Execute one atomic tick: Sense → Think → Act → Learn
                var result = await runner.StepAsync(stoppingToken);

                if (result is not null)
                {
                    // Work was performed - log and emit result
                    _logger.LogInformation(
                        "Adaptation Tick completed: Settings changed: {Changed}, Type: {Type}, Success: {Success}",
                        result.SettingsChanged,
                        result.Action?.AdaptationType ?? "None",
                        result.Result?.Success ?? false);

                    if (result.SettingsChanged)
                    {
                        _logger.LogInformation(
                            "System settings adapted: {Reasoning}",
                            result.Action?.Reasoning ?? "Unknown");
                    }

                    // Standard interval after work
                    await Task.Delay(_adaptationInterval, stoppingToken);
                }
                else
                {
                    // No adaptation needed - back off longer
                    _logger.LogDebug("Adaptation: No adaptation needed, backing off");
                    await Task.Delay(_adaptationInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Task Adaptation Agent loop");
                await Task.Delay(_noWorkDelay, stoppingToken);
            }
        }

        _logger.LogInformation("Task Adaptation Agent loop stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TaskAgentWorker stopping...");
        await base.StopAsync(cancellationToken);
    }
}