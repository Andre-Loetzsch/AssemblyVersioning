﻿using Microsoft.Extensions.Logging;
using NuGet.Common;

namespace Oleander.Assembly.Versioning.NuGet;

internal class NuGetLogger(ILogger logger) : INuGetLogger, ILogger
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

    public void Log(NuGetLogLevel level, string data)
    {
        switch (level)
        {
            case NuGetLogLevel.Debug:
            case NuGetLogLevel.Verbose:
            case NuGetLogLevel.Minimal:
                this.LogDebug(data);
                break;
            case NuGetLogLevel.Information:
                this.LogInformation(data);
                break;
            case NuGetLogLevel.Warning:
                this.LogWarning(data);
                break;
            case NuGetLogLevel.Error:
                this.LogError(data);
                break;
        }
    }

    public Task LogAsync(NuGetLogLevel level, string data)
    {
        return Task.Run(() => { this.Log(level, data); });
    }

    public void Log(ILogMessage message)
    {
        switch (message.Level)
        {
            case NuGetLogLevel.Debug:
            case NuGetLogLevel.Verbose:
            case NuGetLogLevel.Minimal:
                if (message.ProjectPath == null)
                {
                    logger.LogDebug("{time} {message} ", message.Time, message.Message);
                }
                else
                {
                    logger.LogDebug("{time} {message} {projectPath} ", message.Time, message.Message, message.ProjectPath);
                }
                break;
            case NuGetLogLevel.Information:
                if (message.ProjectPath == null)
                {
                    logger.LogInformation("{time} {message} ", message.Time, message.Message);
                }
                else
                {
                    logger.LogInformation("{time} {message} {projectPath} ", message.Time, message.Message, message.ProjectPath);
                }
                break;
            case NuGetLogLevel.Warning:
                if (message.ProjectPath == null)
                {
                    logger.LogWarning("{time} {message} {warningLevel}", message.Time, message.Message, message.WarningLevel);
                }
                else
                {
                    logger.LogWarning("{time} {message} {warningLevel} {projectPath} ", message.Time, message.Message, message.WarningLevel, message.ProjectPath);
                }
                break;
            case NuGetLogLevel.Error:
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

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        logger.Log(logLevel, eventId, state, exception, formatter);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logger.IsEnabled(logLevel);
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return logger.BeginScope(state);
    }
}