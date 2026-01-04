using TaskAgent.Tasks.Domain.Entities;
using TaskAgent.Tasks.Domain.Enums;
using TaskAgent.Web.DTO;

namespace TaskAgent.Web.Mapping;

/// <summary>
/// Maps between domain entities and web DTOs.
/// No business logic - pure structural mapping only.
/// </summary>
public static class TaskMapper
{
    /// <summary>
    /// Maps a TaskItem entity to a TaskResponse DTO.
    /// </summary>
    public static TaskResponse ToResponse(TaskItem task)
    {
        return new TaskResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            DueDate = task.DueDate,
            CompletedAt = task.CompletedAt,
            SnoozedUntil = task.SnoozedUntil,
            EscalationCount = task.EscalationCount
        };
    }

    /// <summary>
    /// Maps a CreateTaskRequest DTO to domain priority enum.
    /// Throws ArgumentException if priority string is invalid.
    /// </summary>
    public static TaskPriority ParsePriority(string priority)
    {
        return priority.ToLowerInvariant() switch
        {
            "low" => TaskPriority.Low,
            "medium" => TaskPriority.Medium,
            "high" => TaskPriority.High,
            "critical" => TaskPriority.Critical,
            _ => throw new ArgumentException($"Invalid priority: {priority}. Must be Low, Medium, High, or Critical.", nameof(priority))
        };
    }

    /// <summary>
    /// Validates that required fields are present in the request.
    /// Throws ArgumentException if validation fails.
    /// </summary>
    public static void ValidateCreateRequest(CreateTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.", nameof(request.Title));

        if (string.IsNullOrWhiteSpace(request.Priority))
            throw new ArgumentException("Priority is required.", nameof(request.Priority));

        // Validate priority is parseable
        ParsePriority(request.Priority);
    }
}