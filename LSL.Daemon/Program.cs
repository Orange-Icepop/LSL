namespace LSL.Daemon;

class Program
{
    private static readonly Mutex s_daemonMutex = new(true, $"Global\\{DaemonConstant.AppName}_Mutex");

    public static void Main(string[] args)
    {
        Console.WriteLine($"Lime Server Launcher Daemon Program, version {DaemonConstant.AppVersion}");
        try
        {
            if (!s_daemonMutex.WaitOne(TimeSpan.Zero, true))
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
            s_daemonMutex.ReleaseMutex();
            s_daemonMutex.Dispose();
        }
    }
}

internal static class DaemonConstant
{
    internal const string AppName = "Orllow_LSL_Daemon";
    internal const string AppVersion = "0.09";
}