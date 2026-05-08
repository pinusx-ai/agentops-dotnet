using Microsoft.Extensions.AI;

namespace AgentOps.Observability;

/// <summary>
/// Extension methods on <see cref="ChatClientBuilder"/> for wiring AgentOps observability
/// into the chat client middleware pipeline.
/// </summary>
public static class ChatClientBuilderExtensions
{
    /// <summary>
    /// Adds AgentOps observability middleware to the chat client pipeline. Wraps the
    /// upstream <c>UseOpenTelemetry()</c> extension from <c>Microsoft.Extensions.AI</c>
    /// with AgentOps defaults applied first; user customization in <paramref name="configure"/>
    /// is applied second and wins on conflicts.
    /// </summary>
    /// <param name="builder">The chat client builder.</param>
    /// <param name="configure">
    /// Optional callback to customize the resulting <see cref="OpenTelemetryChatClient"/>.
    /// Invoked after AgentOps defaults, so user settings override defaults on the same property.
    /// </param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is <c>null</c>.</exception>
    /// <remarks>
    /// <para>AgentOps defaults applied:</para>
    /// <list type="bullet">
    /// <item>
    /// <c>EnableSensitiveData = true</c> — full prompts and completions are captured
    /// in traces under the <c>gen_ai.input.messages</c> and <c>gen_ai.output.messages</c>
    /// span attributes.
    /// </item>
    /// </list>
    /// <para>
    /// <strong>Security note:</strong> the <c>EnableSensitiveData = true</c> default
    /// makes raw prompts and completions visible in trace backends. This is appropriate
    /// for development and v0.1-alpha. For production deployments handling PII or
    /// regulated data, either pass a <paramref name="configure"/> callback that sets
    /// <c>EnableSensitiveData = false</c>, or apply downstream redaction at the
    /// telemetry pipeline level (Application Insights filters, OTel Collector
    /// processors). Built-in PII redaction is on the v0.2 roadmap.
    /// </para>
    /// <para>
    /// For traces to actually be exported, a <c>TracerProvider</c> must be active —
    /// either built via <c>AgentOpsObservability.CreateTracerProvider(...)</c> for
    /// console apps or registered via <c>services.AddAgentOpsObservability(...)</c>
    /// for DI-based apps.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// IChatClient chatClient = new OpenAIClient(apiKey)
    ///     .GetChatClient("gpt-4o-mini")
    ///     .AsIChatClient()
    ///     .AsBuilder()
    ///     .UseFunctionInvocation()
    ///     .UseAgentOpsObservability()
    ///     .Build();
    /// </code>
    /// <para>To override <c>EnableSensitiveData</c>:</para>
    /// <code>
    /// .UseAgentOpsObservability(client => client.EnableSensitiveData = false)
    /// </code>
    /// </example>
    public static ChatClientBuilder UseAgentOpsObservability(
        this ChatClientBuilder builder,
        Action<OpenTelemetryChatClient>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseOpenTelemetry(configure: client =>
        {
            client.EnableSensitiveData = true;
            configure?.Invoke(client);
        });
    }
}