using Microsoft.Extensions.Logging;
using NuGet.Common;
using ILogger = NuGet.Common.ILogger;
using IMSLogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = NuGet.Common.LogLevel;

namespace Oleander.Assembly.Versioning;

public class NuGetLogger(IMSLogger logger) : ILogger
{
    public void LogDebug(string data)
    { 
        logger.LogDebug(data);
    }

    public void LogVerbose(string data)
    {
        logger.LogDebug(data);
    }

    public void LogInformation(string data)
    {
        logger.LogInformation(data);

    }

    public void LogMinimal(string data)
    {
        logger.LogDebug(data);
    }

    public void LogWarning(string data)
    {
        logger.LogWarning(data);
    }

    public void LogError(string data)
    {
        logger.LogError(data);

    }

    public void LogInformationSummary(string data)
    {
        logger.LogError(data);
    }

    public void Log(LogLevel level, string data)
    {
        switch (level)
        {
            case LogLevel.Debug:
            case LogLevel.Verbose:
            case LogLevel.Minimal:
                this.LogDebug(data);
                break;
            case LogLevel.Information:
                this.LogInformation(data);
                break;
            case LogLevel.Warning:
                this.LogWarning(data);
                break;
            case LogLevel.Error:
                this.LogError(data);
                break;
        }
    }

    public Task LogAsync(LogLevel level, string data)
    {
        return Task.Run(() => { this.Log(level, data); });
    }

    public void Log(ILogMessage message)
    {
        switch (message.Level)
        {
            case LogLevel.Debug:
            case LogLevel.Verbose:
            case LogLevel.Minimal:
                if (message.ProjectPath == null)
                {
                    logger.LogDebug("{time} {message} ", message.Time, message.Message);
                }
                else
                {
                    logger.LogDebug("{time} {message} {projectPath} ", message.Time, message.Message, message.ProjectPath);
                }
                break;
            case LogLevel.Information:
                if (message.ProjectPath == null)
                {
                    logger.LogInformation("{time} {message} ", message.Time, message.Message);
                }
                else
                {
                    logger.LogInformation("{time} {message} {projectPath} ", message.Time, message.Message, message.ProjectPath);
                }
                break;
            case LogLevel.Warning:
                if (message.ProjectPath == null)
                {
                    logger.LogWarning("{time} {message} {warningLevel}", message.Time, message.Message, message.WarningLevel);
                }
                else
                {
                    logger.LogWarning("{time} {message} {warningLevel} {projectPath} ", message.Time, message.Message, message.WarningLevel, message.ProjectPath);
                }
                break;
            case LogLevel.Error:
                if (message.ProjectPath == null)
                {
                    logger.LogWarning("{time} {message} {warningLevel}", message.Time, message.Message, message.WarningLevel);
                }
                else
                {
                    logger.LogError("{time} {message} {projectPath} ", message.Time, message.Message, message.ProjectPath);
                }
                break;
        }
    }

    public Task LogAsync(ILogMessage message)
    {
        return Task.Run(() => this.Log(message));
    }
}