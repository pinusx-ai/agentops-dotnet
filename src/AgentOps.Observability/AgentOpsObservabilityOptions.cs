using Microsoft.Extensions.Options;

namespace AgentOps.Observability;

/// <summary>
/// Configuration for AgentOps observability. Controls how OpenTelemetry traces
/// are produced and exported by Microsoft Agent Framework and
/// <c>Microsoft.Extensions.AI</c> instrumentation.
/// </summary>
/// <remarks>
/// <para>
/// Construct via object initializer:
/// </para>
/// <code>
/// var options = new AgentOpsObservabilityOptions
/// {
///     ServiceName = "MyAgent",
///     OtlpEndpoint = "http://localhost:4317"
/// };
/// </code>
/// <para>
/// Properties are mutable to support the standard Options pattern and
/// <c>Configure</c> callbacks. In normal usage, the library captures the
/// options once at construction and does not re-read them, so mutation
/// after that point has no effect.
/// </para>
/// <para>
/// Validation is performed by <see cref="Validate"/>; it is invoked automatically
/// when used via <c>AddAgentOpsObservability</c> (DI) or
/// <c>AgentOpsObservability.CreateTracerProvider</c> (static factory).
/// </para>
/// </remarks>
public sealed class AgentOpsObservabilityOptions
{
    /// <summary>
    /// The service name reported in the OpenTelemetry resource. This is what
    /// appears as the "service" identifier in trace backends such as Aspire
    /// Dashboard, Application Insights, or Langfuse.
    /// </summary>
    /// <value>
    /// A non-empty, non-whitespace string. By convention, use the application's
    /// assembly name (e.g. <c>"MyCompany.MyAgent"</c>).
    /// </value>
    /// <remarks>
    /// Required. Must be set explicitly by the consumer; the <c>required</c>
    /// modifier enforces this at compile time.
    /// </remarks>
    public required string ServiceName { get; set; }

    /// <summary>
    /// The OTLP gRPC endpoint to which traces are exported.
    /// Must be a valid <c>http://</c> or <c>https://</c> URI.
    /// </summary>
    /// <value>
    /// A fully-qualified absolute URI. Defaults to <c>"http://localhost:4317"</c>,
    /// the standard local OpenTelemetry Collector / Aspire Dashboard port.
    /// </value>
    /// <remarks>
    /// For remote endpoints, use a fully-qualified host and port, e.g.
    /// <c>"http://otel-collector.example.com:4317"</c>.
    /// </remarks>
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Validates the options against required-property and format constraints.
    /// </summary>
    /// <returns>
    /// <see cref="ValidateOptionsResult.Success"/> if all properties are valid,
    /// otherwise a <see cref="ValidateOptionsResult"/> containing one or more
    /// failure messages describing what is wrong.
    /// </returns>
    /// <remarks>
    /// <para>Performs the following checks:</para>
    /// <list type="bullet">
    /// <item><see cref="ServiceName"/> must not be null, empty, or whitespace.</item>
    /// <item><see cref="OtlpEndpoint"/> must parse as an absolute URI with either
    /// an <c>http</c> or <c>https</c> scheme.</item>
    /// </list>
    /// <para>
    /// The method is non-throwing. Call sites convert the result to an exception
    /// or DI validation failure as appropriate.
    /// </para>
    /// </remarks>
    public ValidateOptionsResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ServiceName))
        {
            errors.Add($"{nameof(ServiceName)} must not be null, empty, or whitespace.");
        }

        if (!Uri.TryCreate(OtlpEndpoint, UriKind.Absolute, out var uri))
        {
            errors.Add($"{nameof(OtlpEndpoint)} is not a valid absolute URI. Received: '{OtlpEndpoint}'.");
        }
        else if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            errors.Add($"{nameof(OtlpEndpoint)} must use 'http' or 'https' scheme. Received scheme: '{uri.Scheme}'.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}