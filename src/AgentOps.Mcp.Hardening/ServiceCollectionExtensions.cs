// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgentOps.Mcp.Hardening;

/// <summary>
/// Extension methods for registering AgentOps runaway-prevention options
/// with the .NET dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AgentOpsMcpHardeningOptions"/> via the Options
    /// pattern with eager validation.
    /// </summary>
    /// <remarks>
    /// This method registers options only. To enforce limits at runtime, call
    /// <see cref="AgentOpsMcpHardening.BeginScope"/> with the configured options
    /// and apply <c>UseAgentOpsMcpHardening</c> on your chat client and agent
    /// builders.
    /// </remarks>
    /// <param name="services">The service collection to register with.</param>
    /// <param name="configure">A callback to configure the options.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddAgentOpsMcpHardening(
        this IServiceCollection services,
        Action<AgentOpsMcpHardeningOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<AgentOpsMcpHardeningOptions>()
            .Configure(configure);

        services.AddSingleton<IValidateOptions<AgentOpsMcpHardeningOptions>, OptionsValidator>();

        return services;
    }

    private sealed class OptionsValidator : IValidateOptions<AgentOpsMcpHardeningOptions>
    {
        public ValidateOptionsResult Validate(string? name, AgentOpsMcpHardeningOptions options)
            => options.Validate();
    }
}