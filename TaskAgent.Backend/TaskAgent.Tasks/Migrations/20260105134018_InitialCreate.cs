using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskAgent.Tasks.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendedAction = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reasoning = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ConfidenceScore = table.Column<double>(type: "float(5)", precision: 5, scale: 4, nullable: false),
                    RecommendedPriority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RecommendedSnoozeDuration = table.Column<TimeSpan>(type: "time", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsApplied = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaxActiveTasks = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    EscalationThresholdHours = table.Column<int>(type: "int", nullable: false, defaultValue: 24),
                    MinimumConfidenceThreshold = table.Column<double>(type: "float(5)", precision: 5, scale: 4, nullable: false, defaultValue: 0.75),
                    DefaultSnoozeDuration = table.Column<TimeSpan>(type: "time", nullable: false, defaultValue: new TimeSpan(0, 4, 0, 0, 0)),
                    RecommendationValidityDuration = table.Column<TimeSpan>(type: "time", nullable: false, defaultValue: new TimeSpan(0, 1, 0, 0, 0)),
                    AutoApplyRecommendations = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AutoEscalateOverdueTasks = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AutoAwakenSnoozedTasks = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SnoozedUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EscalationCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_ExpiresAt",
                table: "Recommendations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_IsApplied",
                table: "Recommendations",
                column: "IsApplied");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_TaskId",
                table: "Recommendations",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_TaskId_GeneratedAt",
                table: "Recommendations",
                columns: new[] { "TaskId", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Settings_UpdatedAt",
                table: "Settings",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_DueDate",
                table: "Tasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Priority",
                table: "Tasks",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_SnoozedUntil",
                table: "Tasks",
                column: "SnoozedUntil");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Status",
                table: "Tasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Status_Priority",
                table: "Tasks",
                columns: new[] { "Status", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Recommendations");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Tasks");
        }
    }
}
