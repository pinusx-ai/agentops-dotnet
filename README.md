# AgentOps.NET

> Production architecture and library suite for putting **Microsoft Agent Framework** + **Model Context Protocol** into production on .NET. Built for .NET 10. Opinionated. Boring on purpose.

[![NuGet AgentOps.Observability](https://img.shields.io/nuget/v/AgentOps.Observability)](https://www.nuget.org/packages/AgentOps.Observability)
[![NuGet AgentOps.Mcp.Hardening](https://img.shields.io/nuget/v/AgentOps.Mcp.Hardening)](https://www.nuget.org/packages/AgentOps.Mcp.Hardening)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

> **Status:** Two libraries live on NuGet. Runnable end-to-end sample with full Aspire Dashboard trace evidence. Evaluation library and multi-agent reference workload on the roadmap.

---

## What's shipped

| Library | Version | What it does |
|---|---|---|
| [`AgentOps.Observability`](https://www.nuget.org/packages/AgentOps.Observability) | `0.1.0-alpha` | OTel GenAI tracing for MAF + MCP agents with one-line wiring |
| [`AgentOps.Mcp.Hardening`](https://www.nuget.org/packages/AgentOps.Mcp.Hardening) | `0.1.0-alpha` | Runaway prevention: bounded tool calls, recursion depth, token budget |
| `AgentOps.Evaluation` | _Stage 4_ | Golden-set evals for MAF agents in CI |

Plus a runnable sample: [`samples/02-mcp-hardening-quickstart/`](samples/02-mcp-hardening-quickstart/) — three demos exercising all three middleware layers against real LLM, with 12 trace screenshots captured at [`docs/screenshots/`](docs/screenshots/).

---

## Why this exists

Microsoft Agent Framework hit 1.0 GA in April 2026. The MCP C# SDK hit 1.0 in March 2026. Both are production-grade. The official samples deliberately stop at "hello-world agent" and "single-page travel planner."

Every team going from sample to production hits the same potholes in the same week:

1. **Observability that goes beyond `.UseOpenTelemetry()`** — wiring OTel GenAI conventions to your real telemetry backend with cost, latency, and tool-call visibility.
2. **Runaway prevention** — unbounded tool-call loops, recursive sub-agent storms, and silent token-budget burns that turn a $5 chat into a $5,000 incident.
3. **MCP server hardening (auth + audit)** — Entra ID + Managed Identity + scope-to-tool authorization, approval gateways for destructive tools, OWASP-aligned guardrails.
4. **Agent evaluation in CI** — running golden-set evals on every PR, failing the build on regression, tracking drift across agents and workflows.

This repo closes #1 and #2 today. #3 is planned as `AgentOps.Mcp.Auth`. #4 is `AgentOps.Evaluation` (Stage 4).

---

## The Microsoft-skips-this matrix

| Production concern | Official Microsoft sample | What's missing | What this repo provides |
|---|---|---|---|
| Tracing & observability | `.UseOpenTelemetry()` one-liner | Production-ready OTLP wiring with cost / latency / tool-call visibility | `AgentOps.Observability` ✅ |
| Runaway prevention | None in samples | Bounded tool calls, recursion depth, token budget with telemetry signals | `AgentOps.Mcp.Hardening` ✅ |
| MCP server auth | `WithHttpTransport()` with OAuth scopes example | Entra ID validation, scope-to-tool maps, audit logging, approval gateway | `AgentOps.Mcp.Auth` (planned) |
| Agent evaluation in CI | Azure AI Evaluation SDK quickstart | xUnit fixtures, golden YAML, PR-comment reporters, regression gates | `AgentOps.Evaluation` (planned) |
| End-to-end reference workload | Single-agent travel planner | Multi-agent graph, hybrid retrieval, Entra-secured | `samples/contoso-knowledge-assistant` (planned) |
| Infrastructure-as-code | Bicep snippets per service | Single `azd up` deploy of full stack | Bicep + `azure.yaml` (planned) |

---

## The two shipped libraries

### `AgentOps.Observability`

OTel GenAI tracing for MAF + MCP agents. One-line wiring to OTLP. Forwards to any OTLP backend (Aspire Dashboard, Honeycomb, Datadog, OpenTelemetry Collector → Application Insights / Langfuse / etc).

```csharp
using var tracerProvider = AgentOpsObservability.CreateTracerProvider(
    new AgentOpsObservabilityOptions
    {
        ServiceName = "MyAgent",
        OtlpEndpoint = "http://localhost:4317"
    },
    loggerFactory);
```

Or via DI:

```csharp
services.AddAgentOpsObservability(opts => opts.OtlpEndpoint = "...");
```

Captures all MAF + MEAI GenAI semantic conventions: model and version, prompt and completion tokens, tool-call spans, agent-handoff spans, MCP server and tool labels. Source registration covers the `Experimental.*` prefix variants emitted while OTel GenAI conventions remain in Development status.

Deferred to v0.2: PII redaction filter, dedicated Application Insights exporter, Langfuse exporter, sampling configuration. v0.1-alpha uses standard OTLP — bring your own backend.

### `AgentOps.Mcp.Hardening`

Runaway prevention for MAF agents via three composing middleware layers.

```csharp
using var scope = AgentOpsMcpHardening.BeginScope(new AgentOpsMcpHardeningOptions
{
    MaxToolCalls = 50,
    MaxRecursionDepth = 5,
    MaxTokenBudget = 100_000,
});

var chatClient = baseChatClient
    .AsBuilder()
    .UseFunctionInvocation()
    .UseAgentOpsMcpHardening(loggerFactory)  // chat-layer
    .Build();

var agent = new ChatClientAgent(chatClient, ...)
    .AsBuilder()
    .UseAgentOpsMcpHardening(loggerFactory)  // agent + function layers
    .Build();
```

Every breach emits three signals: a typed exception, an OpenTelemetry span event (`agentops.runaway.{capability}` with `capability` / `limit` / `actual` attributes), and a structured `ILogger` warning. Composes with `AgentOps.Observability` so it all lands in one trace tree.

See [ADR-003](docs/decisions/ADR-003-maf-middleware-asymmetry.md) for an architectural finding worth knowing: MAF's `FunctionInvokingChatClient` catches function- and agent-layer exceptions and converts them to LLM-visible tool errors, producing graceful degradation by default. Chat-layer exceptions propagate normally. The library emits identical telemetry from all three layers regardless.

Runnable demo: [`samples/02-mcp-hardening-quickstart/`](samples/02-mcp-hardening-quickstart/).

---

## Try the quickstart

```bash
git clone https://github.com/pinusx-ai/agentops-dotnet
cd agentops-dotnet

export OPENAI_API_KEY=sk-...
dotnet build agentops-dotnet.slnx

# Start an OTLP gRPC receiver on port 4317 (e.g. Aspire Dashboard). Then:
dotnet run --project samples/02-mcp-hardening-quickstart/agent
```

Three demos run sequentially, exercising all three middleware layers. Console output shows formatted exception banners; full traces appear in your OTLP backend with the `agentops.runaway.{capability}` events attached to the failing spans.

Sample walkthrough with screenshots: [`samples/02-mcp-hardening-quickstart/README.md`](samples/02-mcp-hardening-quickstart/README.md).

---

## Roadmap

| Stage | What | Status |
|---|---|---|
| 1 | MAF + MCP + OTel spike — `samples/01` | ✅ Shipped |
| 2 | `AgentOps.Observability` v0.1-alpha on NuGet | ✅ Shipped |
| 3 | `AgentOps.Mcp.Hardening` v0.1-alpha on NuGet | ✅ Shipped |
| 4 | `AgentOps.Evaluation` v0.1-alpha | Planned |
| 5 | `AgentOps.Mcp.Auth` v0.1-alpha (Entra ID + scopes + audit + approval gateway) | Planned |
| 6 | Multi-agent reference workload — `samples/contoso-knowledge-assistant` | Planned |
| 7 | `AgentOps.Templates` (`dotnet new agentops`) | Planned |

Star the repo or follow [@hariprakashdb](https://linkedin.com/in/hariprakashdb) to track shipments.

---

## ADRs

Senior engineers read the ADRs before the README. We do too.

- [ADR-001: Why three libraries instead of one framework](docs/decisions/ADR-001-three-libraries-not-one-framework.md)
- [ADR-003: MAF middleware layer asymmetry and exception propagation](docs/decisions/ADR-003-maf-middleware-asymmetry.md)

Planned: ADR-002 (OTel GenAI conventions over custom schema), ADR-004 (auth strategy when `AgentOps.Mcp.Auth` lands), ADR-005 (approval gateway design), ADR-006 (xUnit over bespoke eval runner), ADR-007 (hybrid retrieval over Azure AI Search + pgvector), ADR-008 (GA-only API surface with previews quarantined to `/labs`).

---

## Compatibility

| Component | Tested versions |
|---|---|
| .NET | 10.0 |
| Microsoft.Agents.AI | 1.3.0 (1.x compatible) |
| Microsoft.Extensions.AI | 10.5.0 (10.x compatible) |
| ModelContextProtocol | 1.0.0 |
| OpenAI / Azure OpenAI | gpt-4o-mini tested; gpt-4o, gpt-5.x compatible |

GA-only on the public API surface. Preview features will quarantine to `/labs` when applicable.

---

## What this project is *not*

To keep scope honest:

- **Not a vertical solution.** No healthcare, claims, aviation, MRO, parts management, maintenance workflow, defense, or aerospace patterns. The planned Contoso reference workload is deliberately generic.
- **Not a competitor to Microsoft Agent Framework.** It depends on MAF and tracks its public API surface. When MAF adds a feature this repo provides, the repo deprecates.
- **Not a replacement for Azure AI Evaluation SDK or RAGAS.** When `AgentOps.Evaluation` ships, it will compose them.
- **Not a low-code platform.** No Copilot Studio, no Power Platform.
- **Not opinionated about which LLM you use.** Bring your own. Tested against Azure OpenAI; documented swap paths to Anthropic via Foundry, OpenAI direct, and OSS via Foundry Local will follow.

---

## Repository layout

```
agentops-dotnet/
├── samples/
│   ├── 01-maf-mcp-quickstart/         ✅ Shipped — MAF + MCP + OTel quickstart
│   └── 02-mcp-hardening-quickstart/   ✅ Shipped — three demos, all middleware layers
├── src/
│   ├── AgentOps.Observability/         ✅ On NuGet
│   └── AgentOps.Mcp.Hardening/         ✅ On NuGet
├── tests/
│   └── AgentOps.Observability.Tests/  ✅
├── docs/
│   ├── decisions/                      ADRs
│   └── screenshots/                    Trace evidence from sample runs
└── agentops-dotnet.slnx                Solution file
```

---

## Contributing

Issues and PRs welcome. The project is in active development; APIs may shift before 1.0.

This project follows the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).

---

## About

Built and maintained by **Hari Prakash**, founder of [Pinusx AI](https://pinusx.com) — production AI systems on .NET and Azure.

12+ years building production .NET systems across enterprise SaaS. 2+ years shipping production agentic AI: zero-hallucination text-to-SQL on a live ERP (Semantic Kernel + Cosmos DB vector search), and a 14-stage multi-agent orchestrator (Kerno).

Available for senior advisory and implementation engagements on Microsoft Agent Framework migrations, MCP server hardening, and production agent reliability.

- LinkedIn: [linkedin.com/in/hariprakashdb](https://linkedin.com/in/hariprakashdb)
- Engagements: [pinusx.com/contact](https://pinusx.com/contact)
- Email: info@pinusx.com

---

## License

MIT. See [LICENSE](LICENSE).