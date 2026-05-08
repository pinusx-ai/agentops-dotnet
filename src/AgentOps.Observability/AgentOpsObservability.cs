using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Net.Sockets;

namespace AgentOps.Observability;

/// <summary>
/// Static factory and entry point for AgentOps observability in non-DI scenarios
/// (console apps, simple tools).
/// </summary>
/// <remarks>
/// <para>
/// For HostBuilder or ASP.NET Core applications that already use dependency injection,
/// prefer <c>IServiceCollection.AddAgentOpsObservability(...)</c> instead. The DI
/// extension and this static factory share the same internal builder, so trace
/// pipeline behavior is identical between the two.
/// </para>
/// </remarks>
public static class AgentOpsObservability
{
    /// <summary>
    /// The logger category name used for diagnostic messages emitted by this library.
    /// </summary>
    internal const string LogCategoryName = "AgentOps.Observability";

    /// <summary>
    /// The TCP connection timeout for the startup endpoint reachability check.
    /// Bounds the worst-case startup latency contribution from this library.
    /// </summary>
    internal static readonly TimeSpan ReachabilityCheckTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Builds and returns a configured <see cref="TracerProvider"/> for AgentOps observability.
    /// </summary>
    /// <param name="options">Configuration. Validated synchronously; throws on validation failure.</param>
    /// <param name="loggerFactory">
    /// Optional logger factory used for library diagnostics (such as the startup endpoint
    /// reachability check). If <c>null</c>, warning-level messages are written to
    /// <see cref="Console.Error"/>.
    /// </param>
    /// <returns>
    /// A live <see cref="TracerProvider"/>. The caller is responsible for disposing it,
    /// typically with <c>using var</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is <c>null</c>.</exception>
    /// <exception cref="OptionsValidationException">Thrown when <paramref name="options"/> fails validation.</exception>
    /// <remarks>
    /// The startup endpoint reachability check blocks for up to
    /// <see cref="ReachabilityCheckTimeout"/> on a synchronous TCP probe. This is a
    /// deliberate sync-over-async trade-off to keep the public API non-async.
    /// </remarks>
    /// <example>
    /// <code>
    /// using var tracerProvider = AgentOpsObservability.CreateTracerProvider(new()
    /// {
    ///     ServiceName = "MyAgent",
    ///     OtlpEndpoint = "http://localhost:4317"
    /// });
    /// </code>
    /// </example>
    public static TracerProvider CreateTracerProvider(
        AgentOpsObservabilityOptions options,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var validation = options.Validate();
        if (validation.Failed)
        {
            throw new OptionsValidationException(
                optionsName: nameof(AgentOpsObservabilityOptions),
                optionsType: typeof(AgentOpsObservabilityOptions),
                failureMessages: validation.Failures ?? []);
        }

        var logger = loggerFactory?.CreateLogger(LogCategoryName) ?? ConsoleErrorLogger.Instance;
        return BuildTracerProvider(options, logger);
    }

    /// <summary>
    /// Internal shared trace-pipeline builder. The single source of truth invoked by
    /// both <see cref="CreateTracerProvider"/> (static factory path) and
    /// <c>AddAgentOpsObservability</c> (DI path).
    /// </summary>
    /// <param name="options">
    /// Configuration. Assumed to have already been validated by the caller — DI validates
    /// via the Options pipeline; the static factory validates inside
    /// <see cref="CreateTracerProvider"/>.
    /// </param>
    /// <param name="logger">Logger for library diagnostics. Must not be <c>null</c>.</param>
    /// <returns>A configured <see cref="TracerProvider"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the OpenTelemetry SDK returns a null <see cref="TracerProvider"/> from
    /// <c>Build()</c>. Defensive — does not occur in practice with current SDK versions.
    /// </exception>
    internal static TracerProvider BuildTracerProvider(AgentOpsObservabilityOptions options, ILogger logger)
    {
        var uri = new Uri(options.OtlpEndpoint); // safe: validated upstream
        WarnIfEndpointUnreachable(uri, logger);

        var provider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(options.ServiceName))
            .AddSource("*Microsoft.Extensions.AI")
            .AddSource("*Microsoft.Agents.AI")
            .AddOtlpExporter(opt => opt.Endpoint = uri)
            .Build();

        return provider ?? throw new InvalidOperationException(
            "OpenTelemetry Sdk.CreateTracerProviderBuilder().Build() returned null.");
    }

    /// <summary>
    /// Performs a synchronous TCP connection probe to the OTLP endpoint at startup.
    /// Logs a warning if the endpoint is unreachable or the probe times out.
    /// Never throws — the check is best-effort, never blocks the application from starting.
    /// </summary>
    private static void WarnIfEndpointUnreachable(Uri uri, ILogger logger)
    {
        try
        {
            using var cts = new CancellationTokenSource(ReachabilityCheckTimeout);
            using var client = new TcpClient();
            client.ConnectAsync(uri.Host, uri.Port, cts.Token).GetAwaiter().GetResult();
            // Connected. Silent on success.
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning(
                "OTLP endpoint {Endpoint} did not respond within {Timeout}. Traces may not be exported.",
                uri,
                ReachabilityCheckTimeout);
        }
        catch (SocketException ex)
        {
            logger.LogWarning(
                "OTLP endpoint {Endpoint} is unreachable: {Message}. Traces may not be exported.",
                uri,
                ex.Message);
        }
    }

    /// <summary>
    /// Minimal inline <see cref="ILogger"/> that writes warning-and-above messages to
    /// <see cref="Console.Error"/>. Used as a fallback when no <see cref="ILoggerFactory"/>
    /// is provided to <see cref="CreateTracerProvider"/>, so console-app users still see
    /// the startup reachability warning rather than silent failure.
    /// </summary>
    private sealed class ConsoleErrorLogger : ILogger
    {
        public static readonly ConsoleErrorLogger Instance = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            Console.Error.WriteLine($"[{logLevel}] {LogCategoryName}: {message}");
            if (exception is not null)
            {
                Console.Error.WriteLine(exception);
            }
        }
    }
}