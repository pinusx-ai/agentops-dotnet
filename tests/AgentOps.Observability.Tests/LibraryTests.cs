using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Xunit;

namespace AgentOps.Observability.Tests;

/// <summary>
/// End-to-end tests covering the public surface of AgentOps.Observability.
/// Exercises both the static factory path (AgentOpsObservability.CreateTracerProvider)
/// and the DI extension path (services.AddAgentOpsObservability).
/// </summary>
public class LibraryTests
{
    /// <summary>
    /// A reachable-but-not-listening endpoint. Connection refused fast (~1 ms),
    /// which exercises the reachability check's warning path without adding the
    /// 2-second timeout to every test run.
    /// </summary>
    private const string FastFailEndpoint = "http://localhost:1";

    // ============================================================
    // AgentOpsObservabilityOptions.Validate()
    // ============================================================

    [Fact]
    public void Validate_Succeeds_WithValidOptions()
    {
        var options = new AgentOpsObservabilityOptions
        {
            ServiceName = "TestService",
            OtlpEndpoint = "http://localhost:4317"
        };

        var result = options.Validate();

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Fails_ForEmptyServiceName(string serviceName)
    {
        var options = new AgentOpsObservabilityOptions
        {
            ServiceName = serviceName,
            OtlpEndpoint = "http://localhost:4317"
        };

        var result = options.Validate();

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, msg => msg.Contains("ServiceName"));
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://localhost:4317")]
    [InlineData("relative/path")]
    public void Validate_Fails_ForInvalidEndpoint(string endpoint)
    {
        var options = new AgentOpsObservabilityOptions
        {
            ServiceName = "TestService",
            OtlpEndpoint = endpoint
        };

        var result = options.Validate();

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, msg => msg.Contains("OtlpEndpoint"));
    }

    // ============================================================
    // Static factory path
    // ============================================================

    [Fact]
    public void StaticFactory_BuildsTracerProvider_WithValidOptions()
    {
        var options = new AgentOpsObservabilityOptions
        {
            ServiceName = "TestService",
            OtlpEndpoint = FastFailEndpoint
        };

        using var tracerProvider = AgentOpsObservability.CreateTracerProvider(options);

        Assert.NotNull(tracerProvider);
    }

    [Fact]
    public void StaticFactory_Throws_ForInvalidOptions()
    {
        var options = new AgentOpsObservabilityOptions
        {
            ServiceName = "",
            OtlpEndpoint = FastFailEndpoint
        };

        var ex = Assert.Throws<OptionsValidationException>(() =>
            AgentOpsObservability.CreateTracerProvider(options));

        Assert.Contains("ServiceName", ex.Message);
    }

    // ============================================================
    // DI extension path
    // ============================================================

    [Fact]
    public void DIExtension_ResolvesTracerProvider_WithValidOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAgentOpsObservability(options =>
        {
            options.ServiceName = "TestService";
            options.OtlpEndpoint = FastFailEndpoint;
        });

        using var provider = services.BuildServiceProvider();
        var tracerProvider = provider.GetRequiredService<TracerProvider>();

        Assert.NotNull(tracerProvider);
    }

    [Fact]
    public void DIExtension_Throws_ForInvalidOptions_OnResolve()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAgentOpsObservability(options =>
        {
            options.ServiceName = "";
            options.OtlpEndpoint = FastFailEndpoint;
        });

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<TracerProvider>());

        Assert.Contains("ServiceName", ex.Message);
    }
}