using System.Diagnostics;

namespace NetPulse;

/// <summary>Converts adapter counters into session traffic using independent baselines.</summary>
public sealed class TrafficSampler
{
    private readonly ICounterSource _source;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly Dictionary<string, CounterBaseline> _baselines = new(StringComparer.Ordinal);
    private long _lastElapsedTicks;
    private long _sessionDownloaded;
    private long _sessionUploaded;

    public TrafficSampler(ICounterSource source) => _source = source;

    public TrafficSample Sample()
    {
        var elapsedTicks = _clock.ElapsedTicks;
        var seconds = _lastElapsedTicks == 0 ? 0 :
            (elapsedTicks - _lastElapsedTicks) / (double)Stopwatch.Frequency;
        _lastElapsedTicks = elapsedTicks;
        var adapters = _source.ReadAdapters();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        long rxDelta = 0, txDelta = 0;

        foreach (var adapter in adapters)
        {
            if (!adapter.IsUp) continue;
            seen.Add(adapter.Id);
            if (!_baselines.TryGetValue(adapter.Id, out var previous))
            {
                _baselines[adapter.Id] = new(adapter.BytesReceived, adapter.BytesSent);
                continue;
            }

            var rx = adapter.BytesReceived >= previous.Received ? adapter.BytesReceived - previous.Received : 0;
            var tx = adapter.BytesSent >= previous.Sent ? adapter.BytesSent - previous.Sent : 0;
            rxDelta = AddSaturated(rxDelta, rx);
            txDelta = AddSaturated(txDelta, tx);
            _baselines[adapter.Id] = new(adapter.BytesReceived, adapter.BytesSent);
        }

        foreach (var id in _baselines.Keys.Where(id => !seen.Contains(id)).ToArray()) _baselines.Remove(id);
        _sessionDownloaded = AddSaturated(_sessionDownloaded, rxDelta);
        _sessionUploaded = AddSaturated(_sessionUploaded, txDelta);
        var preferred = adapters.FirstOrDefault(a => a.IsUp && a.HasDefaultGateway) ?? adapters.FirstOrDefault(a => a.IsUp);

        return new TrafficSample(seconds > 0 ? rxDelta / seconds : 0,
            seconds > 0 ? txDelta / seconds : 0, _sessionDownloaded, _sessionUploaded,
            preferred?.Name, preferred is not null);
    }

    private static long AddSaturated(long a, long b) =>
        b <= 0 ? a : a > long.MaxValue - b ? long.MaxValue : a + b;

    private readonly record struct CounterBaseline(long Received, long Sent);
}
