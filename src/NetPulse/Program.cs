namespace NetPulse;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, @"Local\NetPulse.Singleton", out var createdNew);
        if (!createdNew) return;
        ApplicationConfiguration.Initialize();
        Application.Run(new NetPulseApplicationContext());
    }
}
