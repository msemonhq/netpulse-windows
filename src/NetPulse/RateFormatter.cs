using System.Globalization;

namespace NetPulse;

public enum SpeedUnit { MegabytesPerSecond, MegabitsPerSecond }

public static class RateFormatter
{
    public static string FormatRate(double bytesPerSecond, SpeedUnit unit)
    {
        var value = Math.Max(0, double.IsFinite(bytesPerSecond) ? bytesPerSecond : 0);
        return unit == SpeedUnit.MegabitsPerSecond
            ? $"{value * 8d / 1_000_000d:0.##} Mbps"
            : $"{value / 1_000_000d:0.##} MB/s";
    }

    public static string FormatTotal(long bytes)
    {
        var value = Math.Max(0, bytes);
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double amount = value;
        var unit = 0;
        while (amount >= 1024d && unit < units.Length - 1) { amount /= 1024d; unit++; }
        return $"{amount.ToString(unit == 0 ? "0" : "0.##", CultureInfo.InvariantCulture)} {units[unit]}";
    }
}
