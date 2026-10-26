using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace NetPulse;

/// <summary>Reads interface counters and caches adapter metadata. No network requests are made.</summary>
public sealed class NetworkTrafficMonitor : ICounterSource
{
    private readonly TimeSpan _refreshInterval;
    private DateTimeOffset _lastRefresh;
    private IReadOnlyList<AdapterMetadata> _cached = Array.Empty<AdapterMetadata>();

    public NetworkTrafficMonitor(TimeSpan? refreshInterval = null) =>
        _refreshInterval = refreshInterval ?? TimeSpan.FromSeconds(5);

    public IReadOnlyList<AdapterSnapshot> ReadAdapters()
    {
        var now = DateTimeOffset.UtcNow;
        if (_cached.Count == 0 || now - _lastRefresh >= _refreshInterval) RefreshAdapterCache(now);

        var snapshots = new List<AdapterSnapshot>(_cached.Count);
        foreach (var adapter in _cached)
        {
            try
            {
                var current = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(x => string.Equals(x.Id, adapter.Id, StringComparison.Ordinal));
                if (current is null) continue;
                var up = current.OperationalStatus == OperationalStatus.Up;
                long received = 0, sent = 0;
                if (up)
                {
                    var stats = current.GetIPStatistics();
                    received = Math.Max(0, stats.BytesReceived);
                    sent = Math.Max(0, stats.BytesSent);
                }
                snapshots.Add(new AdapterSnapshot(adapter.Id, adapter.Name, up, adapter.HasDefaultGateway, received, sent));
            }
            catch (NetworkInformationException) { }
            catch (NotSupportedException) { }
            catch (InvalidOperationException) { }
        }
        return snapshots;
    }

    private void RefreshAdapterCache(DateTimeOffset now)
    {
        var metadata = new List<AdapterMetadata>();
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;
            bool hasGateway = false;
            try
            {
                hasGateway = adapter.GetIPProperties().GatewayAddresses.Any(g =>
                    (g.Address.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6) &&
                    !g.Address.Equals(System.Net.IPAddress.Any) &&
                    !g.Address.Equals(System.Net.IPAddress.IPv6Any));
            }
            catch (NetworkInformationException) { }
            metadata.Add(new AdapterMetadata(adapter.Id,
                string.IsNullOrWhiteSpace(adapter.Name) ? adapter.Description : adapter.Name, hasGateway));
        }
        _cached = metadata;
        _lastRefresh = now;
    }

    private sealed record AdapterMetadata(string Id, string Name, bool HasDefaultGateway);
}
