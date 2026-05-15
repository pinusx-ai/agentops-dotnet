# AgentOps.Mcp.Hardening

[![NuGet](https://img.shields.io/nuget/v/AgentOps.Mcp.Hardening.svg)](https://www.nuget.org/packages/AgentOps.Mcp.Hardening)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

Runaway prevention for Microsoft Agent Framework + MCP agents. Stop the unbounded loops, recursive sub-agent storms, and silent budget burns that turn a $5 chat into a $5,000 incident.

## What it does

Three middleware layers, sharing one accumulator, watching three distinct boundaries:

| Capability | Boundary watched | Limit type |
|---|---|---|
| `MaxToolCalls` | Function invocations per agent session | Hard count |
| `MaxRecursionDepth` | Nested agent invocations within a session | Hard depth |
| `MaxTokenBudget` | Cumulative input + output tokens across all LLM calls | Hard budget |

When any limit is breached, the responsible middleware emits **three signals simultaneously**:

- A **typed exception** (`CallCountExceededException`, `RecursionDepthExceededException`, or `TokenBudgetExceededException`, all inheriting from `RunawayDetectedException`)
- An **OpenTelemetry span event** named `agentops.runaway.{capability}` with structured attributes `capability`, `limit`, `actual` — attached to the failing span so operators see exactly where in the trace tree the limit fired
- A **structured `ILogger` warning** with the same fields

## Quickstart

```bash
dotnet add package AgentOps.Mcp.Hardening --version 0.1.0-alpha
```

```csharp
using AgentOps.Mcp.Hardening;

// 1. Open a scope with limits
using var scope = AgentOpsMcpHardening.BeginScope(new AgentOpsMcpHardeningOptions
{
    MaxToolCalls = 50,
    MaxRecursionDepth = 5,
    MaxTokenBudget = 100_000,
});

// 2. Wire the middleware on your chat client (token budget layer)
var chatClient = baseChatClient
    .AsBuilder()
    .UseFunctionInvocation()
    .UseAgentOpsMcpHardening(loggerFactory)  // chat-layer
    .Build();

// 3. Wire the middleware on your agent (recursion + call-count layers)
var agent = new ChatClientAgent(chatClient, instructions: "...", name: "...", tools: [...])
    .AsBuilder()
    .UseAgentOpsMcpHardening(loggerFactory)  // agent + function layers
    .Build();

// 4. Run normally — limits enforced automatically
var response = await agent.RunAsync("...");
```

That's it. Three method calls, three layers wired, full coverage.

## How it composes with Microsoft Agent Framework

`AgentOps.Mcp.Hardening` plugs into MAF's existing middleware system at three distinct layers:

- The chat client layer (via `IChatClient` builder middleware)
- The agent layer (via `AIAgent` builder middleware)
- The function invocation layer (composed inside the agent middleware)

It does **not** replace `FunctionInvokingChatClient.MaximumIterationsPerRequest` — that built-in MAF setting caps tool calls *per LLM turn*. AgentOps caps *per agent session*, which is the boundary that matters for runaway cost.

## How it composes with AgentOps.Observability

Designed as a pair. `AgentOps.Observability` provides the OTel TracerProvider and instrumentation; `AgentOps.Mcp.Hardening` attaches span events to those traces when limits fire. One service, one trace tree, full story.

```csharp
using var tracerProvider = AgentOpsObservability.CreateTracerProvider(...);

var chatClient = baseChatClient
    .AsBuilder()
    .UseFunctionInvocation()
    .UseAgentOpsObservability()
    .UseAgentOpsMcpHardening(loggerFactory)
    .Build();
```

Together: **Observability shows you what happened; Hardening stops what shouldn't.**

## Important: how breaches are propagated

MAF's `FunctionInvokingChatClient` catches function- and agent-layer exceptions and converts them into structured tool errors that the LLM sees. This is deliberate framework design — it lets the agent recover gracefully with a partial answer rather than hard-crashing on a single tool failure.

What this means for your code:

| Middleware layer | Limit | Exception propagation |
|---|---|---|
| Function (call count) | `MaxToolCalls` | Caught by MAF; agent recovers with partial output. Telemetry + log fire. |
| Agent (recursion) | `MaxRecursionDepth` | Caught by MAF; agent recovers. Telemetry + log fire. |
| Chat (token budget) | `MaxTokenBudget` | Propagates to your `try/catch`. Telemetry + log fire. |

The library emits identical telemetry regardless of where the limit fires. Your operators see every breach in the trace tree even when the agent quietly recovers. Most production teams want this behavior — graceful degradation by default, hard abort available where it matters.

For the full runnable demo: [samples/02-mcp-hardening-quickstart](https://github.com/pinusx-ai/agentops-dotnet/tree/main/samples/02-mcp-hardening-quickstart)

## Compatibility

| Dependency | Version |
|---|---|
| .NET | 10.0 |
| Microsoft.Agents.AI | [1.3.0, 2.0.0) |
| Microsoft.Extensions.AI | [10.5.0, 11.0.0) |

## License

MIT. See [LICENSE](https://github.com/pinusx-ai/agentops-dotnet/blob/main/LICENSE).

## Roadmap

v0.1-alpha → v0.2 directions under consideration:

- Per-tool overrides (different limits for different tools)
- Cost-based USD budget (in addition to token budget)
- Pluggable `IRunawayPolicy` interface for custom escalation
- `AgentOps.Mcp.Hardening.Azure` companion package (App Insights sink, Azure Monitor metrics, cost dictionaries)

## About

Built by [Pinusx AI, LLC](https://pinusx.com) — production AI systems for .NET teams.