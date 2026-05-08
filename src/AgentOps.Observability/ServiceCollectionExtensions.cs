using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;

namespace AgentOps.Observability;

/// <summary>
/// Dependency injection registration helpers for AgentOps observability.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers AgentOps observability services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">A delegate that configures <see cref="AgentOpsObservabilityOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> or <paramref name="configure"/> is <c>null</c>.</exception>
    /// <remarks>
    /// <para>Registers three things into the container:</para>
    /// <list type="bullet">
    /// <item>
    /// <see cref="AgentOpsObservabilityOptions"/> via the standard
    /// <see cref="IOptions{TOptions}"/> pipeline.
    /// </item>
    /// <item>
    /// An <see cref="IValidateOptions{TOptions}"/> implementation that delegates to
    /// <see cref="AgentOpsObservabilityOptions.Validate"/>, so failure messages match
    /// the static factory path.
    /// </item>
    /// <item>
    /// A singleton <see cref="TracerProvider"/> built from the options. The
    /// TracerProvider must be resolved at least once during application lifetime
    /// (typically via constructor injection or by calling
    /// <c>UseAgentOpsObservability()</c> on a
    /// <see cref="Microsoft.Extensions.AI.ChatClientBuilder"/>) for traces to flow.
    /// </item>
    /// </list>
    /// <para>
    /// Validation runs lazily on first resolution of <see cref="IOptions{TOptions}.Value"/>.
    /// For non-DI scenarios (console apps), use
    /// <see cref="AgentOpsObservability.CreateTracerProvider"/> instead.
    /// </para>
    /// <para>
    /// Requires <see cref="ILoggerFactory"/> to be registered in the container — true
    /// by default in ASP.NET Core, Generic Host, and Worker Service apps. In bare
    /// <c>new ServiceCollection()</c> setups, call <c>services.AddLogging()</c> first.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddAgentOpsObservability(options =>
    /// {
    ///     options.ServiceName = "MyAgent";
    ///     options.OtlpEndpoint = "http://localhost:4317";
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddAgentOpsObservability(
        this IServiceCollection services,
        Action<AgentOpsObservabilityOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<AgentOpsObservabilityOptions>()
            .Configure(configure);

        services.AddSingleton<
            IValidateOptions<AgentOpsObservabilityOptions>,
            AgentOpsObservabilityOptionsValidator>();

        services.AddSingleton<TracerProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AgentOpsObservabilityOptions>>().Value;
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(AgentOpsObservability.LogCategoryName);
            return AgentOpsObservability.BuildTracerProvider(options, logger);
        });

        return services;
    }

    /// <summary>
    /// Bridges <see cref="AgentOpsObservabilityOptions.Validate"/> into the
    /// <see cref="IValidateOptions{TOptions}"/> pipeline so DI consumers receive the
    /// same detailed validation messages defined on the options class.
    /// </summary>
    private sealed class AgentOpsObservabilityOptionsValidator
        : IValidateOptions<AgentOpsObservabilityOptions>
    {
        public ValidateOptionsResult Validate(string? name, AgentOpsObservabilityOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return options.Validate();
        }
    }
}