// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

using AgentOps.Hardening.Quickstart.McpServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<WeatherTools>();

await builder.Build().RunAsync();