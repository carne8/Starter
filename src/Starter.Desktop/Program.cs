using Avalonia;
using Serilog;

namespace Starter.Desktop;

public static class Program
{
    private const string MutexName = "Starter-426a2d89-cfe9-4554-b9a5-8c7d85417f25";

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task Main(string[] args)
    {
        using var mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            Console.WriteLine("Starter is already running.");
            return;
        }

        try
        {
            Features.Logging.setupLogger();
            Log.Information("---*--- Starting up ---*---");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Fatal error, exiting.");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseR3()
            .With(new Win32PlatformOptions { WinUICompositionBackdropCornerRadius = 20 })
            #if DEBUG
            .WithDeveloperTools()
            #endif
            .LogToTrace();
}
