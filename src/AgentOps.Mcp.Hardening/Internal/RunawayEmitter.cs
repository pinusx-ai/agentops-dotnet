// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AgentOps.Mcp.Hardening;

/// <remarks>
/// Emits OpenTelemetry span events and structured log entries for runaway
/// detections. Separated from middleware classes to keep them DRY and to
/// keep the accumulator free of telemetry side effects.
/// </remarks>
internal static partial class RunawayEmitter
{
    public static void EmitTelemetry(RunawayDetectedException exception, ILogger logger)
    {
        Activity.Current?.AddEvent(new ActivityEvent(
            name: $"agentops.runaway.{exception.Capability}",
            tags: new ActivityTagsCollection
            {
                ["agentops.runaway.capability"] = exception.Capability,
                ["agentops.runaway.limit"] = exception.Limit,
                ["agentops.runaway.actual"] = exception.Actual,
            }));

        LogRunawayDetected(logger, exception, exception.Capability, exception.Limit, exception.Actual);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "AgentOps runaway detected: capability={Capability}, limit={Limit}, actual={Actual}")]
    private static partial void LogRunawayDetected(
        ILogger logger,
        Exception exception,
        string capability,
        long limit,
        long actual);
}