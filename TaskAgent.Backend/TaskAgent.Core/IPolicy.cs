using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAgent.Core;

/// <summary>
/// Represents a decision-making policy for the agent's Think phase.
/// Responsible for mapping perceptions to actions.
/// </summary>
/// <typeparam name="TPercept">The type of perception data received as input.</typeparam>
/// <typeparam name="TAction">The type of action to be decided.</typeparam>
public interface IPolicy<in TPercept, TAction>
{
    /// <summary>
    /// Asynchronously decides on an action based on the given perception.
    /// Returns null if no action should be taken (no-work scenario).
    /// </summary>
    /// <param name="percept">The perception data to base the decision on. May be null.</param>
    /// <param name="cancellationToken">Token to cancel the decision-making operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the decided action,
    /// or null if no action should be taken.
    /// </returns>
    Task<TAction?> DecideAsync(TPercept? percept, CancellationToken cancellationToken = default);
}