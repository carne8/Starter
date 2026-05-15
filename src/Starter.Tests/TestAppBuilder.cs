using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Starter.Tests.TestAppBuilder))]
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerAssembly)]

namespace Starter.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder
            .Configure(() => new App { IsTestMode = true })
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
