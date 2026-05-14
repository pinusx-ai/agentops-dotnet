// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgentOps.Mcp.Hardening;

internal sealed class FunctionCallCountMiddleware
{
    private readonly RunawayAccumulator _accumulator;
    private readonly ILogger<FunctionCallCountMiddleware> _logger;

    public FunctionCallCountMiddleware(
        RunawayAccumulator accumulator,
        ILogger<FunctionCallCountMiddleware> logger)
    {
        _accumulator = accumulator;
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(
        AIAgent agent,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken cancellationToken)
    {
        var exception = _accumulator.TryIncrementToolCalls();
        if (exception is not null)
        {
            RunawayEmitter.EmitTelemetry(exception, _logger);
            throw exception;
        }

        return await next(context, cancellationToken).ConfigureAwait(false);
    }
}