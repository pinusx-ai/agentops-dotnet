// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

using AgentOps.Mcp.Hardening;
using Microsoft.Agents.AI;

namespace AgentOps.Hardening.Quickstart.Agent;

internal static class HardeningDemos
{
    public static async Task RunCallCountDemoAsync(AIAgent agent)
    {
        const long limit = 3;
        Console.WriteLine();
        Console.WriteLine("=== Demo 1: Call Count Runaway ===");
        Console.WriteLine($"Configured: MaxToolCalls = {limit}");
        Console.WriteLine("Prompt: \"Get the weather in NYC, SF, Tokyo, and London.\"");
        Console.WriteLine();

        using var scope = AgentOpsMcpHardening.BeginScope(new AgentOpsMcpHardeningOptions
        {
            MaxToolCalls = limit,
        });

        try
        {
            var response = await agent.RunAsync(
                "Get the weather in NYC, then SF, then Tokyo, then London. Use the get_weather tool for each.");
            Console.WriteLine("[Agent] " + response.Text);
        }
        catch (CallCountExceededException ex)
        {
            PrintRunawayBanner(ex);
        }
    }

    public static async Task RunRecursionDepthDemoAsync(AIAgent agent)
    {
        const long limit = 1;
        Console.WriteLine();
        Console.WriteLine("=== Demo 2: Recursion Depth Runaway ===");
        Console.WriteLine($"Configured: MaxRecursionDepth = {limit}");
        Console.WriteLine("Prompt: \"Use the call_sub_agent tool to delegate.\"");
        Console.WriteLine();

        using var scope = AgentOpsMcpHardening.BeginScope(new AgentOpsMcpHardeningOptions
        {
            MaxRecursionDepth = limit,
        });

        try
        {
            var response = await agent.RunAsync(
                "Call the call_sub_agent tool with the question 'what is 2+2?'.");
            Console.WriteLine("[Agent] " + response.Text);
        }
        catch (RecursionDepthExceededException ex)
        {
            PrintRunawayBanner(ex);
        }
    }

    public static async Task RunTokenBudgetDemoAsync(AIAgent agent)
    {
        const long limit = 50;
        Console.WriteLine();
        Console.WriteLine("=== Demo 3: Token Budget Runaway ===");
        Console.WriteLine($"Configured: MaxTokenBudget = {limit}");
        Console.WriteLine("Prompt: \"Explain the history of the steam engine in detail.\"");
        Console.WriteLine();

        using var scope = AgentOpsMcpHardening.BeginScope(new AgentOpsMcpHardeningOptions
        {
            MaxTokenBudget = limit,
        });

        try
        {
            var response = await agent.RunAsync(
                "Explain the history of the steam engine in detail with at least three paragraphs.");
            Console.WriteLine("[Agent] " + response.Text);
        }
        catch (TokenBudgetExceededException ex)
        {
            PrintRunawayBanner(ex);
        }
    }

    private static void PrintRunawayBanner(RunawayDetectedException ex)
    {
        Console.WriteLine();
        Console.WriteLine($"✗ {ex.GetType().Name}");
        Console.WriteLine($"  Capability: {ex.Capability}");
        Console.WriteLine($"  Limit:      {ex.Limit}");
        Console.WriteLine($"  Actual:     {ex.Actual}");
        Console.WriteLine($"  Trace:      see Aspire Dashboard for span event 'agentops.runaway.{ex.Capability}'");
        Console.WriteLine();
    }
}