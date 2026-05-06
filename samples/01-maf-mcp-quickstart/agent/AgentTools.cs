using System.ComponentModel;

namespace AgentOps.Quickstart.Agent;

public static class AgentTools
{
    [Description("Returns the current UTC date and time")]
    public static string GetCurrentTime() =>
        DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

    [Description("Returns a brief description of an Azure service by name")]
    public static string LookupAzureService(
        [Description("Azure service name, e.g. 'cosmos db'")] string serviceName)
    {
        return serviceName.ToLowerInvariant() switch
        {
            "cosmos db" => "Globally distributed multi-model database with vector search (DiskANN). NoSQL, MongoDB, Cassandra APIs.",
            "azure ai search" => "Hybrid retrieval service. Combines BM25 keyword search with vector similarity. Semantic ranker available.",
            "app insights" => "Application Performance Monitoring. Receives OpenTelemetry traces. Built into Azure Monitor.",
            "azure openai" => "Microsoft-managed OpenAI deployments. Same models as OpenAI direct, plus Entra ID auth, regional residency, content filtering.",
            _ => $"No description for '{serviceName}'."
        };
    }
}