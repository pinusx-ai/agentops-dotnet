// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

namespace AgentOps.Mcp.Hardening;

/// <summary>
/// Base exception for all AgentOps runaway-prevention detections.
/// </summary>
/// <remarks>
/// Concrete subtypes (<see cref="CallCountExceededException"/>,
/// <see cref="RecursionDepthExceededException"/>,
/// <see cref="TokenBudgetExceededException"/>) identify which capability fired.
/// Consumers can catch this base type for uniform handling, or catch specific
/// subtypes for capability-specific recovery.
/// </remarks>
public abstract class RunawayDetectedException : Exception
{
    /// <summary>
    /// Stable identifier for the capability that fired. Used as the
    /// <c>agentops.runaway.capability</c> attribute on emitted OpenTelemetry
    /// span events and in structured log entries.
    /// </summary>
    public abstract string Capability { get; }

    /// <summary>
    /// The configured limit value that was exceeded.
    /// </summary>
    public long Limit { get; }

    /// <summary>
    /// The actual value at the moment of detection.
    /// </summary>
    public long Actual { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RunawayDetectedException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="limit">The configured limit.</param>
    /// <param name="actual">The actual value that exceeded the limit.</param>
    protected RunawayDetectedException(string message, long limit, long actual)
        : base(message)
    {
        Limit = limit;
        Actual = actual;
    }
}