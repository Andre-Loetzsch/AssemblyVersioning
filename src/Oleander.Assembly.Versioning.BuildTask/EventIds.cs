using Microsoft.Extensions.Logging;

namespace Oleander.Assembly.Versioning.BuildTask;

internal static class EventIds
{
    public const string ExceptionEventName = "EXC";
    public static EventId AnExceptionHasOccurred = new (1, ExceptionEventName);

    public const string DebugEventName = "DBG";
    public static EventId DebugTargetFileName = new(1, DebugEventName);
    public static EventId DebugProjectDirName = new(2, DebugEventName);
    public static EventId DebugProjectFileName = new(3, DebugEventName);
    public static EventId DebugGitRepositoryDirName = new(4, DebugEventName);

    public const string InfoEventName = "INF";
    public static EventId TaskStarted  = new (1, InfoEventName);
    public static EventId CalculatedVersion = new(2, InfoEventName);
    public static EventId TaskCompleted = new (3, InfoEventName);

    public const string WarningEventName = "WRN";
    public static EventId ProjectDirNotExist = new(1, WarningEventName);
    public static EventId PropertyTargetFileNameIsNull = new(2, WarningEventName);
    public static EventId PropertyProjectDirNameIsNull = new(3, WarningEventName);
    public static EventId PropertyProjectFileNameIsNull = new(4, WarningEventName);
    public static EventId PropertyGitRepositoryDirNameIsNull = new(5, WarningEventName);

    public const string ErrorEventName = "ERR";
    public static EventId VersioningCacheDirNotExist = new(1, WarningEventName);
    public static EventId VersioningFailed = new (2, ErrorEventName);
    public static EventId ExternalProcessFailed = new(3, ErrorEventName);
}