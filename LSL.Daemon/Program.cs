namespace LSL.Daemon;

class Program
{
    private static Mutex daemonMutex { get; } = new(true, $"Global\\{DaemonConstant.AppName}_Mutex");

    public static void Main(string[] args)
    {
        Console.WriteLine($"Lime Server Launcher Daemon Program, version {DaemonConstant.AppVersion}");
        try
        {
            if (daemonMutex.WaitOne(TimeSpan.Zero, true))
            {
                Console.WriteLine("An active daemon is running. Exiting...");
                Console.ReadKey();
            }
            else
            {
                
            }
        }
        finally
        {
            daemonMutex.ReleaseMutex();
            daemonMutex.Dispose();
        }
    }
}

internal static class DaemonConstant
{
    internal const string AppName = "Orllow_LSL_Daemon";
    internal const string AppVersion = "0.09";
}