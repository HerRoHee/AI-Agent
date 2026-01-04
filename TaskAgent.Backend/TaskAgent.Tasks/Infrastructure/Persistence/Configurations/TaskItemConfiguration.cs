using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskAgent.Tasks.Domain.Entities;
using TaskAgent.Tasks.Domain.Enums;

namespace TaskAgent.Tasks.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for TaskItem entity.
/// </summary>
internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        // Primary key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.Id)
            .IsRequired()
            .ValueGeneratedNever(); // Domain generates GUIDs

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>() // Store as string for readability
            .HasMaxLength(50);

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasConversion<string>() // Store as string for readability
            .HasMaxLength(50);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .IsRequired();

        builder.Property(t => t.DueDate);

        builder.Property(t => t.CompletedAt);

        builder.Property(t => t.SnoozedUntil);

        builder.Property(t => t.EscalationCount)
            .IsRequired()
            .HasDefaultValue(0);

        // Indexes for common queries
        builder.HasIndex(t => t.Status)
            .HasDatabaseName("IX_Tasks_Status");

        builder.HasIndex(t => t.Priority)
            .HasDatabaseName("IX_Tasks_Priority");

        builder.HasIndex(t => t.DueDate)
            .HasDatabaseName("IX_Tasks_DueDate");

        builder.HasIndex(t => t.SnoozedUntil)
            .HasDatabaseName("IX_Tasks_SnoozedUntil");

        builder.HasIndex(t => new { t.Status, t.Priority })
            .HasDatabaseName("IX_Tasks_Status_Priority");
    }
}