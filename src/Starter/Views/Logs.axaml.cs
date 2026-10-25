using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Serilog.Events;

namespace Starter.Views;

public class LogLine : Run
{
    public static readonly DirectProperty<LogLine, LogEventLevel> LogLevelProperty =
        AvaloniaProperty.RegisterDirect<LogLine, LogEventLevel>(
            nameof(LogLevel),
            o => o.LogLevel,
            (o, v) => o.LogLevel = v
        );

    public LogEventLevel LogLevel
    {
        get;
        set => SetAndRaise(LogLevelProperty, ref field, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != LogLevelProperty) return;

        var resourceName = LogLevel switch
        {
            LogEventLevel.Verbose => "LogBrushVerbose",
            LogEventLevel.Debug => "LogBrushDebug",
            LogEventLevel.Information => "LogBrushInformation",
            LogEventLevel.Warning => "LogBrushWarning",
            LogEventLevel.Error => "LogBrushError",
            _ => "LogBrushVerbose"
        };

        Bind(ForegroundProperty, this.GetResourceObservable(resourceName));
    }
}

public partial class Logs : UserControl
{
    public Logs()
    {
        InitializeComponent();
    }
}
