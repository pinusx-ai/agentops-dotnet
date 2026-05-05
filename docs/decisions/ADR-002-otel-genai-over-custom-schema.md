# ADR-002: OpenTelemetry GenAI conventions over a custom schema

- Status: Accepted (architectural decision; libraries in active development)
- Date: 2026-05-05
- Deciders: Hari Prakash (founder, Pinusx AI)

## Context

AgentOps.NET emits agent telemetry — spans, attributes, metrics, and events that flow through OpenTelemetry collectors to backends like Aspire Dashboard, Application Insights, Langfuse, or Datadog. The shape of that telemetry is a long-lived interface. Once dashboards, alerts, and CI evaluation gates are built against it, changing the schema breaks everyone downstream.

The macro choice is: adopt the OpenTelemetry GenAI Semantic Conventions, or define an AgentOps-specific schema. This decision shapes every observability surface the libraries expose.

## Decision drivers

- **Where the upstream stack already emits.** `Microsoft.Extensions.AI`'s `UseOpenTelemetry()` middleware emits `gen_ai.*` attributes natively. A custom schema requires either bypassing or post-processing what the upstream emits.
- **Backend support today.** Aspire Dashboard, Application Insights, Langfuse, Datadog (since v1.37), and Grafana all interpret `gen_ai.*` natively. A custom schema means custom dashboards everywhere.
- **Cross-language interop.** A Python team's traces and a .NET team's traces should look the same to operations and security. Custom schemas fragment by language.
- **Schema evolution cost.** The conventions are evolving. The question isn't whether the schema will change; it's whether AgentOps owns those changes (custom) or the OpenTelemetry SIG does (conventions).
- **Stability vs. ship time.** The GenAI conventions are formally in "Development" status as of May 2026. Waiting for stable means shipping no telemetry today.

## Considered options

### Option 1: Adopt OpenTelemetry GenAI Semantic Conventions

Emit exclusively in the `gen_ai.*` namespace as defined by the OpenTelemetry SIG. Where gaps arise, contribute upstream rather than fork. Accept experimental status, hedged by `OTEL_SEMCONV_STABILITY_OPT_IN`.

### Option 2: Custom AgentOps schema (`agentops.*` attributes)

Define a schema specific to AgentOps.NET. Every attribute is owned, named, and stabilized by the project. No dependency on SIG timelines.

### Option 3: Hybrid — `gen_ai.*` primary, `agentops.*` extensions for gaps

Adopt the conventions where they apply; create `agentops.*` attributes for things the conventions don't cover (agent handoff, cost-per-trace, redaction policy state).

## Decision

**Option 1.** AgentOps.NET emits exclusively in the `gen_ai.*` namespace. Where the conventions don't cover a need, the policy is to propose upstream to the OpenTelemetry SIG rather than create an `agentops.*` namespace.

## Rationale

