# Changelog

All notable changes to AgentOps.Mcp.Hardening will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0-alpha] - 2026-05-15

### Added

- Static factory `AgentOpsMcpHardening.BeginScope(...)` opening an AsyncLocal-scoped state container with configured limits.
- `AgentOpsMcpHardeningOptions` configuration class with three optional limits: `MaxToolCalls`, `MaxRecursionDepth`, `MaxTokenBudget`.
- DI extension `IServiceCollection.AddAgentOpsMcpHardening(...)` registering options validation.
- `ChatClientBuilder.UseAgentOpsMcpHardening(...)` extension wiring the chat-layer middleware (`ChatTokenBudgetMiddleware`).
- `AIAgentBuilder.UseAgentOpsMcpHardening(...)` extension wiring the agent-layer (`AgentRecursionMiddleware`) and function-layer (`FunctionCallCountMiddleware`) middleware.
- Three internal middleware components sharing a singleton accumulator via `AsyncLocal<ScopeState>`.
- Typed exception hierarchy rooted at `RunawayDetectedException` with three concrete subclasses: `CallCountExceededException`, `RecursionDepthExceededException`, `TokenBudgetExceededException`.
- Triple-signal emission on breach: typed exception + OpenTelemetry span event (`agentops.runaway.{capability}` with structured attributes `capability`, `limit`, `actual`) + `ILogger` warning via source-generated `LoggerMessage`.
- Targets `net10.0`.

### Notes

- Initial alpha release. The public API surface may change before 1.0.
- Function- and agent-layer exceptions are caught by MAF's `FunctionInvokingChatClient` and presented to the LLM as structured tool errors, producing graceful degradation. Chat-layer exceptions propagate to consumer code. See [ADR-003](../../docs/decisions/ADR-003-maf-middleware-asymmetry.md) for the architectural rationale.
- Deferred to v0.2: per-tool overrides, cost-based USD budget, pluggable `IRunawayPolicy` interface, `AgentOps.Mcp.Hardening.Azure` companion package (App Insights sink, Azure Monitor metrics, cost dictionaries).

[Unreleased]: https://github.com/pinusx-ai/agentops-dotnet/compare/hardening-v0.1.0-alpha...HEAD
[0.1.0-alpha]: https://github.com/pinusx-ai/agentops-dotnet/releases/tag/hardening-v0.1.0-alpha