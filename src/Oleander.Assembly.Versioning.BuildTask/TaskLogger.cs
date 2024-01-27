using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Oleander.Assembly.Versioning.BuildTask;

internal class TaskLogger(VersioningTask task) : ILogger
{
    private readonly List<string> _logs = new List<string>();
    private readonly List<string> _stackTrace = new List<string>();

    void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var msg = formatter(state, exception);
        var log = $"{this._logs.Count + 1,3} {DateTime.Now:yyyy.MM.dd HH:mm:ss} - {logLevel,12} - {msg}";

        if (logLevel != LogLevel.Trace) this._stackTrace.Add(log);
        if (!((ILogger)this).IsEnabled(logLevel)) return;

        this._logs.Add(log);

        if (string.IsNullOrEmpty(eventId.Name))
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                case LogLevel.Trace:
                    eventId = new EventId(eventId.Id, EventIds.DebugEventName);
                    break;
                case LogLevel.Information:
                    eventId = new EventId(eventId.Id, EventIds.InfoEventName);
                    break;
                case LogLevel.Warning:
                    eventId = new EventId(eventId.Id, EventIds.InfoEventName);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    eventId = new EventId(eventId.Id, EventIds.ErrorEventName);
                    break;
            }
        }

        var code = $"OAVBT{eventId.Id:00}";
        const string file = "Oleander.Assembly.Versioning.BuildTask.dll";

        switch (logLevel)
        {
            case LogLevel.None:
                return;
            case LogLevel.Trace:
                task.Log.LogMessage(eventId.Name, code, null, file, 0, 0, 0, 0, MessageImportance.Low, msg);
                break;
            case LogLevel.Debug:
                task.Log.LogMessage(eventId.Name, code, null, file, 0, 0, 0, 0, MessageImportance.Normal, msg);
                break;
            case LogLevel.Information:
                task.Log.LogMessage(eventId.Name, code, null, file, 0, 0, 0, 0, MessageImportance.High, msg);
                break;
            case LogLevel.Warning:
                task.Log.LogWarning(eventId.Name, code, null, file, 0, 0, 0, 0, msg);
                break;
            case LogLevel.Critical:
            case LogLevel.Error:
                task.Log.LogError(eventId.Name, code, null, file, 0, 0, 0, 0, msg);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public string[] GetLogs()
    {
        var logs = this._logs.ToArray();
        this._logs.Clear();
        this._stackTrace.Clear();
        return logs;
    }

    public string[] GetStackTrace()
    {
        var logs = this._stackTrace.ToArray();
        this._stackTrace.Clear();
        return logs;
    }

    public LogLevel LogFilter { get; set; }

    bool ILogger.IsEnabled(LogLevel logLevel)
    {
        return this.LogFilter switch
        {
            LogLevel.None => false,
            LogLevel.Debug => logLevel != LogLevel.Trace,
            LogLevel.Information => logLevel is LogLevel.Information or LogLevel.Warning or LogLevel.Error or LogLevel.Critical,
            LogLevel.Warning => logLevel is LogLevel.Warning or LogLevel.Error or LogLevel.Critical,
            LogLevel.Error => logLevel is LogLevel.Error or LogLevel.Critical,
            LogLevel.Critical => logLevel == LogLevel.Critical,
            _ => true
        };
    }

    IDisposable? ILogger.BeginScope<TState>(TState state)
    {
        return null;
    }
}