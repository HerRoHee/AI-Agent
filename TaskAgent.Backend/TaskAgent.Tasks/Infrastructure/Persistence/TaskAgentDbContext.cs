using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskAgent.Tasks.Domain.Entities;
using TaskAgent.Tasks.Infrastructure.Persistence.Configurations;

namespace TaskAgent.Tasks.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for the task agent system.
/// Manages task items, recommendations, and system settings persistence.
/// </summary>
public sealed class TaskAgentDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the task items in the database.
    /// </summary>
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    /// <summary>
    /// Gets or sets the task recommendations in the database.
    /// </summary>
    public DbSet<TaskRecommendation> Recommendations => Set<TaskRecommendation>();

    /// <summary>
    /// Gets or sets the system settings in the database.
    /// </summary>
    public DbSet<SystemSettings> Settings => Set<SystemSettings>();

    /// <summary>
    /// Initializes a new instance of the TaskAgentDbContext.
    /// </summary>
    public TaskAgentDbContext(DbContextOptions<TaskAgentDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configures the entity models and relationships.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new TaskItemConfiguration());
        modelBuilder.ApplyConfiguration(new TaskRecommendationConfiguration());
        modelBuilder.ApplyConfiguration(new SystemSettingsConfiguration());
    }
}