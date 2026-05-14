// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgentOps.Mcp.Hardening;

internal sealed class AgentRecursionMiddleware
{
    private readonly RunawayAccumulator _accumulator;
    private readonly ILogger<AgentRecursionMiddleware> _logger;

    public AgentRecursionMiddleware(
        RunawayAccumulator accumulator,
        ILogger<AgentRecursionMiddleware> logger)
    {
        _accumulator = accumulator;
        _logger = logger;
    }

    public async Task<AgentResponse> InvokeAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent agent,
        CancellationToken cancellationToken)
    {
        var exception = _accumulator.TryIncrementRecursionDepth();
        if (exception is not null)
        {
            RunawayEmitter.EmitTelemetry(exception, _logger);
            throw exception;
        }

        try
        {
            return await agent.RunAsync(messages, session, options, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _accumulator.DecrementRecursionDepth();
        }
    }
}