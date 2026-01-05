using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskAgent.Tasks.Domain.Entities;
using TaskAgent.Tasks.Domain.Enums;
using TaskAgent.Tasks.Infrastructure.Persistence;

namespace TaskAgent.Tasks.Infrastructure.Seeder;

/// <summary>
/// Provides database seeding functionality for initial data and testing.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly TaskAgentDbContext _context;

    public DatabaseSeeder(TaskAgentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Seeds the database with initial system settings and sample tasks.
    /// </summary>
    /// <param name="includeSampleTasks">Whether to include sample task data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SeedAsync(bool includeSampleTasks = false, CancellationToken cancellationToken = default)
    {
        // Ensure database is created
        await _context.Database.EnsureCreatedAsync(cancellationToken);

        // Seed system settings if not present
        await SeedSystemSettingsAsync(cancellationToken);

        // Optionally seed sample tasks
        if (includeSampleTasks)
        {
            await SeedSampleTasksAsync(cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Seeds default system settings if none exist.
    /// </summary>
    private async Task SeedSystemSettingsAsync(CancellationToken cancellationToken)
    {
        var hasSettings = await _context.Settings.AnyAsync(cancellationToken);
        if (hasSettings)
            return;

        var defaultSettings = new SystemSettings();
        await _context.Settings.AddAsync(defaultSettings, cancellationToken);
    }

    /// <summary>
    /// Seeds sample tasks for testing and demonstration purposes.
    /// </summary>
    private async Task SeedSampleTasksAsync(CancellationToken cancellationToken)
    {
        var hasTasks = await _context.Tasks.AnyAsync(cancellationToken);
        if (hasTasks)
            return;

        var now = DateTimeOffset.UtcNow;

        var sampleTasks = new List<TaskItem>
        {
            // Critical task - overdue
            new TaskItem(
                "Fix production database connection issue",
                "Users are unable to access the application due to connection timeouts. Investigate and resolve immediately.",
                TaskPriority.Critical,
                now.AddHours(-2)
            ),

            // High priority - due soon
            new TaskItem(
                "Deploy security patch for authentication service",
                "Critical security vulnerability identified in authentication module. Patch must be deployed before end of day.",
                TaskPriority.High,
                now.AddHours(6)
            ),

            // High priority - active work
            new TaskItem(
                "Complete Q4 performance review documentation",
                "Prepare performance review documents for team members. Include metrics and improvement recommendations.",
                TaskPriority.High,
                now.AddDays(3)
            ),

            // Medium priority - upcoming
            new TaskItem(
                "Refactor legacy payment processing module",
                "Technical debt cleanup: modernize payment processing code to improve maintainability and performance.",
                TaskPriority.Medium,
                now.AddDays(7)
            ),

            // Medium priority - no due date
            new TaskItem(
                "Update API documentation with new endpoints",
                "Document the newly added REST API endpoints and update the developer portal with examples.",
                TaskPriority.Medium,
                null
            ),

            // Low priority - future work
            new TaskItem(
                "Research machine learning frameworks for recommendation engine",
                "Evaluate TensorFlow, PyTorch, and scikit-learn for potential use in product recommendation system.",
                TaskPriority.Low,
                now.AddDays(14)
            ),

            // Low priority - backlog
            new TaskItem(
                "Optimize image compression in media pipeline",
                "Investigate better compression algorithms to reduce storage costs while maintaining quality.",
                TaskPriority.Low,
                null
            )
        };

        // Mark one task as active
        sampleTasks[2].Activate();

        // Snooze one task
        sampleTasks[4].Snooze(now.AddHours(8));

        await _context.Tasks.AddRangeAsync(sampleTasks, cancellationToken);
    }

    /// <summary>
    /// Clears all data from the database (useful for testing).
    /// </summary>
    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        _context.Recommendations.RemoveRange(_context.Recommendations);
        _context.Tasks.RemoveRange(_context.Tasks);
        _context.Settings.RemoveRange(_context.Settings);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Resets the database by clearing all data and re-seeding.
    /// </summary>
    public async Task ResetDatabaseAsync(bool includeSampleTasks = false, CancellationToken cancellationToken = default)
    {
        await ClearAllDataAsync(cancellationToken);
        await SeedAsync(includeSampleTasks, cancellationToken);
    }
}