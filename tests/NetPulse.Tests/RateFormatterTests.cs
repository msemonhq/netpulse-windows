using NetPulse;

namespace NetPulse.Tests;

public sealed class RateFormatterTests
{
    [Fact]
    public void FormatRate_FormatsBytesAndKilobytes()
    {
        Assert.Equal("0 B/s", RateFormatter.FormatRate(0));
        Assert.Equal("1 KB/s", RateFormatter.FormatRate(1024));
    }

    [Fact]
    public void FormatTotal_FormatsByteTotals()
    {
        Assert.Equal("0 B", RateFormatter.FormatTotal(0));
        Assert.Equal("1 KB", RateFormatter.FormatTotal(1024));
        Assert.Equal("1 MB", RateFormatter.FormatTotal(1024L * 1024L));
    }

    [Fact]
    public void FormatIconRate_UsesCompactSuffixes()
    {
        Assert.Equal("0", RateFormatter.FormatIconRate(0));
        Assert.Equal("1K", RateFormatter.FormatIconRate(1024));
        Assert.Equal("1M", RateFormatter.FormatIconRate(1024d * 1024d));
    }
}
