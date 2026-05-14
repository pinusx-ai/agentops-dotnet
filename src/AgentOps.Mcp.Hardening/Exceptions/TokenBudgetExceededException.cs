// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

namespace AgentOps.Mcp.Hardening;

/// <summary>
/// Thrown when the configured <see cref="AgentOpsMcpHardeningOptions.MaxTokenBudget"/>
/// limit is exceeded by cumulative token consumption within the current
/// runaway-prevention scope.
/// </summary>
public sealed class TokenBudgetExceededException : RunawayDetectedException
{
    /// <summary>
    /// Capability identifier emitted on OpenTelemetry events and log entries.
    /// </summary>
    public const string CapabilityName = "tokenbudget";

    /// <inheritdoc/>
    public override string Capability => CapabilityName;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenBudgetExceededException"/> class.
    /// </summary>
    /// <param name="limit">The configured maximum token budget.</param>
    /// <param name="actual">The actual cumulative token count at the moment of detection.</param>
    public TokenBudgetExceededException(long limit, long actual)
        : base($"Token budget limit exceeded. Limit: {limit}, Actual: {actual}.", limit, actual)
    {
    }
}