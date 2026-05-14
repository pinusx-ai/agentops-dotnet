// Copyright (c) Pinusx AI, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root.

namespace AgentOps.Mcp.Hardening;

/// <remarks>
/// Holds AsyncLocal-scoped runaway-prevention state. Registered as a singleton
/// in DI; the AsyncLocal flow handles per-operation isolation. State mutations
/// use Interlocked operations for concurrent tool-call safety.
/// </remarks>
internal sealed class RunawayAccumulator
{
    private readonly AsyncLocal<ScopeState?> _current = new();

    public IDisposable BeginScope(AgentOpsMcpHardeningOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var previous = _current.Value;
        _current.Value = new ScopeState(options);
        return new Scope(this, previous);
    }

    public RunawayDetectedException? TryIncrementToolCalls()
    {
        var state = _current.Value;
        if (state is null) return null;

        var limit = state.CallCountLimit;
        if (limit is null) return null;

        var newCount = state.IncrementCallCount();
        return newCount > limit.Value
            ? new CallCountExceededException(limit.Value, newCount)
            : null;
    }

    public RunawayDetectedException? TryIncrementRecursionDepth()
    {
        var state = _current.Value;
        if (state is null) return null;

        var limit = state.RecursionDepthLimit;
        if (limit is null) return null;

        var newDepth = state.IncrementDepth();
        return newDepth > limit.Value
            ? new RecursionDepthExceededException(limit.Value, newDepth)
            : null;
    }

    public void DecrementRecursionDepth() => _current.Value?.DecrementDepth();

    public RunawayDetectedException? TryAddTokens(long count)
    {
        var state = _current.Value;
        if (state is null) return null;

        var limit = state.TokenBudgetLimit;
        if (limit is null) return null;

        var newTotal = state.AddTokens(count);
        return newTotal > limit.Value
            ? new TokenBudgetExceededException(limit.Value, newTotal)
            : null;
    }

    private sealed class ScopeState
    {
        private readonly AgentOpsMcpHardeningOptions _options;
        private long _callCount;
        private long _depth;
        private long _tokens;

        public ScopeState(AgentOpsMcpHardeningOptions options) => _options = options;

        public long? CallCountLimit => _options.MaxToolCalls;
        public long? RecursionDepthLimit => _options.MaxRecursionDepth;
        public long? TokenBudgetLimit => _options.MaxTokenBudget;

        public long IncrementCallCount() => Interlocked.Increment(ref _callCount);
        public long IncrementDepth() => Interlocked.Increment(ref _depth);
        public void DecrementDepth() => Interlocked.Decrement(ref _depth);
        public long AddTokens(long count) => Interlocked.Add(ref _tokens, count);
    }

    private sealed class Scope : IDisposable
    {
        private readonly RunawayAccumulator _owner;
        private readonly ScopeState? _previous;
        private bool _disposed;

        public Scope(RunawayAccumulator owner, ScopeState? previous)
        {
            _owner = owner;
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _owner._current.Value = _previous;
        }
    }
}