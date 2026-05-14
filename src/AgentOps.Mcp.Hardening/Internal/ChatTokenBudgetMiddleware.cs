// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgentOps.Mcp.Hardening;

internal sealed class ChatTokenBudgetMiddleware
{
    private readonly RunawayAccumulator _accumulator;
    private readonly ILogger<ChatTokenBudgetMiddleware> _logger;

    public ChatTokenBudgetMiddleware(
        RunawayAccumulator accumulator,
        ILogger<ChatTokenBudgetMiddleware> logger)
    {
        _accumulator = accumulator;
        _logger = logger;
    }

    public async Task<ChatResponse> InvokeAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        IChatClient innerClient,
        CancellationToken cancellationToken)
    {
        var response = await innerClient.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);

        var usage = response.Usage;
        if (usage is not null)
        {
            var input = usage.InputTokenCount ?? 0;
            var output = usage.OutputTokenCount ?? 0;
            var total = input + output;

            if (total > 0)
            {
                var exception = _accumulator.TryAddTokens(total);
                if (exception is not null)
                {
                    RunawayEmitter.EmitTelemetry(exception, _logger);
                    throw exception;
                }
            }
        }

        return response;
    }
}