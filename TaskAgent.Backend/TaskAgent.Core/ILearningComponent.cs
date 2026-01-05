using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAgent.Core;

/// <summary>
/// Represents a learning component for the agent's Learn phase.
/// Responsible for generating experiences from agent behavior and updating internal state.
/// </summary>
/// <typeparam name="TExperience">The type of learning experience to be generated.</typeparam>
public interface ILearningComponent<TExperience>
{
    /// <summary>
    /// Asynchronously processes the agent's tick and generates a learning experience.
    /// Returns null if no learning occurred (no-work scenario).
    /// </summary>
    /// <typeparam name="TPercept">The type of perception data.</typeparam>
    /// <typeparam name="TAction">The type of action taken.</typeparam>
    /// <typeparam name="TResult">The type of result produced.</typeparam>
    /// <param name="percept">The perception data from the Sense phase. May be null.</param>
    /// <param name="action">The action decided in the Think phase. May be null.</param>
    /// <param name="result">The result from the Act phase. May be null.</param>
    /// <param name="cancellationToken">Token to cancel the learning operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the learning experience,
    /// or null if no learning occurred.
    /// </returns>
    Task<TExperience?> LearnAsync<TPercept, TAction, TResult>(
        TPercept? percept,
        TAction? action,
        TResult? result,
        CancellationToken cancellationToken = default);
}