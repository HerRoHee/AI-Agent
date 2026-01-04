using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAgent.Tasks.Infrastructure.Abstractions;

/// <summary>
/// Abstraction for system time operations.
/// Enables deterministic testing by allowing time to be controlled in tests.
/// </summary>
public interface ISystemClock
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// Production implementation of system clock using actual system time.
/// </summary>
public sealed class SystemClock : ISystemClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>
/// Test implementation of system clock that allows time to be manually set.
/// Useful for deterministic testing of time-dependent behavior.
/// </summary>
public sealed class FakeSystemClock : ISystemClock
{
    private DateTimeOffset _currentTime;

    /// <summary>
    /// Initializes a new instance with the specified time.
    /// </summary>
    public FakeSystemClock(DateTimeOffset initialTime)
    {
        _currentTime = initialTime;
    }

    /// <summary>
    /// Initializes a new instance with the current system time.
    /// </summary>
    public FakeSystemClock()
        : this(DateTimeOffset.UtcNow)
    {
    }

    /// <inheritdoc />
    public DateTimeOffset UtcNow => _currentTime;

    /// <summary>
    /// Manually sets the current time.
    /// </summary>
    public void SetTime(DateTimeOffset time)
    {
        _currentTime = time;
    }

    /// <summary>
    /// Advances the current time by the specified duration.
    /// </summary>
    public void Advance(TimeSpan duration)
    {
        _currentTime = _currentTime.Add(duration);
    }
}