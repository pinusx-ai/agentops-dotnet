# Changelog

All notable changes to AgentOps.Observability will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0-alpha] - 2026-05-07

### Added
- DI extension `IServiceCollection.AddAgentOpsObservability(...)` for HostBuilder/ASP.NET Core apps.
- Static factory `AgentOpsObservability.CreateTracerProvider(...)` for console apps.
- `ChatClientBuilder.UseAgentOpsObservability()` extension wrapping `UseOpenTelemetry` with sane defaults.
- `AgentOpsObservabilityOptions` configuration class with validation.
- Automatic source registration for `Microsoft.Extensions.AI` and `Microsoft.Agents.AI`, including the `Experimental.*` prefix variants emitted while the OpenTelemetry GenAI Semantic Conventions remain in Development status.
- OTLP exporter wired with configurable endpoint (defaults to `http://localhost:4317`).
- Endpoint reachability check at startup with a clear warning log if the OTLP endpoint is unreachable.
- Targets `net10.0`.

### Notes
- Initial alpha release. The public API surface may change before 1.0.
- Deferred to v0.2: PII redaction filter, Application Insights exporter alongside OTLP, sampling configuration.

[Unreleased]: https://github.com/pinusx-ai/agentops-dotnet/compare/v0.1.0-alpha...HEAD
[0.1.0-alpha]: https://github.com/pinusx-ai/agentops-dotnet/releases/tag/v0.1.0-alpha
