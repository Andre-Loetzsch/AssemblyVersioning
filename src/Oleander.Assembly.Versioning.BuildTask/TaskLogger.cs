using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Oleander.Assembly.Versioning.BuildTask;

public class TaskLogger(VersioningTask task) : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var msg = formatter(state, exception);

        if (task.ProjectDirName != null)
        {
            var path = Path.Combine(task.ProjectDirName, ".versioning", "cache");
            
            
            if (Directory.Exists(path))
            {
                File.AppendAllText(Path.Combine(path, "versioning.log"), $"[{DateTime.Now:yyyy.MM.dd HH:mm:ss}] {logLevel,12} {msg}");
            }
            else
            {
            }
        }

        File.AppendAllText(Path.Combine("D:\\dev\\git\\oleander\\AssemblyVersioning", "versioning.log"), $"[{DateTime.Now:yyyy.MM.dd HH:mm:ss}] {logLevel,12} {msg} - {task.ProjectDirName}");


        switch (logLevel)
        {
            case LogLevel.None:
                return;
            case LogLevel.Debug:
            case LogLevel.Trace:
                task.Log.LogMessage(MessageImportance.Low, msg);
                MSBuildLogFormatter.CreateMSBuildMessage("AVTL", msg, "VersioningTask");
                break;
            case LogLevel.Information:
                task.Log.LogMessage(MessageImportance.Normal, msg);
                MSBuildLogFormatter.CreateMSBuildMessage("AVTN", msg, "VersioningTask");
                break;
            case LogLevel.Warning:
                task.Log.LogWarning(msg);
                MSBuildLogFormatter.CreateMSBuildWarning("AVTW", msg, "VersioningTask");
                break;
            case LogLevel.Critical:
            case LogLevel.Error:
                task.Log.LogError(msg);
                MSBuildLogFormatter.CreateMSBuildError("AVTE", msg, "VersioningTask");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        switch (logLevel)
        {
            case LogLevel.None:
            case LogLevel.Debug:
            case LogLevel.Trace:
                return task.Log.LogsMessagesOfImportance(MessageImportance.Low);
            case LogLevel.Information:
                return task.Log.LogsMessagesOfImportance(MessageImportance.Normal);
            case LogLevel.Warning:
            case LogLevel.Critical:
            case LogLevel.Error:
                return task.Log.LogsMessagesOfImportance(MessageImportance.High);
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
}