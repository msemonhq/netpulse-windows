namespace NetPulse;

/// <summary>
/// Pure byte-counter calculations kept separate from Windows networking APIs
/// so they can be tested without live network traffic.
/// </summary>
public static class TrafficRateCalculator
{
    /// <summary>
    /// Calculates the non-negative counter delta. A decreasing counter usually
    /// indicates an adapter reset or change; it is treated as a reset, not a spike.
    /// </summary>
    public static long GetDeltaBytes(long currentBytes, long previousBytes)
    {
        if (currentBytes < 0 || previousBytes < 0 || currentBytes < previousBytes)
        {
            return 0;
        }

        return currentBytes - previousBytes;
    }

    /// <summary>Returns bytes per second for a counter delta and elapsed interval.</summary>
    public static double GetBytesPerSecond(long deltaBytes, TimeSpan elapsed)
    {
        if (deltaBytes <= 0 || elapsed <= TimeSpan.Zero)
        {
            return 0;
        }

        return deltaBytes / elapsed.TotalSeconds;
    }
}
