using Avalonia;
using Avalonia.Headless.XUnit;
using Starter.Views;

namespace Starter.Tests;

public class Tests
{
    [AvaloniaFact]
    public void Test1()
    {
        var window = new MainWindow();


        Assert.True(true);
    }
}
