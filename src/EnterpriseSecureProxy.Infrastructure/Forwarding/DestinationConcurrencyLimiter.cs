using System.Collections.Concurrent;
using EnterpriseSecureProxy.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace EnterpriseSecureProxy.Infrastructure.Forwarding;

/// <summary>Caps simultaneous in-flight calls per destination name.</summary>
public sealed class DestinationConcurrencyLimiter
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _maxConcurrent;

    public DestinationConcurrencyLimiter(IOptions<DownstreamOptions> options) =>
        _maxConcurrent = Math.Max(1, options.Value.MaxConcurrentRequestsPerDestination);

    // OWASP Insecure Design: limit concurrent calls per destination so one noisy client cannot exhaust downstream capacity.
    public async Task<IConcurrencyLease> AcquireAsync(string destinationName, CancellationToken cancellationToken)
    {
        var gate = _gates.GetOrAdd(destinationName, _ => new SemaphoreSlim(_maxConcurrent, _maxConcurrent));
        if (!await gate.WaitAsync(0, cancellationToken))
        {
            return DeniedConcurrencyLease.Instance;
        }

        return new SemaphoreLease(gate);
    }

    private sealed class SemaphoreLease : IConcurrencyLease
    {
        private readonly SemaphoreSlim _gate;
        private int _released;

        public SemaphoreLease(SemaphoreSlim gate) => _gate = gate;

        public bool Acquired => true;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                _gate.Release();
            }
        }
    }

    private sealed class DeniedConcurrencyLease : IConcurrencyLease
    {
        public static readonly DeniedConcurrencyLease Instance = new();
        public bool Acquired => false;
        public void Dispose() { }
    }
}

public interface IConcurrencyLease : IDisposable
{
    bool Acquired { get; }
}
