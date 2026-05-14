// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgentOps.Mcp.Hardening;

/// <summary>
/// Extension methods for adding AgentOps runaway-prevention middleware to a
/// <see cref="ChatClientBuilder"/> pipeline.
/// </summary>
public static class ChatClientBuilderExtensions
{
    /// <summary>
    /// Adds AgentOps token-budget enforcement to the chat client pipeline.
    /// Token usage is read from <c>ChatResponse.Usage</c> after each LLM call;
    /// when cumulative consumption within the current scope exceeds the
    /// configured <see cref="AgentOpsMcpHardeningOptions.MaxTokenBudget"/>,
    /// a <see cref="TokenBudgetExceededException"/> is thrown.
    /// </summary>
    /// <param name="builder">The chat client builder.</param>
    /// <param name="loggerFactory">
    /// Optional. A logger factory used to create the middleware's logger.
    /// If null, structured log emission is disabled; OpenTelemetry span events
    /// are still emitted.
    /// </param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null.</exception>
    public static ChatClientBuilder UseAgentOpsMcpHardening(
        this ChatClientBuilder builder,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var logger = loggerFactory?.CreateLogger<ChatTokenBudgetMiddleware>()
                     ?? NullLogger<ChatTokenBudgetMiddleware>.Instance;

        var middleware = new ChatTokenBudgetMiddleware(AgentOpsMcpHardening.Accumulator, logger);

        return builder.Use(
            getResponseFunc: middleware.InvokeAsync,
            getStreamingResponseFunc: null);
    }
}