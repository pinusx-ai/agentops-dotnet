// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace AgentOps.Hardening.Quickstart.McpServer;

[McpServerToolType]
public class WeatherTools
{
    [McpServerTool]
    [Description("Returns the current weather for a given city.")]
    public static string GetWeather(
        [Description("The city to get the weather for.")] string city)
    {
        return city.ToLowerInvariant() switch
        {
            "nyc" or "new york" => "62°F, sunny",
            "sf" or "san francisco" => "58°F, foggy",
            "tokyo" => "71°F, cloudy",
            "london" => "52°F, drizzling",
            "mumbai" => "84°F, humid",
            _ => $"60°F, partly cloudy (stub data for {city})"
        };
    }
}