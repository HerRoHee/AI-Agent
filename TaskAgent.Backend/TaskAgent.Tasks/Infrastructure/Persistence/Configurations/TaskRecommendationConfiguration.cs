using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskAgent.Tasks.Domain.Entities;

namespace TaskAgent.Tasks.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for TaskRecommendation entity.
/// </summary>
internal sealed class TaskRecommendationConfiguration : IEntityTypeConfiguration<TaskRecommendation>
{
    public void Configure(EntityTypeBuilder<TaskRecommendation> builder)
    {
        builder.ToTable("Recommendations");

        // Primary key
        builder.HasKey(r => r.Id);

        // Properties
        builder.Property(r => r.Id)
            .IsRequired()
            .ValueGeneratedNever(); // Domain generates GUIDs

        builder.Property(r => r.TaskId)
            .IsRequired();

        builder.Property(r => r.RecommendedAction)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Reasoning)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(r => r.ConfidenceScore)
            .IsRequired()
            .HasPrecision(5, 4); // e.g., 0.9500

        builder.Property(r => r.RecommendedPriority)
            .HasConversion<string>() // Store as string for readability
            .HasMaxLength(50);

        builder.Property(r => r.RecommendedSnoozeDuration);

        builder.Property(r => r.GeneratedAt)
            .IsRequired();

        builder.Property(r => r.ExpiresAt)
            .IsRequired();

        builder.Property(r => r.IsApplied)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.AppliedAt);

        // Indexes for common queries
        builder.HasIndex(r => r.TaskId)
            .HasDatabaseName("IX_Recommendations_TaskId");

        builder.HasIndex(r => new { r.TaskId, r.GeneratedAt })
            .HasDatabaseName("IX_Recommendations_TaskId_GeneratedAt");

        builder.HasIndex(r => r.IsApplied)
            .HasDatabaseName("IX_Recommendations_IsApplied");

        builder.HasIndex(r => r.ExpiresAt)
            .HasDatabaseName("IX_Recommendations_ExpiresAt");
    }
}