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
/// Entity Framework Core configuration for SystemSettings entity.
/// </summary>
internal sealed class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        builder.ToTable("Settings");

        // Primary key
        builder.HasKey(s => s.Id);

        // Properties
        builder.Property(s => s.Id)
            .IsRequired()
            .ValueGeneratedNever(); // Domain generates GUIDs

        builder.Property(s => s.MaxActiveTasks)
            .IsRequired()
            .HasDefaultValue(10);

        builder.Property(s => s.EscalationThresholdHours)
            .IsRequired()
            .HasDefaultValue(24);

        builder.Property(s => s.MinimumConfidenceThreshold)
            .IsRequired()
            .HasPrecision(5, 4) // e.g., 0.7500
            .HasDefaultValue(0.75);

        builder.Property(s => s.DefaultSnoozeDuration)
            .IsRequired()
            .HasDefaultValue(TimeSpan.FromHours(4));

        builder.Property(s => s.RecommendationValidityDuration)
            .IsRequired()
            .HasDefaultValue(TimeSpan.FromHours(1));

        builder.Property(s => s.AutoApplyRecommendations)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.AutoEscalateOverdueTasks)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.AutoAwakenSnoozedTasks)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // Index for UpdatedAt to track changes
        builder.HasIndex(s => s.UpdatedAt)
            .HasDatabaseName("IX_Settings_UpdatedAt");
    }
}