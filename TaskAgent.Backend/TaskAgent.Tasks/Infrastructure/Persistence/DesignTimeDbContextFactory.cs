using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskAgent.Tasks.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TaskAgentDbContext>
{
    public TaskAgentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TaskAgentDbContext>();

        // Use LocalDB for design-time migrations
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=TaskAgentDb;Trusted_Connection=true;MultipleActiveResultSets=true");

        return new TaskAgentDbContext(optionsBuilder.Options);
    }
}