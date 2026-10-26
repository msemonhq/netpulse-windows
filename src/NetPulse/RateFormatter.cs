using System.Globalization;

namespace NetPulse;

public static class RateFormatter
{
    public static string FormatRate(double bytesPerSecond)
    {
        var value = Math.Max(0, double.IsFinite(bytesPerSecond) ? bytesPerSecond : 0);

        if (value >= 1024d * 1024d * 1024d)
        {
            return $"{value / (1024d * 1024d * 1024d):0.##} GB/s";
        }

        if (value >= 1024d * 1024d)
        {
            return $"{value / (1024d * 1024d):0.##} MB/s";
        }

        if (value >= 1024d)
        {
            return $"{value / 1024d:0.#} KB/s";
        }

        return $"{value:0} B/s";
    }

    public static string FormatTotal(long bytes)
    {
        var value = Math.Max(0, bytes);
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double amount = value;
        var unit = 0;

        while (amount >= 1024d && unit < units.Length - 1)
        {
            amount /= 1024d;
            unit++;
        }

        return $"{amount.ToString(unit == 0 ? "0" : "0.##", CultureInfo.InvariantCulture)} {units[unit]}";
    }

    /// <summary>
    /// Produces a short value suitable for a small tray icon. The icon alternates
    /// between directions; the tooltip always shows both complete readings.
    /// </summary>
    public static string FormatIconRate(double bytesPerSecond)
    {
        var value = Math.Max(0, double.IsFinite(bytesPerSecond) ? bytesPerSecond : 0);

        if (value >= 1024d * 1024d)
        {
            var megabytes = value / (1024d * 1024d);
            return megabytes >= 100 ? $"{megabytes:0}M" : $"{megabytes:0.#}M";
        }

        if (value >= 1024d)
        {
            var kilobytes = value / 1024d;
            return kilobytes >= 100 ? $"{kilobytes:0}K" : $"{kilobytes:0.#}K";
        }

        return $"{value:0}";
    }
}
