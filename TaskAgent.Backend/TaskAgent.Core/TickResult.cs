using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAgent.Core;

/// <summary>
/// Base class for representing the result of a single agent tick.
/// Null or derived instances indicate no work was performed.
/// </summary>
/// <typeparam name="TPercept">Type of perception data sensed.</typeparam>
/// <typeparam name="TAction">Type of action decided.</typeparam>
/// <typeparam name="TResult">Type of result from action execution.</typeparam>
/// <typeparam name="TExperience">Type of learning experience generated.</typeparam>
public abstract class TickResult<TPercept, TAction, TResult, TExperience>
{
    /// <summary>
    /// Gets the timestamp when this tick was executed.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the perception data gathered during the Sense phase.
    /// Null if no perception was gathered.
    /// </summary>
    public TPercept? Percept { get; init; }

    /// <summary>
    /// Gets the action decided during the Think phase.
    /// Null if no action was decided.
    /// </summary>
    public TAction? Action { get; init; }

    /// <summary>
    /// Gets the result of executing the action during the Act phase.
    /// Null if no action was executed or no result was produced.
    /// </summary>
    public TResult? Result { get; init; }

    /// <summary>
    /// Gets the learning experience generated during the Learn phase.
    /// Null if no learning occurred.
    /// </summary>
    public TExperience? Experience { get; init; }

    /// <summary>
    /// Indicates whether this tick performed any meaningful work.
    /// </summary>
    public bool HasWork => Percept is not null || Action is not null || Result is not null || Experience is not null;
}