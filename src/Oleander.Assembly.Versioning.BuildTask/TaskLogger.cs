using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Oleander.Assembly.Versioning.BuildTask;

internal class TaskLogger(VersioningTask task) : ILogger
{
    private readonly List<string> _logs = new List<string>();

    void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var msg = formatter(state, exception);

        if (((ILogger)this).IsEnabled(logLevel))
        {
            this._logs.Add($"{this._logs.Count + 1,3} {DateTime.Now:yyyy.MM.dd HH:mm:ss} - {logLevel,12} - {msg}");
        }
       
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

        var code = $"{eventId.Name}{eventId.Id:000}";
        const string file = "Oleander.Assembly.Versioning.BuildTask.dll";
        const string subcategory = "OAVBT";

        switch (logLevel)
        {
            case LogLevel.None:
                return;
            case LogLevel.Debug:
                task.Log.LogMessage(subcategory, code, null, file, 0, 0, 0, 0, MessageImportance.Low, msg);
                break;
            case LogLevel.Trace:
                task.Log.LogMessage(subcategory, code, null, file, 0, 0, 0, 0, MessageImportance.Normal, msg);
                break;
            case LogLevel.Information:
                task.Log.LogMessage(subcategory, code, null, file, 0, 0, 0, 0, MessageImportance.High, msg);
                break;
            case LogLevel.Warning:
                task.Log.LogWarning(subcategory, code, null, file, 0, 0, 0, 0, msg);
                break;
            case LogLevel.Critical:
            case LogLevel.Error:
                task.Log.LogError(subcategory, code, null, file, 0, 0, 0, 0, msg);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public TaskLoggingHelper Log => task.Log;

    public string[] GetLogs()
    {
        var logs = this._logs.ToArray();
        this._logs.Clear();
        return logs;
    }

    bool ILogger.IsEnabled(LogLevel logLevel)
    {
        #if DEBUG1
        return true;
        #else
        return logLevel
            is LogLevel.Information
            or LogLevel.Warning
            or LogLevel.Error
            or LogLevel.Critical;
        #endif
    }

    IDisposable? ILogger.BeginScope<TState>(TState state)
    {
        return null;
    }
}