using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAgent.Tasks.Domain.Enums;

/// <summary>
/// Represents the current status of a task in its lifecycle.
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Task is waiting to be started.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Task is currently being worked on.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Task has been temporarily snoozed and will return to pending.
    /// </summary>
    Snoozed = 2,

    /// <summary>
    /// Task requires urgent attention and has been escalated.
    /// </summary>
    Escalated = 3,

    /// <summary>
    /// Task has been finished and is in a terminal state.
    /// </summary>
    Completed = 4
}
