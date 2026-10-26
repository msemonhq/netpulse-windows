using NetPulse;
using Xunit;

namespace NetPulse.Tests;

public sealed class TrafficRateCalculatorTests
{
    [Theory]
    [InlineData(1500L, 1000L, 500L)]
    [InlineData(1000L, 1000L, 0L)]
    [InlineData(0L, 0L, 0L)]
    public void GetDeltaBytes_ReturnsExpectedDelta(long current, long previous, long expected)
    {
        Assert.Equal(expected, TrafficRateCalculator.GetDeltaBytes(current, previous));
    }

    [Fact]
    public void GetDeltaBytes_TreatsCounterDecreaseAsReset()
    {
        Assert.Equal(0, TrafficRateCalculator.GetDeltaBytes(25, 900));
    }

    [Fact]
    public void GetDeltaBytes_RejectsNegativeCounters()
    {
        Assert.Equal(0, TrafficRateCalculator.GetDeltaBytes(-1, 0));
        Assert.Equal(0, TrafficRateCalculator.GetDeltaBytes(1, -1));
    }

    [Fact]
    public void GetBytesPerSecond_UsesElapsedTime()
    {
        var rate = TrafficRateCalculator.GetBytesPerSecond(1200, TimeSpan.FromSeconds(2));

        Assert.Equal(600d, rate);
    }

    [Fact]
    public void GetBytesPerSecond_ReturnsZeroForInvalidIntervalsOrDeltas()
    {
        Assert.Equal(0d, TrafficRateCalculator.GetBytesPerSecond(100, TimeSpan.Zero));
        Assert.Equal(0d, TrafficRateCalculator.GetBytesPerSecond(-10, TimeSpan.FromSeconds(1)));
        Assert.Equal(0d, TrafficRateCalculator.GetBytesPerSecond(100, TimeSpan.FromSeconds(-1)));
    }
}
