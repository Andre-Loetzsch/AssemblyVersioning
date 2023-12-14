using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Task = Microsoft.Build.Utilities.Task;

namespace Oleander.Assembly.Versioning.BuildTask;

public class TaskLogger(Task targetTask) : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var msg = formatter(state, exception);

        switch (logLevel)
        {
            case LogLevel.None:
                return;
            case LogLevel.Debug:
            case LogLevel.Trace:
                targetTask.Log.LogMessage(MessageImportance.Low, msg);
                break;
            case LogLevel.Information:
                targetTask.Log.LogMessage(MessageImportance.Normal, msg);
                break;
            case LogLevel.Warning:
            case LogLevel.Critical:
                targetTask.Log.LogWarning(msg);
                break;
            case LogLevel.Error:
                targetTask.Log.LogError(msg);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Information;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
}