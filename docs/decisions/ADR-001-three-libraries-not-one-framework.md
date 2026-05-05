# ADR-001: Three libraries, not one framework

- Status: Accepted (architectural decision; libraries in active development)
- Date: 2026-05-05
- Deciders: Hari Prakash (founder, Pinusx AI)

## Context

AgentOps.NET addresses three production gaps left open by Microsoft's official Microsoft Agent Framework (MAF) + Model Context Protocol (MCP) samples: observability defaults that wire to enterprise telemetry stacks, hardening for MCP servers exposing tools to agents, and agent evaluation as a CI gate.

Each gap is real, each is independently valuable, and each has a distinct community looking for it. The packaging question is whether AgentOps.NET ships as one framework that addresses all three, or as three libraries that can be adopted independently.

This decision sets the surface-area shape for everything that follows. It governs versioning, dependency graphs, deprecation strategy, and how teams adopt the work piecewise. Reversing it later would mean breaking changes in package identity — the most expensive kind.

## Decision drivers

- **Where each concern injects into the runtime.** The three concerns have non-overlapping injection points in the MAF + MCP architecture. Observability hooks the chat-client middleware pipeline. Hardening hooks the MCP server's transport/auth layer. Evaluation runs out-of-process. A library should follow the architecture, not impose one.
- **Independent lifecycles.** `Microsoft.Extensions.AI`, the MCP C# SDK, and Azure AI Evaluation SDK + RAGAS evolve on different cadences. Coupling all three into one library means any one of them dragging the package's version with it.
- **Heterogeneous audiences.** Observability is for every team running production agents. Hardening is for security-owning teams with compliance constraints. Evaluation is a CI/QA concern, often owned by a different person from the production engineer. One package forces three audiences to accept each other's surface.
- **Deprecation path.** When MAF or MCP eventually subsumes one of these concerns — a likely outcome given Microsoft's stated intent to track production needs — AgentOps.NET should be able to deprecate the affected library cleanly without forcing users to migrate the other two.

## Considered options

### Option 1: One unified framework (`AgentOps.NET` package)

A single NuGet package wiring observability, hardening, and evaluation behind one builder API. Single install, single version, batteries included.

### Option 2: Three independent libraries

`AgentOps.Observability`, `AgentOps.Mcp.Hardening`, and `AgentOps.Evaluation` published as separate NuGet packages. Each addresses one concern at the architectural layer where that concern lives. A `dotnet new agentops` template installs all three by default for green-field projects, but each is consumable in isolation.

### Option 3: Meta-package with optional sub-libraries

A `AgentOps.NET` meta-package that depends on three sub-packages. Users install the meta or any subset. Combines the discovery surface of Option 1 with the composability of Option 2.

## Decision

**Option 2.** AgentOps.NET ships as three independent libraries. No meta-package.

## Rationale

The decisive argument is architectural. The three concerns inject into the MAF + MCP runtime at three structurally different points, demonstrated by the spike at [agentops-learning](https://github.com/pinusx-ai/agentops-learning):

1. **Observability** injects at the `IChatClient` middleware layer. The hands-on path is `ChatClientBuilder.UseOpenTelemetry(...)`, sitting in the per-request, in-process pipeline that every model call flows through. The lifecycle here is request-time middleware: registered at startup, invoked on every chat completion.

2. **MCP server hardening** wraps the MCP server's transport/auth layer. In the spike, that's where `WithStdioServerTransport()` sits; in production, the same architectural slot is occupied by the HTTP transport, with Entra ID validation, scope-to-tool authorization, audit logging, and the approval gateway layered on top. The lifecycle is server-boundary: a process running the MCP server, doing per-tool-invocation auth and policy.

3. **Evaluation** runs out-of-process. The hands-on shape is xUnit fixtures consuming golden YAML, executed in CI, comparing groundedness / relevance / faithfulness scores against thresholds. The lifecycle is out-of-band: triggered by a PR, runs against a snapshot of the agent stack, never on the runtime path.

These are not three flavors of the same concern. They are three architecturally distinct surfaces with three different lifetimes — request-time middleware, server-boundary hardening, and out-of-process test execution.

Forcing them into one library would create coupling between concerns the runtime keeps separate. A team adopting only observability would inherit a transitive dependency on the MCP C# SDK even if their agent has no MCP integration. A security team replacing the hardening defaults with their own would be blocked by surface they can't carve out. A CI pipeline running evaluations would pull in middleware code that has no business in a test runner.

This mirrors Microsoft's own packaging. `Microsoft.Extensions.AI`, `Microsoft.Agents.AI`, and `ModelContextProtocol` ship as independent packages because they describe independent concerns at independent layers. AgentOps.NET follows the upstream shape rather than imposing a fictional unification on top of it.

The composability gain is concrete:

- A team can adopt `AgentOps.Observability` against an MAF agent that uses no MCP at all.
- A security team can adopt `AgentOps.Mcp.Hardening` standalone, applied to any MCP server — agent or not.
- A QA team can adopt `AgentOps.Evaluation` as a CI gate without changing a single line of runtime code.

Option 3 (meta-package) was rejected because it solves a discoverability problem at the cost of versioning coherence. A meta-package implies a coordinated release train across the three sub-libraries, which would re-introduce exactly the lifecycle coupling Option 2 exists to avoid. Discoverability is better solved with a documented umbrella name — the GitHub repo, the org, the website — than with a package that offers no technical value over three direct dependencies.

## Consequences

### Positive

- Each library has one reason to change. Versioning aligns with the upstream concern it wraps.
- Each library is consumable standalone. Adoption is incremental, not all-or-nothing.
- Deprecation is local. When MAF or MCP subsumes a concern, the corresponding library can be marked deprecated without affecting the others.
- Each library's upstream coupling is contained. A breaking change in the MCP C# SDK does not force a version bump on `AgentOps.Observability`.
- Each audience — production engineers, security teams, QA/CI owners — can consume the library most relevant to them without paying for surface area covering adjacent concerns.

### Negative

- Three packages to publish, version, document, and support. Operationally heavier than one package.
- A compatibility matrix must be maintained. Combinations of the three libraries that pin to different upstream versions can diverge over time.
- First-time adoption is slightly higher friction. A green-field project running `dotnet new agentops` installs all three; ad-hoc adoption requires three deliberate `dotnet add package` calls.
- No single discovery surface on NuGet. Searching "AgentOps.NET" returns three results, not one. Mitigated by repo, website, and consistent naming conventions.
- Cross-cutting features that span concerns (e.g., redaction policies that should apply to both observability spans and audit logs) require an explicit shared abstraction rather than being implicit within one framework. Treated as forcing better design, not as a cost.

## Notes / References

- The hands-on spike grounding the three injection points: [pinusx-ai/agentops-learning](https://github.com/pinusx-ai/agentops-learning)
- `Microsoft.Extensions.AI` middleware pipeline: <https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai>
- MCP C# SDK: <https://github.com/modelcontextprotocol/csharp-sdk>
- Azure AI Evaluation SDK: <https://learn.microsoft.com/en-us/azure/ai-studio/how-to/develop/evaluate-sdk>
- Related decisions: ADR-002 (OTel GenAI conventions over a custom schema), ADR-008 (GA-only public API surface)