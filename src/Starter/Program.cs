using System.Globalization;
using Avalonia;
using Serilog;
using Starter.Features;
using Starter.Features.PlatformInterop;

namespace Starter;

public static class Program
{
    public static readonly Win32PlatformOptions Win32PlatformOptions = new() { WinUICompositionBackdropCornerRadius = 20 };
    // ReSharper disable once NotAccessedField.Local
    private static Stream? lockStream;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (!EnsureSingleInstance())
        {
            foreach (var arg in args) IPC.sendString(arg);
            Console.WriteLine("Starter is already running.");
            return;
        }

        try
        {
            Logging.setupLogger();
            Log.Information("---*--- Starting up ---*---");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Fatal error, exiting.");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        // Check if the current user's culture actually requires an East Asian IME
        var isCjk = IsCjkLanguage(CultureInfo.CurrentUICulture);

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // .UseR3() can't use because of https://github.com/Cysharp/R3/issues/379
            .With(Win32PlatformOptions)
            .With(new X11PlatformOptions { EnableIme = isCjk })
            #if DEBUG
            .WithDeveloperTools()
            #endif
            .LogToTrace();
    }

    private static bool IsCjkLanguage(CultureInfo culture)
    {
        var lang = culture.TwoLetterISOLanguageName.ToLower();
        return lang is "zh" or "ja" or "ko" or "vi";
    }

    private static bool EnsureSingleInstance()
    {
        if (OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException();

        var dir = Features.Constants.ConfigDirectory;
        Directory.CreateDirectory(dir);

        var lockFile = Path.Combine(dir, "starter.lock");
        FileStream? stream = null;

        try
        {
            stream = File.Open(
                lockFile,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite
            );

            if (stream.Length == 0) stream.SetLength(1);
            stream.Lock(0, 1);
            lockStream = stream;
            return true;
        }
        catch (IOException)
        {
            stream?.Dispose();
            return false;
        }
        catch
        {
            stream?.Dispose();
            throw;
        }
    }
}
