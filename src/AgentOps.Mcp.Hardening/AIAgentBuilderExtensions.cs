// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgentOps.Mcp.Hardening;

/// <summary>
/// Extension methods for adding AgentOps runaway-prevention middleware to an
/// <see cref="AIAgentBuilder"/> pipeline.
/// </summary>
public static class AIAgentBuilderExtensions
{
    /// <summary>
    /// Adds AgentOps runaway-prevention middleware to the agent pipeline:
    /// recursion-depth enforcement (agent middleware) and tool-call-count
    /// enforcement (function middleware).
    /// </summary>
    /// <remarks>
    /// Function middleware fires only when the agent uses <c>FunctionInvokingChatClient</c>,
    /// i.e., when the underlying chat client pipeline includes <c>.UseFunctionInvocation()</c>.
    /// </remarks>
    /// <param name="builder">The agent builder.</param>
    /// <param name="loggerFactory">
    /// Optional. A logger factory used to create loggers for the middleware classes.
    /// If null, structured log emission is disabled; OpenTelemetry span events
    /// are still emitted.
    /// </param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null.</exception>
    public static AIAgentBuilder UseAgentOpsMcpHardening(
        this AIAgentBuilder builder,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var accumulator = AgentOpsMcpHardening.Accumulator;

        var agentLogger = loggerFactory?.CreateLogger<AgentRecursionMiddleware>()
                          ?? NullLogger<AgentRecursionMiddleware>.Instance;
        var functionLogger = loggerFactory?.CreateLogger<FunctionCallCountMiddleware>()
                             ?? NullLogger<FunctionCallCountMiddleware>.Instance;

        var agentMiddleware = new AgentRecursionMiddleware(accumulator, agentLogger);
        var functionMiddleware = new FunctionCallCountMiddleware(accumulator, functionLogger);

        return builder
            .Use(runFunc: agentMiddleware.InvokeAsync, runStreamingFunc: null)
            .Use(functionMiddleware.InvokeAsync);
    }
}