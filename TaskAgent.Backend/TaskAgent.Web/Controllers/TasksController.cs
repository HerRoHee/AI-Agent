using Microsoft.AspNetCore.Mvc;
using TaskAgent.Tasks.Application.Interfaces;
using TaskAgent.Tasks.Application.Services;
using TaskAgent.Tasks.Domain.Enums;
using TaskAgent.Web.DTO;
using TaskAgent.Web.Mapping;
using TaskStatus = TaskAgent.Tasks.Domain.Enums.TaskStatus;

namespace TaskAgent.Web.Controllers;

/// <summary>
/// Thin HTTP API controller for task operations.
/// NO BUSINESS LOGIC - only enqueues tasks and retrieves data.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class TasksController : ControllerBase
{
    private readonly TaskQueueService _queueService;
    private readonly ITaskRepository _taskRepository;

    public TasksController(
        TaskQueueService queueService,
        ITaskRepository taskRepository)
    {
        _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
    }

    /// <summary>
    /// Creates and enqueues a new task.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTask(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate request (structural validation only)
            TaskMapper.ValidateCreateRequest(request);

            // Parse priority string to enum
            var priority = TaskMapper.ParsePriority(request.Priority);

            // Enqueue task (business logic in application layer)
            var task = await _queueService.EnqueueTaskAsync(
                request.Title,
                request.Description,
                priority,
                request.DueDate,
                cancellationToken);

            // Map to response DTO
            var response = TaskMapper.ToResponse(task);

            return CreatedAtAction(
                nameof(GetTask),
                new { id = task.Id },
                response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a task by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTask(Guid id, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken);

        if (task is null)
            return NotFound(new { error = $"Task {id} not found." });

        return Ok(TaskMapper.ToResponse(task));
    }

    /// <summary>
    /// Retrieves all tasks, optionally filtered by status.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTasks(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        TaskStatus? statusFilter = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<TaskStatus>(status, ignoreCase: true, out var parsed))
            {
                statusFilter = parsed;
            }
            else
            {
                return BadRequest(new { error = $"Invalid status: {status}" });
            }
        }

        var tasks = await _taskRepository.GetAllAsync(statusFilter, cancellationToken);
        var response = tasks.Select(TaskMapper.ToResponse);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves system statistics.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(SystemStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var allTasks = await _taskRepository.GetAllAsync(cancellationToken: cancellationToken);
        var overdueTasks = await _taskRepository.GetOverdueTasksAsync(cancellationToken);

        var stats = new SystemStatsResponse
        {
            TotalTasks = allTasks.Count,
            PendingTasks = await _taskRepository.CountByStatusAsync(TaskStatus.Pending, cancellationToken),
            ActiveTasks = await _taskRepository.CountByStatusAsync(TaskStatus.Active, cancellationToken),
            SnoozedTasks = await _taskRepository.CountByStatusAsync(TaskStatus.Snoozed, cancellationToken),
            EscalatedTasks = await _taskRepository.CountByStatusAsync(TaskStatus.Escalated, cancellationToken),
            CompletedTasks = await _taskRepository.CountByStatusAsync(TaskStatus.Completed, cancellationToken),
            OverdueTasks = overdueTasks.Count
        };

        return Ok(stats);
    }
}