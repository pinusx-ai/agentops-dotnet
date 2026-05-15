# 02 — MCP Hardening Quickstart

Runnable demo of `AgentOps.Mcp.Hardening` — three runaway scenarios, each
caught by a different middleware layer.

## What you'll see

Three sequential demos in one run. Each configures its own limits via
`BeginScope` and triggers the corresponding middleware:

| Demo | Limit | Middleware fires | Behavior |
|---|---|---|---|
| Call Count Runaway | `MaxToolCalls = 3` | `FunctionCallCountMiddleware` | Agent calls `get_weather` 4 times; the 4th and any retries throw; **agent recovers gracefully with partial output** |
| Recursion Depth Runaway | `MaxRecursionDepth = 1` | `AgentRecursionMiddleware` | Agent invokes a sub-agent; the nested invocation throws; **agent recovers gracefully** |
| Token Budget Runaway | `MaxTokenBudget = 50` | `ChatTokenBudgetMiddleware` | First LLM response exceeds the budget; **exception propagates cleanly to your `try/catch`** |

Every breach — whether it propagates or gets handled internally by the
agent — emits the same three signals:

- An OpenTelemetry span event named `agentops.runaway.{capability}` with
  structured attributes (`capability`, `limit`, `actual`), visible in
  Aspire Dashboard on the failed span
- A structured `ILogger` warning to the console
- A typed exception (`CallCountExceededException`,
  `RecursionDepthExceededException`, or `TokenBudgetExceededException`)
  attached to the span as `error.type`

## Why two behaviors?

This isn't a bug — it's how Microsoft Agent Framework composes middleware
layers:

- **Function and agent-layer exceptions** are caught by
  `FunctionInvokingChatClient`. The framework converts them into structured
  tool errors that the LLM sees, and the agent adapts. You get graceful
  degradation by default — the limit prevents runaway cost; the agent
  returns the best partial answer it can.
- **Chat-layer exceptions** bypass that handling and bubble straight up to
  your code, suitable when you want a hard abort (e.g., for token budget
  enforcement where there's no partial state worth preserving).

Both behaviors are valuable. The library emits identical telemetry
regardless — your operators see every breach in the trace tree even when
the agent quietly recovers.

## Prerequisites

- .NET 10 SDK
- `OPENAI_API_KEY` environment variable
- Aspire Dashboard running on `localhost:4317` (gRPC) /
  `localhost:18888` (web UI) — see the top-level README for setup

## Run it

```powershell
# From the repo root
dotnet build samples/02-mcp-hardening-quickstart/mcp-server --configuration Release
dotnet run --project samples/02-mcp-hardening-quickstart/agent --configuration Release
```

The agent will spawn the MCP server as a subprocess. Expected console
output:

- Demo 1: a `FunctionCallCountMiddleware` warning, then a partial weather
  report (London missing)
- Demo 2: an `AgentRecursionMiddleware` warning, then "there was an error
  when trying to call the sub-agent"
- Demo 3: a `ChatTokenBudgetMiddleware` warning, then a formatted
  `✗ TokenBudgetExceededException` banner

Open the Aspire Dashboard for the full picture — every breach is a red span
with an `agentops.runaway.*` event attached.

## What's happening under the hood

1. `using var scope = AgentOpsMcpHardening.BeginScope(...)` puts a fresh
   counter into `AsyncLocal` storage with the configured limits.
2. Three middleware layers — `AgentRecursionMiddleware`,
   `FunctionCallCountMiddleware`, `ChatTokenBudgetMiddleware` — share the
   same accumulator and each watch their respective boundary.
3. The moment any limit is exceeded, the responsible middleware emits an
   OTel span event, logs a warning, and throws a typed exception.
4. When the `using` block exits, the scope is wiped and the next demo
   starts with fresh counters.

## Trying it yourself

After the first run, edit the limits in `agent/HardeningDemos.cs` and
re-run to see different behavior:

| Change | What happens |
|---|---|
| Raise `MaxToolCalls` to `10` in Demo 1 | Agent completes all 4 weather calls without error |
| Disable `MaxRecursionDepth` in Demo 2 | Sub-agent invocation succeeds; check Aspire trace for the nested span |
| Drop `MaxTokenBudget` to `10` in Demo 3 | LLM response immediately exceeds budget |

Each variation produces a different trace tree in Aspire Dashboard — that's
the strategic point: **Observability shows you what happened; Hardening
stops what shouldn't.**

## Learn more

- [AgentOps.Mcp.Hardening on NuGet](https://www.nuget.org/packages/AgentOps.Mcp.Hardening)
- [AgentOps.Observability on NuGet](https://www.nuget.org/packages/AgentOps.Observability)
- [Top-level repo README](../../README.md)