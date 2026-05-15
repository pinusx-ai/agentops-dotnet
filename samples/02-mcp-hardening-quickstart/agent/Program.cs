// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

using AgentOps.Hardening.Quickstart.Agent;
using AgentOps.Mcp.Hardening;
using AgentOps.Observability;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using OpenAI;

const string ServiceName = "AgentOps.Hardening.Quickstart.Agent";

// --- Setup -----------------------------------------------------------------

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("Set the OPENAI_API_KEY environment variable.");

var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? "http://localhost:4317";

using var loggerFactory = LoggerFactory.Create(builder =>
    builder.SetMinimumLevel(LogLevel.Warning).AddConsole());

// --- Observability (sister library; emits GenAI OTel spans) ----------------

using var tracerProvider = AgentOpsObservability.CreateTracerProvider(
    new AgentOpsObservabilityOptions { ServiceName = ServiceName, OtlpEndpoint = otlpEndpoint },
    loggerFactory);

// --- Chat client pipeline ---------------------------------------------------

var baseChatClient = new OpenAIClient(apiKey)
    .GetChatClient("gpt-4o-mini")
    .AsIChatClient();

var chatClient = baseChatClient
    .AsBuilder()
    .UseFunctionInvocation()
    .UseAgentOpsObservability()
    .UseAgentOpsMcpHardening(loggerFactory)  // chat-layer middleware (token budget)
    .Build();

// --- MCP server (spawn as subprocess) --------------------------------------

var mcpServerProject = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "mcp-server", "McpServer.csproj"));

var mcpTransport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "weather-mcp",
    Command = "dotnet",
    Arguments = ["run", "--project", mcpServerProject, "--configuration", "Release", "--no-build"],
});

await using var mcpClient = await McpClient.CreateAsync(mcpTransport);
var mcpTools = await mcpClient.ListToolsAsync();

// --- Agent tools (inline) --------------------------------------------------

var agentTools = new AgentTools(chatClient, loggerFactory);
var subAgentTool = AIFunctionFactory.Create(agentTools.CallSubAgent);

// --- Agent pipeline ---------------------------------------------------------

var agent = new ChatClientAgent(
        chatClient,
        instructions: "You are a helpful assistant. Use tools when appropriate.",
        name: "AgentOps.Hardening.Quickstart.Agent",
        tools: [.. mcpTools.Cast<AITool>(), subAgentTool])
    .AsBuilder()
    .UseAgentOpsMcpHardening(loggerFactory)  // agent + function middleware
    .Build();

// --- Run the three demos ----------------------------------------------------

Console.WriteLine("AgentOps.Mcp.Hardening — runaway prevention quickstart");
Console.WriteLine("Aspire Dashboard: http://localhost:18888");
Console.WriteLine();

await HardeningDemos.RunCallCountDemoAsync(agent);
await HardeningDemos.RunRecursionDepthDemoAsync(agent);
await HardeningDemos.RunTokenBudgetDemoAsync(agent);

Console.WriteLine();
Console.WriteLine("All demos complete. Inspect the traces at http://localhost:18888.");