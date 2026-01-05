using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskAgent.Tasks.Application.Interfaces;
using TaskAgent.Tasks.Domain.Entities;
using TaskAgent.Tasks.Infrastructure.Persistence;

namespace TaskAgent.Tasks.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of the settings repository.
/// Manages system settings persistence with singleton pattern.
/// </summary>
public sealed class SettingsRepository : ISettingsRepository
{
    private readonly TaskAgentDbContext _context;

    public SettingsRepository(TaskAgentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<SystemSettings?> GetAsync(CancellationToken cancellationToken = default)
    {
        // System settings follow a singleton pattern - return the first (and should be only) settings
        return await _context.Settings
            .AsNoTracking()
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveAsync(SystemSettings settings, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Settings
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            // Create new settings
            await _context.Settings.AddAsync(settings, cancellationToken);
        }
        else
        {
            // Update existing settings - replace with new settings object
            // This ensures domain-generated IDs are respected
            _context.Settings.Remove(existing);
            await _context.Settings.AddAsync(settings, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SystemSettings> EnsureExistsAsync(CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync(cancellationToken);

        if (existing is not null)
            return existing;

        // Create default settings if none exist
        var defaultSettings = new SystemSettings();
        await SaveAsync(defaultSettings, cancellationToken);

        return defaultSettings;
    }
}