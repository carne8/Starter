using System.Collections.ObjectModel;
using System.Text;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Styling;
using R3;
using Serilog.Events;
using Serilog.Formatting.Display;
using Starter.Features;

namespace Starter.ViewModels;

public class CustomRun : Run
{
    private readonly LogEventLevel logLevel;

    public CustomRun(string text, LogEventLevel logLevel) : base(text)
    {
        this.logLevel = logLevel;
        SetForeground();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.NewValue is ThemeVariant) SetForeground();
        base.OnPropertyChanged(change);
    }

    private void SetForeground()
    {
        Foreground = logLevel switch
        {
            LogEventLevel.Verbose => ActualThemeVariant == ThemeVariant.Light ? LightBrushVerbose : DarkBrushVerbose,
            LogEventLevel.Debug => BrushDebug,
            LogEventLevel.Information => BrushInformation,
            LogEventLevel.Warning => BrushWarning,
            LogEventLevel.Error => BrushError,
            LogEventLevel.Fatal => BrushFatal,
            _ => Foreground
        };
    }

    private static readonly SolidColorBrush LightBrushVerbose = new(new Color(255, 0, 0, 0));
    private static readonly SolidColorBrush DarkBrushVerbose = new(new Color(255, 255, 255, 255));
    private static readonly SolidColorBrush BrushDebug = new(new Color(255, 80, 161, 79));
    private static readonly SolidColorBrush BrushInformation = new(new Color(255, 1, 132, 188));
    private static readonly SolidColorBrush BrushWarning = new(new Color(255, 193, 131, 1));
    private static readonly SolidColorBrush BrushError = new(new Color(255, 228, 86, 73));
    private static readonly SolidColorBrush BrushFatal = new(new Color(255, 166, 38, 164));
}

public partial class LogsViewModel : ObservableObject
{
    private const string Template = "[{Timestamp:HH:mm:ss} {Level:u3}] [{Context}] {Message:lj}";
    private const string TemplateWithException = "[{Timestamp:HH:mm:ss} {Level:u3}] [{Context}] {Message:lj}{NewLine}";
    private static readonly MessageTemplateTextFormatter Formatter = new(Template);
    private static readonly MessageTemplateTextFormatter FormatterWithException = new(TemplateWithException);

    public static readonly LogEventLevel[] LogLevels =
    [
        LogEventLevel.Verbose,
        LogEventLevel.Debug,
        LogEventLevel.Information,
        LogEventLevel.Warning,
        LogEventLevel.Error,
        LogEventLevel.Fatal
    ];

    [ObservableProperty] public partial LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;
    [ObservableProperty] public partial ObservableCollection<Inline> Lines { get; set; } = [];
    [ObservableProperty] public partial bool WrapText { get; set; }
    [ObservableProperty] public partial string SelectedLogContext { get; set; } = "None";
    public ObservableCollection<string> LogContexts { get; } = [ "None" ];

    public LogsViewModel()
    {
        Logging.logs
            .ObserveOnUIThreadDispatcher()
            .Subscribe(logEvent =>
            {
                // Register log context
                var rawLogContext = logEvent.Properties["Context"].ToString();
                var logContext = rawLogContext.Substring(1, rawLogContext.Length - 2);
                if (!LogContexts.Contains(logContext)) LogContexts.Add(logContext);

                PrintLogEvent(logEvent);
            });
    }

    private void PrintLogEvent(LogEvent logEvent)
    {
        var rawLogContext = logEvent.Properties["Context"].ToString();
        var logContext = rawLogContext.Substring(1, rawLogContext.Length - 2); // Remove double-quote

        if (logEvent.Level < MinimumLevel) return;
        if (SelectedLogContext != "None" && logContext != SelectedLogContext) return;

        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);

        if (Lines.Count != 0) sb.AppendLine();
        if (logEvent.Exception is null) Formatter.Format(logEvent, sw);
        else
        {
            FormatterWithException.Format(logEvent, sw);
            sb.Append(logEvent.Exception.Message);
        }

        Lines.Add(new CustomRun(sb.ToString(), logEvent.Level));
    }

    private void RefreshLogs()
    {
        Lines.Clear();
        foreach (var log in Logging.logs.Logs) PrintLogEvent(log);
    }

    partial void OnMinimumLevelChanged(LogEventLevel value) => RefreshLogs();
    partial void OnSelectedLogContextChanged(string value) => RefreshLogs();
}
