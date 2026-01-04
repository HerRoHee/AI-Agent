using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAgent.Tasks.Domain.Entities;

namespace TaskAgent.Tasks.Application.Interfaces;

/// <summary>
/// Repository interface for system settings persistence operations.
/// Implementations are provided by the Infrastructure layer.
/// </summary>
public interface ISettingsRepository
{
    /// <summary>
    /// Retrieves the current system settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The system settings, or null if not initialized.</returns>
    Task<SystemSettings?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the system settings.
    /// Creates new settings if none exist, otherwise updates existing settings.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(SystemSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures system settings exist, creating default settings if necessary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The existing or newly created system settings.</returns>
    Task<SystemSettings> EnsureExistsAsync(CancellationToken cancellationToken = default);
}