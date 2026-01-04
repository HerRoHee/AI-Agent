using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskAgent.Tasks.Application.Interfaces;
using TaskAgent.Tasks.Infrastructure.Abstractions;
using TaskAgent.Tasks.Infrastructure.Persistence;
using TaskAgent.Tasks.Infrastructure.Repositories;
using TaskAgent.Tasks.Infrastructure.Seeder;

namespace TaskAgent.Tasks.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure services with dependency injection.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Adds infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTaskAgentInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Register DbContext
        services.AddDbContext<TaskAgentDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            }));

        // Register repositories
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();

        // Register seeder
        services.AddScoped<DatabaseSeeder>();

        // Register system clock
        services.AddSingleton<ISystemClock, SystemClock>();

        return services;
    }

    /// <summary>
    /// Adds infrastructure services with in-memory database (for testing).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="databaseName">The in-memory database name.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTaskAgentInfrastructureInMemory(
        this IServiceCollection services,
        string databaseName = "TaskAgentTestDb")
    {
        // Register in-memory DbContext
        services.AddDbContext<TaskAgentDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        // Register repositories
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();

        // Register seeder
        services.AddScoped<DatabaseSeeder>();

        // Register system clock (can be overridden with FakeSystemClock in tests)
        services.AddSingleton<ISystemClock, SystemClock>();

        return services;
    }
}