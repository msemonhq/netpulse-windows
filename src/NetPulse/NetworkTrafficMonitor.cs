using System.Net.NetworkInformation;

namespace NetPulse;

public sealed record NetworkAdapterInfo(
    string Id,
    string Name,
    string Description,
    OperationalStatus Status,
    NetworkInterfaceType Type,
    long SpeedBitsPerSecond);

public readonly record struct TrafficCounters(long BytesReceived, long BytesSent);

/// <summary>
/// Reads Windows network-interface counters. It deliberately does not generate
/// traffic or contact an external speed-test service.
/// </summary>
public sealed class NetworkTrafficMonitor
{
    public IReadOnlyList<NetworkAdapterInfo> GetAdapters()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(IsSupported)
            .Select(adapter => new NetworkAdapterInfo(
                adapter.Id,
                string.IsNullOrWhiteSpace(adapter.Name) ? adapter.Description : adapter.Name,
                adapter.Description,
                adapter.OperationalStatus,
                adapter.NetworkInterfaceType,
                adapter.Speed))
            .OrderBy(adapter => adapter.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Reads either one selected adapter or all active non-loopback, non-tunnel
    /// adapters. Returns false when no eligible adapter can be sampled.
    /// </summary>
    public bool TryReadCounters(string? selectedAdapterId, out TrafficCounters counters)
    {
        long received = 0;
        long sent = 0;
        var found = false;

        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (!IsSupported(adapter))
            {
                continue;
            }

            if (selectedAdapterId is not null &&
                !string.Equals(adapter.Id, selectedAdapterId, StringComparison.Ordinal))
            {
                continue;
            }

            // For the aggregate view, only count interfaces that are currently up.
            if (adapter.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            try
            {
                var statistics = adapter.GetIPStatistics();
                received = SaturatingAdd(received, statistics.BytesReceived);
                sent = SaturatingAdd(sent, statistics.BytesSent);
                found = true;
            }
            catch (NetworkInformationException)
            {
                // A transient adapter state should not terminate the tray app.
            }
            catch (NotSupportedException)
            {
                // Some virtual adapters do not expose all counters.
            }
        }

        counters = found ? new TrafficCounters(received, sent) : default;
        return found;
    }

    private static bool IsSupported(NetworkInterface adapter) =>
        adapter.NetworkInterfaceType is not NetworkInterfaceType.Loopback
            and not NetworkInterfaceType.Tunnel;

    private static long SaturatingAdd(long current, long amount)
    {
        if (amount <= 0)
        {
            return current;
        }

        return current > long.MaxValue - amount ? long.MaxValue : current + amount;
    }
}
