using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAgent.Tasks.Domain.Exceptions;

/// <summary>
/// Base exception for all domain rule violations.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when an invalid state transition is attempted.
/// </summary>
public sealed class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(string fromState, string toState)
        : base($"Cannot transition from {fromState} to {toState}.")
    {
        FromState = fromState;
        ToState = toState;
    }

    public string FromState { get; }
    public string ToState { get; }
}

/// <summary>
/// Exception thrown when an operation is attempted on a completed task.
/// </summary>
public sealed class TaskAlreadyCompletedException : DomainException
{
    public TaskAlreadyCompletedException(Guid taskId)
        : base($"Task {taskId} is already completed and cannot be modified.")
    {
        TaskId = taskId;
    }

    public Guid TaskId { get; }
}

/// <summary>
/// Exception thrown when a domain invariant is violated.
/// </summary>
public sealed class InvariantViolationException : DomainException
{
    public InvariantViolationException(string message) : base(message)
    {
    }
}
