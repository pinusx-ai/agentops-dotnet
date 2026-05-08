# AgentOps.Observability

OpenTelemetry GenAI observability for **Microsoft Agent Framework** and **Model Context Protocol** on .NET 10. Drop-in OTLP exporter wiring, ChatClient middleware, source registration with sane defaults.

> **0.1.0-alpha** — initial release. Public API may change before 1.0. See [CHANGELOG](https://github.com/pinusx-ai/agentops-dotnet/blob/main/CHANGELOG.md).

Part of [AgentOps.NET](https://github.com/pinusx-ai/agentops-dotnet) — the reference architecture and library suite for putting Microsoft Agent Framework + MCP into production on Azure.

## Install

```bash
dotnet add package AgentOps.Observability --prerelease
```

## Quick start

**For HostBuilder / ASP.NET Core apps (DI):**

```csharp
builder.Services.AddAgentOpsObservability(options =>
{
    options.ServiceName = "MyAgent";
    options.OtlpEndpoint = "http://localhost:4317";
});
```

**For console apps (static factory):**

```csharp
using var tracerProvider = AgentOpsObservability.CreateTracerProvider(new()
{
    ServiceName = "MyAgent",
    OtlpEndpoint = "http://localhost:4317"
});
```

**On the chat client:**

```csharp
IChatClient chatClient = new OpenAIClient(apiKey)
    .GetChatClient("gpt-4o-mini")
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .UseAgentOpsObservability()
    .Build();
```

## What's in v0.1-alpha

- OTLP exporter wired with sane defaults
- Automatic source registration for `Microsoft.Extensions.AI` and `Microsoft.Agents.AI` (including `Experimental.*` prefix variants)
- `ChatClientBuilder.UseAgentOpsObservability()` middleware
- Endpoint reachability check at startup with clear warning logs
- Targets `net10.0`

## Deferred to v0.2

- PII redaction filter
- Application Insights exporter alongside OTLP
- Sampling configuration

## Documentation

- Working sample: [`agentops-dotnet/samples/01-maf-mcp-quickstart/`](https://github.com/pinusx-ai/agentops-dotnet/tree/main/samples/01-maf-mcp-quickstart/)
- Architecture decisions: [ADR-001 (three libraries)](https://github.com/pinusx-ai/agentops-dotnet/blob/main/docs/decisions/ADR-001-three-libraries-not-one-framework.md), [ADR-002 (OTel GenAI conventions)](https://github.com/pinusx-ai/agentops-dotnet/blob/main/docs/decisions/ADR-002-otel-genai-over-custom-schema.md)

## License

MIT — see [LICENSE](https://github.com/pinusx-ai/agentops-dotnet/blob/main/LICENSE).

Built by [Pinusx AI](https://pinusx.com).