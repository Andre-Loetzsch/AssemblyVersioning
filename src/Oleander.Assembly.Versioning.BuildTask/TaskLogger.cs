using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Oleander.Assembly.Versioning.BuildTask;

internal class TaskLogger : ILogger
{
    private readonly VersioningTask _task;
    private string? _logFilePath;

    public TaskLogger(VersioningTask task)
    {
        this._task = task;
        this.InitNewSession();
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var msg = formatter(state, exception);

        if (this._task.ProjectDirName != null)
        {
            var path = Path.Combine(this._task.ProjectDirName, ".versioning", "cache");

            if (Directory.Exists(path))
            {
                this._logFilePath = Path.Combine(path, "versioning.log");
                File.AppendAllText(this._logFilePath, $"[{DateTime.Now:yyyy.MM.dd HH:mm:ss}] - {logLevel,12} - {msg}{Environment.NewLine}");
            }
        }

        switch (logLevel)
        {
            case LogLevel.None:
                return;
            case LogLevel.Debug:
            case LogLevel.Trace:
                this._task.Log.LogMessage(MessageImportance.Low, msg);
                MSBuildLogFormatter.CreateMSBuildMessage("AVTL", msg, "VersioningTask");
                break;
            case LogLevel.Information:
                this._task.Log.LogMessage(MessageImportance.Normal, msg);
                MSBuildLogFormatter.CreateMSBuildMessage("AVTN", msg, "VersioningTask");
                break;
            case LogLevel.Warning:
                this._task.Log.LogWarning(msg);
                MSBuildLogFormatter.CreateMSBuildWarning("AVTW", msg, "VersioningTask");
                break;
            case LogLevel.Critical:
            case LogLevel.Error:
                this._task.Log.LogError(msg);
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
                return this._task.Log.LogsMessagesOfImportance(MessageImportance.Low);
            case LogLevel.Information:
                return this._task.Log.LogsMessagesOfImportance(MessageImportance.Normal);
            case LogLevel.Warning:
            case LogLevel.Critical:
            case LogLevel.Error:
                return this._task.Log.LogsMessagesOfImportance(MessageImportance.High);
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    private void InitNewSession()
    {
        if (this._logFilePath == null && this._task.ProjectDirName != null)
        {
            this._logFilePath = Path.Combine(this._task.ProjectDirName, ".versioning", "cache", "versioning.log");
        }

        if (this._logFilePath == null) return;
        
        File.WriteAllText(this._logFilePath, $"[{DateTime.Now:yyyy.MM.dd HH:mm:ss}] - Information - New session created. { Process.GetCurrentProcess().ProcessName } { Process.GetCurrentProcess().Id }{Environment.NewLine}");
    }
}