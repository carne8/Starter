using System.Collections.ObjectModel;
using System.Text;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using R3;
using Serilog.Events;
using Serilog.Formatting.Display;
using Starter.Features;

namespace Starter.Desktop.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private const string Template = "[{Timestamp:HH:mm:ss} {Level:u3}] [{Context}] {Message:lj}";
    private const string TemplateWithException = "[{Timestamp:HH:mm:ss} {Level:u3}] [{Context}] {Message:lj}{NewLine}{Exception}";
    private static readonly MessageTemplateTextFormatter Formatter = new(Template);
    private static readonly MessageTemplateTextFormatter FormatterWithException = new(TemplateWithException);

    private static readonly Dictionary<LogEventLevel, IBrush> Brushes = new([
            new KeyValuePair<LogEventLevel, IBrush>(LogEventLevel.Verbose, new SolidColorBrush()),
            new KeyValuePair<LogEventLevel, IBrush>(LogEventLevel.Debug, new SolidColorBrush(new Color(255, 80, 161, 79))),
            new KeyValuePair<LogEventLevel, IBrush>(LogEventLevel.Information, new SolidColorBrush(new Color(255, 1, 132, 188))),
            new KeyValuePair<LogEventLevel, IBrush>(LogEventLevel.Warning, new SolidColorBrush(new Color(255, 193, 131, 1))),
            new KeyValuePair<LogEventLevel, IBrush>(LogEventLevel.Error, new SolidColorBrush(new Color(255, 228, 86, 73))),
            new KeyValuePair<LogEventLevel, IBrush>(LogEventLevel.Fatal, new SolidColorBrush(new Color(255, 166, 38, 164)))
        ]
    );

    public static readonly LogEventLevel[] LogLevels =
    [
        LogEventLevel.Verbose,
        LogEventLevel.Debug,
        LogEventLevel.Information,
        LogEventLevel.Warning,
        LogEventLevel.Error,
        LogEventLevel.Fatal
    ];

    [ObservableProperty] private LogEventLevel minimumLevel = LogEventLevel.Information;
    [ObservableProperty] private ObservableCollection<Inline> lines = [];
    [ObservableProperty] private bool wrapText;
    [ObservableProperty] private string selectedLogContext = "None";
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
        else FormatterWithException.Format(logEvent, sw);

        Lines.Add(new Run(sb.ToString()) { Foreground = Brushes[logEvent.Level] });
    }

    private void RefreshLogs()
    {
        Lines.Clear();
        foreach (var log in Logging.logs.Logs) PrintLogEvent(log);
    }

    partial void OnMinimumLevelChanged(LogEventLevel value) => RefreshLogs();
    partial void OnSelectedLogContextChanged(string value) => RefreshLogs();
}
