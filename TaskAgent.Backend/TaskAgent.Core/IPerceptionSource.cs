using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAgent.Core;

/// <summary>
/// Represents a source of perceptual data for the agent's Sense phase.
/// Responsible for gathering observations from the environment.
/// </summary>
/// <typeparam name="TPercept">The type of perception data to be gathered.</typeparam>
public interface IPerceptionSource<TPercept>
{
    /// <summary>
    /// Asynchronously senses the environment and returns perceptual data.
    /// Returns null if no new perception is available (no-work scenario).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the sensing operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the perceived data,
    /// or null if no perception is available.
    /// </returns>
    Task<TPercept?> SenseAsync(CancellationToken cancellationToken = default);
}