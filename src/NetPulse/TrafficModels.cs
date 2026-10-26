namespace NetPulse;

public sealed record AdapterSnapshot(
    string Id,
    string Name,
    bool IsUp,
    bool HasDefaultGateway,
    long BytesReceived,
    long BytesSent);

public sealed record TrafficSample(
    double DownloadBytesPerSecond,
    double UploadBytesPerSecond,
    long SessionDownloadedBytes,
    long SessionUploadedBytes,
    string? PreferredAdapterName,
    bool IsConnected);

public interface ICounterSource
{
    IReadOnlyList<AdapterSnapshot> ReadAdapters();
}
