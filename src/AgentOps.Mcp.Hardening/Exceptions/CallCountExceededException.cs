// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

namespace AgentOps.Mcp.Hardening;

/// <summary>
/// Thrown when the configured <see cref="AgentOpsMcpHardeningOptions.MaxToolCalls"/>
/// limit is exceeded within the current runaway-prevention scope.
/// </summary>
public sealed class CallCountExceededException : RunawayDetectedException
{
    /// <summary>
    /// Capability identifier emitted on OpenTelemetry events and log entries.
    /// </summary>
    public const string CapabilityName = "callcount";

    /// <inheritdoc/>
    public override string Capability => CapabilityName;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallCountExceededException"/> class.
    /// </summary>
    /// <param name="limit">The configured maximum tool call count.</param>
    /// <param name="actual">The actual tool call count at the moment of detection.</param>
    public CallCountExceededException(long limit, long actual)
        : base($"Tool call count limit exceeded. Limit: {limit}, Actual: {actual}.", limit, actual)
    {
    }
}