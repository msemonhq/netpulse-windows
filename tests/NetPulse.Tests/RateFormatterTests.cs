using NetPulse;
using Xunit;

namespace NetPulse.Tests;

public sealed class RateFormatterTests
{
    [Fact]
    public void FormatRate_FormatsMegabytesPerSecond()
    {
        Assert.Equal("1 MB/s", RateFormatter.FormatRate(1_000_000, SpeedUnit.MegabytesPerSecond));
        Assert.Equal("0 MB/s", RateFormatter.FormatRate(0, SpeedUnit.MegabytesPerSecond));
    }

    [Fact]
    public void FormatRate_FormatsMegabitsPerSecond()
    {
        Assert.Equal("8 Mbps", RateFormatter.FormatRate(1_000_000, SpeedUnit.MegabitsPerSecond));
    }

    [Fact]
    public void FormatTotal_FormatsByteTotals()
    {
        Assert.Equal("0 B", RateFormatter.FormatTotal(0));
        Assert.Equal("1 KB", RateFormatter.FormatTotal(1024));
        Assert.Equal("1 MB", RateFormatter.FormatTotal(1024L * 1024L));
    }
}
