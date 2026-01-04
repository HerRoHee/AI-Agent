using TaskAgent.Tasks.Application.Services;
using TaskAgent.Tasks.Infrastructure;
using TaskAgent.Web.Workers;
using TaskAgent.Tasks.Infrastructure.Seeder;

namespace TaskAgent.Web
{
    public class Program
    {
        public static async void Main(string[] args)
        {
            

            var builder = WebApplication.CreateBuilder(args);

            // ============================================================================
            // SERVICE REGISTRATION
            // ============================================================================

            // Add controllers
            builder.Services.AddControllers();

            // Add API documentation
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new()
                {
                    Title = "TaskAgent API",
                    Version = "v1",
                    Description = "Intelligent task management agent with Sense → Think → Act → Learn loop"
                });
            });

            // Add Infrastructure layer (EF Core, Repositories)
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddTaskAgentInfrastructure(connectionString);

            // Add Application layer services
            builder.Services.AddScoped<TaskQueueService>();
            builder.Services.AddScoped<TaskEvaluationService>();
            builder.Services.AddScoped<RecommendationService>();
            builder.Services.AddSingleton<LearningService>(); // Singleton to maintain experience history

            // Add Background Worker (Agent Loop)
            builder.Services.AddHostedService<TaskAgentWorker>();

            // Add CORS (if needed for frontend)
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // ============================================================================
            // APPLICATION PIPELINE
            // ============================================================================

            var app = builder.Build();

            // Seed database on startup
            using (var scope = app.Services.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                try
                {
                    logger.LogInformation("Seeding database...");
                    await seeder.SeedAsync(includeSampleTasks: builder.Environment.IsDevelopment());
                    logger.LogInformation("Database seeded successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error seeding database");
                }
            }

            // Configure HTTP pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskAgent API v1");
                    options.RoutePrefix = string.Empty; // Swagger at root
                });
            }

            app.UseHttpsRedirection();

            app.UseCors();

            app.UseAuthorization();

            app.MapControllers();

            // ============================================================================
            // RUN APPLICATION
            // ============================================================================

            app.Logger.LogInformation("Starting TaskAgent.Web application");
            app.Logger.LogInformation("Agent workers will start automatically");

            app.Run();
        }
    }
}
