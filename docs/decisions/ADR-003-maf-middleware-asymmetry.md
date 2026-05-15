# ADR-003: MAF Middleware Layer Asymmetry and Exception Propagation

- **Status:** Accepted
- **Date:** 2026-05-15
- **Deciders:** Hari Prakash (Pinusx AI)
- **Related ADRs:** ADR-001 (initial AgentOps architecture)

## Context

Microsoft Agent Framework 1.3.0 exposes middleware extensibility at three distinct layers in its execution pipeline:

1. **Chat client layer** — middleware added via `IChatClient.AsBuilder().Use(...).Build()`. Runs around every LLM call.
2. **Agent layer** — middleware added via `AIAgentBuilder.Use(...)`. Runs once per `AIAgent.RunAsync()` invocation, wrapping the entire agent run (including any internal LLM and tool-call iterations).
3. **Function invocation layer** — middleware added via `FunctionInvokingChatClient.AsBuilder().UseFunctionInvocation(...)`. Runs around each individual tool/function call dispatched by the LLM.

`AgentOps.Mcp.Hardening` implements one middleware component at each layer:

- `ChatTokenBudgetMiddleware` (chat layer) — enforces `MaxTokenBudget`
- `AgentRecursionMiddleware` (agent layer) — enforces `MaxRecursionDepth`
- `FunctionCallCountMiddleware` (function layer) — enforces `MaxToolCalls`

These three middleware components share a single AsyncLocal-scoped accumulator, so their state composes correctly within one agent session.

### The problem we encountered

When middleware components in each layer threw their typed runaway exceptions, **observed runtime behavior was not symmetric**:

| Middleware layer | Exception thrown | Reaches consumer `try/catch`? |
|---|---|---|
| Chat (`ChatTokenBudgetMiddleware`) | `TokenBudgetExceededException` | ✅ Yes — propagates cleanly |
| Agent (`AgentRecursionMiddleware`) | `RecursionDepthExceededException` | ❌ No — silently absorbed |
| Function (`FunctionCallCountMiddleware`) | `CallCountExceededException` | ❌ No — silently absorbed |

The agent did not crash on the function/agent-layer breaches. Instead, the LLM produced a graceful response acknowledging that a tool had failed.

### Root cause

`Microsoft.Extensions.AI.FunctionInvokingChatClient` — the default middleware MAF inserts when `UseFunctionInvocation()` is called — wraps function invocations in a `try/catch` and **converts caught exceptions into structured tool-error responses** that are fed back to the LLM as content for its next turn.

This is intentional framework design: it allows agents to recover from transient tool failures without aborting the entire run. The LLM sees the error, adapts, and may retry, ask for clarification, or produce a partial answer.

The same behavior applies to agent-layer exceptions when an agent is invoked from inside a tool call (such as our `CallSubAgent` pattern in the recursion demo) — the outer `FunctionInvokingChatClient` catches the sub-agent's exception just as it would any other tool failure.

The chat layer sits **above** `FunctionInvokingChatClient` in the pipeline, so chat-layer exceptions are not subject to this interception.

## Decision

We accept this asymmetry rather than fight it. The library's design honors how MAF was built rather than working around it.

### What we do

1. **Emit identical telemetry from all three layers.** Every breach — whether it propagates or gets absorbed by `FunctionInvokingChatClient` — emits the same three signals:

   - Typed exception (`RunawayDetectedException` subclass)
   - OpenTelemetry span event (`agentops.runaway.{capability}`) with structured attributes (`capability`, `limit`, `actual`)
   - Structured `ILogger` warning via source-generated `LoggerMessage`

   Operators see every breach in the trace tree regardless of where in the pipeline it fired.

2. **Document the asymmetry prominently.** The library README, the sample README, and this ADR all explain the behavior. There is no surprise when consumers wire the library and observe degraded behavior on function/agent-layer breaches.

3. **Do not attempt to escape `FunctionInvokingChatClient`'s catch.** Options considered and rejected:

   - Throwing `OperationCanceledException` instead — would propagate, but destroys exception type information and conflates with genuine cancellation flows.
   - Setting `FunctionInvocationContext.Terminate = true` — ends the function-call loop cleanly but does not propagate the exception, so we lose the typed-exception signal entirely.
   - Forcing consumers to handle exceptions via callbacks instead of `try/catch` — drives users toward a non-idiomatic API.

### What this means for consumers

| Middleware layer | Production behavior |
|---|---|
| Chat (`MaxTokenBudget`) | Hard abort. Exception reaches your `try/catch`. Suitable for "stop spending money now" semantics. |
| Agent (`MaxRecursionDepth`) | Graceful degradation. Agent recovers with a partial answer. Suitable for "this delegation isn't going anywhere" semantics. |
| Function (`MaxToolCalls`) | Graceful degradation. Agent recovers with a partial answer. Suitable for "you've called enough tools" semantics. |

In all three cases, the breach is fully captured in telemetry. Production monitoring can alert on the OTel events; consumer code can choose whether to raise an alert in real time or rely on operator review of traces.

## Consequences

### Positive

- **Graceful degradation by default.** Most production agents should not hard-crash on a single runaway-prevention trigger. They should degrade, log the event, and produce the best output they can. Our defaults match what most teams want.
- **Telemetry-first observability.** Even when exceptions don't propagate, operators see complete information in their distributed trace UI. This actually matches modern SRE practice better than synchronous exception handling.
- **API stability.** We don't fight MAF's internal design, so future MAF versions are unlikely to break us unexpectedly.

### Negative

- **Surprise risk for new consumers.** A developer writing `try { ... } catch (CallCountExceededException) { ... }` may expect their catch to fire, only to discover at runtime that the agent recovered gracefully instead. This is mitigated by README documentation but is still a real source of cognitive overhead.
- **Test surface is more complex.** Tests must verify behavior through telemetry (OTel + ILogger) rather than relying solely on exception propagation. Our public-API-only test plan (see Q5 design) accommodates this by hand-rolling a `TestLogger` and using `OpenTelemetry.Exporter.InMemory`.
- **Library cannot offer "true hard abort" for function-layer breaches.** If a user genuinely wants their agent to die when `MaxToolCalls` is exceeded, they cannot get that with v0.1. (A future v0.2 might address via a `HardAbort` policy mode that uses `Terminate = true` plus an ambient flag.)

### Future considerations

For v0.2, a `IRunawayPolicy` interface could let consumers customize escalation per-capability:

- `GracefulDegradationPolicy` (default — current behavior)
- `HardAbortPolicy` (use `Terminate = true` + ambient flag + post-run re-throw)
- `EscalateAfterNBreachesPolicy` (let N breaches occur, then propagate)

This is intentionally deferred from v0.1-alpha to keep the initial surface small and let real-world usage shape the policy API.

## References

- Runtime evidence: `samples/02-mcp-hardening-quickstart` shows all three layers firing in a single run with full Aspire Dashboard traces.
- Screenshots: `docs/screenshots/02-mcp-hardening-quickstart/` captures the breach behavior at each layer.
- MAF source: [`Microsoft.Extensions.AI.FunctionInvokingChatClient`](https://github.com/dotnet/extensions/blob/main/src/Libraries/Microsoft.Extensions.AI/Functions/FunctionInvokingChatClient.cs) for the function-layer exception-catching code path.