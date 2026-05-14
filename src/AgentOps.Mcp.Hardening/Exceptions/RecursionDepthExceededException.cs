// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

namespace AgentOps.Mcp.Hardening;

/// <summary>
/// Thrown when the configured <see cref="AgentOpsMcpHardeningOptions.MaxRecursionDepth"/>
/// limit is exceeded by nested agent invocations within the current
/// runaway-prevention scope.
/// </summary>
public sealed class RecursionDepthExceededException : RunawayDetectedException
{
    /// <summary>
    /// Capability identifier emitted on OpenTelemetry events and log entries.
    /// </summary>
    public const string CapabilityName = "recursiondepth";

    /// <inheritdoc/>
    public override string Capability => CapabilityName;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecursionDepthExceededException"/> class.
    /// </summary>
    /// <param name="limit">The configured maximum recursion depth.</param>
    /// <param name="actual">The actual recursion depth at the moment of detection.</param>
    public RecursionDepthExceededException(long limit, long actual)
        : base($"Agent recursion depth limit exceeded. Limit: {limit}, Actual: {actual}.", limit, actual)
    {
    }
}