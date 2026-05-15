// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

using AgentOps.Mcp.Hardening;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace AgentOps.Hardening.Quickstart.Agent;

internal sealed class AgentTools
{
    private readonly IChatClient _chatClient;
    private readonly ILoggerFactory? _loggerFactory;

    public AgentTools(IChatClient chatClient, ILoggerFactory? loggerFactory = null)
    {
        _chatClient = chatClient;
        _loggerFactory = loggerFactory;
    }

    [Description("Delegates a question to a sub-agent and returns its answer.")]
    public async Task<string> CallSubAgent(
        [Description("The question to ask the sub-agent.")] string question)
    {
        var subAgent = new ChatClientAgent(
                _chatClient,
                instructions: "You are a helpful sub-agent. Answer concisely.",
                name: "sub-agent")
            .AsBuilder()
            .UseAgentOpsMcpHardening(_loggerFactory)  // sub-agent also wired — recursion check fires here
            .Build();

        var response = await subAgent.RunAsync(question);
        return response.Text;
    }
}