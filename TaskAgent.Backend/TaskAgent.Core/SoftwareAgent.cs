using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAgent.Core;

/// <summary>
/// Abstract base class representing a software agent that follows the Sense → Think → Act → Learn loop.
/// Each tick executes one atomic cycle of the agent's behavior.
/// </summary>
/// <typeparam name="TPercept">Type of perception data sensed from the environment.</typeparam>
/// <typeparam name="TAction">Type of action decided by the agent's policy.</typeparam>
/// <typeparam name="TResult">Type of result produced by action execution.</typeparam>
/// <typeparam name="TExperience">Type of learning experience generated.</typeparam>
public abstract class SoftwareAgent<TPercept, TAction, TResult, TExperience>
{
    /// <summary>
    /// Gets the perception source for the Sense phase.
    /// </summary>
    protected IPerceptionSource<TPercept> PerceptionSource { get; }

    /// <summary>
    /// Gets the policy for the Think phase.
    /// </summary>
    protected IPolicy<TPercept, TAction> Policy { get; }

    /// <summary>
    /// Gets the actuator for the Act phase.
    /// </summary>
    protected IActuator<TAction, TResult> Actuator { get; }

    /// <summary>
    /// Gets the learning component for the Learn phase.
    /// </summary>
    protected ILearningComponent<TExperience> LearningComponent { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftwareAgent{TPercept, TAction, TResult, TExperience}"/> class.
    /// </summary>
    /// <param name="perceptionSource">The perception source for sensing.</param>
    /// <param name="policy">The policy for decision-making.</param>
    /// <param name="actuator">The actuator for action execution.</param>
    /// <param name="learningComponent">The learning component for experience generation.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    protected SoftwareAgent(
        IPerceptionSource<TPercept> perceptionSource,
        IPolicy<TPercept, TAction> policy,
        IActuator<TAction, TResult> actuator,
        ILearningComponent<TExperience> learningComponent)
    {
        PerceptionSource = perceptionSource ?? throw new ArgumentNullException(nameof(perceptionSource));
        Policy = policy ?? throw new ArgumentNullException(nameof(policy));
        Actuator = actuator ?? throw new ArgumentNullException(nameof(actuator));
        LearningComponent = learningComponent ?? throw new ArgumentNullException(nameof(learningComponent));
    }

    /// <summary>
    /// Executes one atomic tick of the agent's Sense → Think → Act → Learn loop.
    /// Returns null if no work was performed during this tick.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the tick operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the tick result,
    /// or null if no work was performed.
    /// </returns>
    public async Task<TickResult<TPercept, TAction, TResult, TExperience>?> StepAsync(
        CancellationToken cancellationToken = default)
    {
        // Phase 1: Sense
        var percept = await SenseAsync(cancellationToken);

        // Phase 2: Think
        var action = await ThinkAsync(percept, cancellationToken);

        // Phase 3: Act
        var result = await ActAsync(action, cancellationToken);

        // Phase 4: Learn
        var experience = await LearnAsync(percept, action, result, cancellationToken);

        // Check if any work was performed
        if (percept is null && action is null && result is null && experience is null)
        {
            return null;
        }

        // Create and return the tick result
        return CreateTickResult(percept, action, result, experience);
    }

    /// <summary>
    /// Executes the Sense phase of the agent loop.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The perceived data, or null if no perception is available.</returns>
    protected virtual async Task<TPercept?> SenseAsync(CancellationToken cancellationToken)
    {
        return await PerceptionSource.SenseAsync(cancellationToken);
    }

    /// <summary>
    /// Executes the Think phase of the agent loop.
    /// </summary>
    /// <param name="percept">The perception data from the Sense phase.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The decided action, or null if no action should be taken.</returns>
    protected virtual async Task<TAction?> ThinkAsync(TPercept? percept, CancellationToken cancellationToken)
    {
        return await Policy.DecideAsync(percept, cancellationToken);
    }

    /// <summary>
    /// Executes the Act phase of the agent loop.
    /// </summary>
    /// <param name="action">The action decided in the Think phase.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The execution result, or null if no result was produced.</returns>
    protected virtual async Task<TResult?> ActAsync(TAction? action, CancellationToken cancellationToken)
    {
        return await Actuator.ExecuteAsync(action, cancellationToken);
    }

    /// <summary>
    /// Executes the Learn phase of the agent loop.
    /// </summary>
    /// <param name="percept">The perception data from the Sense phase.</param>
    /// <param name="action">The action decided in the Think phase.</param>
    /// <param name="result">The result from the Act phase.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The learning experience, or null if no learning occurred.</returns>
    protected virtual async Task<TExperience?> LearnAsync(
        TPercept? percept,
        TAction? action,
        TResult? result,
        CancellationToken cancellationToken)
    {
        return await LearningComponent.LearnAsync(percept, action, result, cancellationToken);
    }

    /// <summary>
    /// Creates a tick result object from the phase outputs.
    /// Derived classes can override this to provide custom tick result types.
    /// </summary>
    /// <param name="percept">The perception data.</param>
    /// <param name="action">The decided action.</param>
    /// <param name="result">The execution result.</param>
    /// <param name="experience">The learning experience.</param>
    /// <returns>A tick result object encapsulating the agent's work.</returns>
    protected abstract TickResult<TPercept, TAction, TResult, TExperience> CreateTickResult(
        TPercept? percept,
        TAction? action,
        TResult? result,
        TExperience? experience);
}