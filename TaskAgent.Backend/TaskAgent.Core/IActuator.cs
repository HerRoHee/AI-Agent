using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAgent.Core;

/// <summary>
/// Represents an actuator that executes actions in the agent's Act phase.
/// Responsible for performing actions and producing results.
/// </summary>
/// <typeparam name="TAction">The type of action to be executed.</typeparam>
/// <typeparam name="TResult">The type of result produced by action execution.</typeparam>
public interface IActuator<in TAction, TResult>
{
    /// <summary>
    /// Asynchronously executes the given action and returns the result.
    /// Returns null if the action cannot be executed or produces no result (no-work scenario).
    /// </summary>
    /// <param name="action">The action to execute. May be null.</param>
    /// <param name="cancellationToken">Token to cancel the execution operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the execution result,
    /// or null if no result was produced.
    /// </returns>
    Task<TResult?> ExecuteAsync(TAction? action, CancellationToken cancellationToken = default);
}