The core argument is empirical: the upstream stack already emits `gen_ai.*`. The hands-on spike at [agentops-learning](https://github.com/pinusx-ai/agentops-learning) required zero custom tagging code — the `Microsoft.Extensions.AI` middleware populated the namespace automatically. Specifically:

- **Span hierarchy** (the `orchestrate_tools` parent, `chat gpt-4o-mini` and `execute_tool get_weather` children) emerged automatically via OpenTelemetry's `TraceId` / `ParentSpanId` correlation. No custom orchestration logic.
- **Token usage** (`gen_ai.usage.input_tokens`, `gen_ai.usage.output_tokens`) was recorded per span. Cost calculation reduces to `tokens × per-model price` downstream — no custom cost attribute needed.
- **Full content** (`gen_ai.input.messages`, `gen_ai.output.messages`) was captured by setting one option (`EnableSensitiveData = true`) — production debugging requirements covered without custom serialization.
- **Tool call attributes** (`gen_ai.tool.call.arguments`, `gen_ai.tool.call.result`, `gen_ai.tool.call.id`, `gen_ai.tool.description`) populated automatically per the spec.

A custom schema would mean re-emitting the same information under different attribute names. Every dashboard, alert, and query downstream would need to be rebuilt for backends that already understand `gen_ai.*`.

The vendor argument is concrete, not theoretical. As of May 2026, Datadog supports `gen_ai.*` natively (since v1.37); Application Insights renders GenAI spans in its default GenAI view; Langfuse maps them to LLM traces; Aspire Dashboard surfaces them in default trace views. Adopting the conventions inherits this entire ecosystem for free; defining a custom schema discards it.

The experimental-status concern is real but bounded. The OpenTelemetry community shipped a migration mechanism — `OTEL_SEMCONV_STABILITY_OPT_IN` with the value `gen_ai_latest_experimental` — that lets instrumentation pin to the current spec or opt into the latest experimental. AgentOps.NET inherits this hedge automatically through `Microsoft.Extensions.AI`; no project-specific migration tooling is required.

Option 2 (custom schema) was rejected because it solves a stability problem at the cost of every other dimension on the list. Option 3 (hybrid) was rejected because it creates schema fragmentation: a downstream consumer would need both the `gen_ai.*` spec and an AgentOps-specific spec to interpret traces fully. The contribute-upstream-on-gaps policy gets the same outcome — coverage of agent-specific needs — without splitting the namespace and without putting AgentOps in the position of maintaining a parallel schema indefinitely.

## Consequences

### Positive

- Zero custom tagging code in `AgentOps.Observability`. The middleware does the work.
- Dashboards, alerts, and queries work on every supported OpenTelemetry backend without custom mapping.
- Cross-language interoperability with Python and TypeScript agent stacks operating on the same conventions.
- Cost calculation is downstream arithmetic on standard token attributes, not a custom field that AgentOps must define and maintain.
- The schema is owned by the OpenTelemetry SIG. The project does not carry schema-design maintenance burden.
- Evolution path is automatic: when the spec stabilizes, AgentOps inherits the stabilized version through `Microsoft.Extensions.AI` updates.

### Negative

- **The .NET implementation namespaces ActivitySources under `Experimental.`** (e.g., `Experimental.Microsoft.Extensions.AI`) to signal stability state. Consumers must register with a wildcard pattern — `AddSource("*Microsoft.Extensions.AI")` — to capture spans regardless of the prefix. When the spec stabilizes, the prefix is expected to drop; the wildcard hedge continues to work.
- **The spec is in "Development" status as of May 2026.** Attribute names and shapes may shift. Migrations are mediated by `OTEL_SEMCONV_STABILITY_OPT_IN`, but a future migration step is in scope.
- **Some agent-specific concerns aren't covered by the conventions yet.** Multi-agent handoff between graph workflow nodes has no first-class span attribute today. The `invoke_agent` operation exists at the single-agent invocation level but not at the workflow-edge level. AgentOps.NET will live with this gap for v0.1 and propose an upstream extension when the multi-agent use case stabilizes.
- **Compiler-generated method names appear in tool spans** (e.g., `_Main_g_GetCurrentTime_0_2`) when tools are defined as local functions inside top-level statements. This is a C# code-shape consequence, not a convention issue. Production AgentOps.NET samples define tools as static methods on named classes to produce clean span names.

## Notes / References

- Hands-on spike validating these observations: [pinusx-ai/agentops-learning](https://github.com/pinusx-ai/agentops-learning)
- OpenTelemetry GenAI Semantic Conventions (root): <https://opentelemetry.io/docs/specs/semconv/gen-ai/>
- GenAI client spans (status: Development): <https://opentelemetry.io/docs/specs/semconv/gen-ai/gen-ai-spans/>
- GenAI agent and framework spans (status: Development): <https://opentelemetry.io/docs/specs/semconv/gen-ai/gen-ai-agent-spans/>
- Stability transition plan and `OTEL_SEMCONV_STABILITY_OPT_IN`: defined within each conventions document linked above
- `Microsoft.Extensions.AI` middleware pipeline: <https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai>
- Related decisions: ADR-001 (three libraries, not one framework), ADR-003 (Langfuse alongside Application Insights)