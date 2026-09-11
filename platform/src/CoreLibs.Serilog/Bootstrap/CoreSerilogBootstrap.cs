using Serilog;

namespace CoreLibs.Serilog.Bootstrap;

public static class CoreSerilogBootstrap
{
    /// <summary>
    /// Configures a bootstrap console logger and registers an unhandled-exception handler.
    /// Call this as the very first line of Program.cs, before the host is built.
    /// </summary>
    /// <param name="exitOnFatal">
    /// When <c>true</c>, calls <see cref="Environment.Exit(int)"/> after flushing logs.
    /// Defaults to <c>false</c> so the runtime can decide how to handle the process exit.
    /// </param>
    public static void Configure(bool exitOnFatal = false)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log.Fatal(
                (Exception)args.ExceptionObject,
                "Unhandled exception. IsTerminating: {IsTerminating}",
                args.IsTerminating);

            Log.CloseAndFlush();

            if (exitOnFatal)
                Environment.Exit(1);
        };
    }
}
