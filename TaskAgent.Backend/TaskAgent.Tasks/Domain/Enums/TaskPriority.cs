using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAgent.Tasks.Domain.Enums;

/// <summary>
/// Represents the priority level of a task.
/// </summary>
public enum TaskPriority
{
    /// <summary>
    /// Low priority - can be addressed when time permits.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium priority - standard importance level.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High priority - requires timely attention.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority - requires immediate attention.
    /// </summary>
    Critical = 3
}