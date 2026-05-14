// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

using Microsoft.Extensions.Options;

namespace AgentOps.Mcp.Hardening;

/// <summary>
/// Static entry point for AgentOps runaway-prevention. Provides scope management
/// for the runaway-prevention limits configured via <see cref="AgentOpsMcpHardeningOptions"/>.
/// </summary>
/// <remarks>
/// State is held in a process-wide singleton accumulator scoped per-operation
/// via <see cref="AsyncLocal{T}"/>. The same accumulator is referenced by the
/// builder extensions; this ensures scopes begun here are visible to the
/// middleware pipeline regardless of DI configuration.
/// </remarks>
public static class AgentOpsMcpHardening
{
    /// <summary>
    /// Process-wide accumulator shared with builder extensions.
    /// </summary>
    internal static readonly RunawayAccumulator Accumulator = new();

    /// <summary>
    /// Begins a runaway-prevention scope. The configured limits in <paramref name="options"/>
    /// are enforced by AgentOps middleware until the returned scope is disposed.
    /// </summary>
    /// <param name="options">The runaway-prevention limits to enforce within the scope.</param>
    /// <returns>An <see cref="IDisposable"/> scope handle. Dispose to reset state.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
    /// <exception cref="OptionsValidationException">
    /// Thrown if any configured limit is invalid (must be greater than zero if set).
    /// </exception>
    public static IDisposable BeginScope(AgentOpsMcpHardeningOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var result = options.Validate();
        if (result.Failed)
        {
            throw new OptionsValidationException(
                optionsName: nameof(AgentOpsMcpHardeningOptions),
                optionsType: typeof(AgentOpsMcpHardeningOptions),
                failureMessages: result.Failures);
        }

        return Accumulator.BeginScope(options);
    }
}