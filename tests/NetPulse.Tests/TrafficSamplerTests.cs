using NetPulse;
using Xunit;

namespace NetPulse.Tests;

public sealed class TrafficSamplerTests
{
    [Fact]
    public void NewlyAppearingAdapterUsesBaselineOnly()
    {
        var source = new FakeSource { Current = [A("wifi", 1000, 200, true)] };
        var sampler = new TrafficSampler(source);
        sampler.Sample();
        source.Current = [A("wifi", 1100, 250, true), A("vpn", 900000, 700000, false)];
        var sample = sampler.Sample();
        Assert.Equal(100, sample.SessionDownloadedBytes);
        Assert.Equal(50, sample.SessionUploadedBytes);
    }

    [Fact]
    public void DisappearingThenReturningAdapterUsesNewBaseline()
    {
        var source = new FakeSource { Current = [A("wifi", 100, 50, true)] };
        var sampler = new TrafficSampler(source);
        sampler.Sample();
        source.Current = [];
        sampler.Sample();
        source.Current = [A("wifi", 50000, 60000, true)];
        var sample = sampler.Sample();
        Assert.Equal(0, sample.SessionDownloadedBytes);
        Assert.Equal(0, sample.SessionUploadedBytes);
    }

    [Fact]
    public void CounterResetDoesNotCreateTrafficSpike()
    {
        var source = new FakeSource { Current = [A("wifi", 5000, 9000, true)] };
        var sampler = new TrafficSampler(source);
        sampler.Sample();
        source.Current = [A("wifi", 10, 20, true)];
        var reset = sampler.Sample();
        Assert.Equal(0, reset.SessionDownloadedBytes);
        Assert.Equal(0, reset.SessionUploadedBytes);
        source.Current = [A("wifi", 30, 50, true)];
        var next = sampler.Sample();
        Assert.Equal(20, next.SessionDownloadedBytes);
        Assert.Equal(30, next.SessionUploadedBytes);
    }

    [Fact]
    public void SumsPerAdapterDeltasNotLifetimeTotals()
    {
        var source = new FakeSource { Current = [A("wifi", 1000, 1000, true), A("ethernet", 2000, 2000, false)] };
        var sampler = new TrafficSampler(source);
        sampler.Sample();
        source.Current = [A("wifi", 1100, 1050, true), A("ethernet", 2200, 2100, false)];
        var sample = sampler.Sample();
        Assert.Equal(300, sample.SessionDownloadedBytes);
        Assert.Equal(150, sample.SessionUploadedBytes);
    }

    private static AdapterSnapshot A(string id, long rx, long tx, bool gateway) =>
        new(id, id, true, gateway, rx, tx);

    private sealed class FakeSource : ICounterSource
    {
        public IReadOnlyList<AdapterSnapshot> Current { get; set; } = [];
        public IReadOnlyList<AdapterSnapshot> ReadAdapters() => Current;
    }
}
