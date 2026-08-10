using System.Collections.ObjectModel;
using System.Text;
using Avalonia.Controls.Documents;
using R3;
using Serilog.Events;
using Serilog.Formatting.Display;
using Starter.Features;

namespace Starter.ViewModels;

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
    [ObservableProperty] public partial string[] LogContexts { get; set; } = [ "None" ];

    public LogsViewModel()
    {
        var start = new string(char.MinValue, "Starter".Length);

        Logging.logs
            .ObserveOnUIThreadDispatcher()
            .Subscribe(this, (logEvent, t) =>
            {
                // Register log context
                var rawLogContext = logEvent.Properties["Context"].ToString();
                var logContext = rawLogContext.Substring(1, rawLogContext.Length - 2);
                if (!LogContexts.Contains(logContext))
                {
                    LogContexts = LogContexts
                        .Append(logContext)
                        .OrderBy(c =>
                        {
                            if (c == "None") return string.Empty;
                            if (c.StartsWith("Starter")) return start + c["Starter".Length..];
                            return start + c;
                        })
                        .ToArray();
                }

                t.PrintLogEvent(logEvent);
            });
    }

    private static Run CreateRun(string message, LogEventLevel level)
    {
        var run = new Run(message);
        run.Classes.Add(level switch
        {
            LogEventLevel.Verbose => "log-verbose",
            LogEventLevel.Debug => "log-debug",
            LogEventLevel.Information => "log-information",
            LogEventLevel.Warning => "log-warning",
            LogEventLevel.Error => "log-error",
            LogEventLevel.Fatal => "log-fatal",
            _ => string.Empty
        });

        return run;
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

        Lines.Add(CreateRun(sb.ToString(), logEvent.Level));
    }

    private void RefreshLogs()
    {
        Lines.Clear();
        foreach (var log in Logging.logs.Logs) PrintLogEvent(log);
    }

    partial void OnMinimumLevelChanged(LogEventLevel value) => RefreshLogs();
    partial void OnSelectedLogContextChanged(string value) => RefreshLogs();
}
