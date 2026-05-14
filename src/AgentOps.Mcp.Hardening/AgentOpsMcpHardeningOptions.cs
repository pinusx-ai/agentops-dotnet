// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

using Microsoft.Extensions.Options;

namespace AgentOps.Mcp.Hardening;

/// <summary>
/// Configuration options for AgentOps runaway-prevention middleware.
/// </summary>
/// <remarks>
/// All limits are opt-in. Properties left unset (null) disable the corresponding
/// capability. An options instance with all properties unset is a valid configuration
/// (no-op for every capability). Each set value must be greater than zero.
/// </remarks>
public sealed class AgentOpsMcpHardeningOptions
{
    /// <summary>
    /// Maximum tool calls permitted within the current runaway-prevention scope.
    /// When exceeded, a <see cref="CallCountExceededException"/> is thrown by the
    /// function-invocation middleware.
    /// Leave <c>null</c> to disable. Must be greater than 0 if set.
    /// </summary>
    /// <remarks>
    /// This counter is scope-wide and accumulates across multiple
    /// <c>agent.RunAsync()</c> invocations within the same scope. For per-turn
    /// iteration capping, use MAF's built-in
    /// <c>FunctionInvokingChatClient.MaximumIterationsPerRequest</c>.
    /// </remarks>
    public long? MaxToolCalls { get; set; }

    /// <summary>
    /// Maximum nested agent invocation depth permitted within the current
    /// runaway-prevention scope. When exceeded, a
    /// <see cref="RecursionDepthExceededException"/> is thrown by the agent middleware.
    /// Leave <c>null</c> to disable. Must be greater than 0 if set.
    /// </summary>
    public long? MaxRecursionDepth { get; set; }

    /// <summary>
    /// Maximum cumulative token consumption (input + output) permitted within the
    /// current runaway-prevention scope. When exceeded, a
    /// <see cref="TokenBudgetExceededException"/> is thrown by the chat client middleware.
    /// Leave <c>null</c> to disable. Must be greater than 0 if set.
    /// </summary>
    public long? MaxTokenBudget { get; set; }

    /// <summary>
    /// Validates the configured option values. Each set value must be greater
    /// than zero; unset (null) values are valid (no-op for that capability).
    /// </summary>
    /// <returns>
    /// A <see cref="ValidateOptionsResult"/> indicating success or, on failure,
    /// the validation error messages.
    /// </returns>
    public ValidateOptionsResult Validate()
    {
        List<string>? failures = null;

        if (MaxToolCalls is <= 0)
        {
            failures ??= [];
            failures.Add($"{nameof(MaxToolCalls)} must be greater than 0 if set.");
        }

        if (MaxRecursionDepth is <= 0)
        {
            failures ??= [];
            failures.Add($"{nameof(MaxRecursionDepth)} must be greater than 0 if set.");
        }

        if (MaxTokenBudget is <= 0)
        {
            failures ??= [];
            failures.Add($"{nameof(MaxTokenBudget)} must be greater than 0 if set.");
        }

        return failures is null
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}