using AgentOps.Quickstart.Agent;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("OPENAI_API_KEY not set");

// Spawn the MCP server process and connect over stdio
await using var mcpClient = await McpClient.CreateAsync(
    new StdioClientTransport(new StdioClientTransportOptions
    {
        Name = "McpServer",
        Command = "dotnet",
        Arguments = ["run", "--project", "../mcp-server"]
    }));

// Discover the tools exposed by the MCP server
var mcpTools = await mcpClient.ListToolsAsync();

Console.WriteLine($"Connected to MCP server. Found {mcpTools.Count} tools:");
foreach (var t in mcpTools)
    Console.WriteLine($"  - {t.Name}: {t.Description}");

var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? "http://localhost:4317";

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("AgentOps.Quickstart.Agent"))
    .AddSource("*Microsoft.Extensions.AI")
    .AddSource("*Microsoft.Agents.AI")
    .AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint))
    .Build();

IChatClient chatClient = new OpenAIClient(apiKey)
    .GetChatClient("gpt-4o-mini")
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .UseOpenTelemetry(configure: c => c.EnableSensitiveData = true)
    .Build();

AIAgent agent = new ChatClientAgent(
    chatClient,
    instructions: "You are a senior .NET architect. Be concise and production-focused. Use tools when they help.",
    name: "ArchitectBot",
    tools: [
        AIFunctionFactory.Create(AgentTools.GetCurrentTime),
        AIFunctionFactory.Create(AgentTools.LookupAzureService),
        ..mcpTools
    ]);

// Multi-turn conversation
var session = await agent.CreateSessionAsync();

Console.WriteLine("Architect bot ready. Type your questions. Empty line to exit.");
while (true)
{
    Console.Write("\nYou: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) break;

    var response = await agent.RunAsync(input, session);
    Console.WriteLine($"\nBot: {response}");
}