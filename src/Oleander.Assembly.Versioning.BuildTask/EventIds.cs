using Microsoft.Extensions.Logging;

namespace Oleander.Assembly.Versioning.BuildTask;

internal static class EventIds
{
    public const string ExceptionEventName = "EXC";
    public static EventId AnExceptionHasOccurred = new (11, ExceptionEventName);

    public const string DebugEventName = "DBG";
    public static EventId DebugTargetFileName = new(21, DebugEventName);
    public static EventId DebugProjectDirName = new(22, DebugEventName);
    public static EventId DebugProjectFileName = new(23, DebugEventName);
    public static EventId DebugGitRepositoryDirName = new(24, DebugEventName);

    public const string InfoEventName = "INF";
    public static EventId TaskStarted  = new (31, InfoEventName);
    public static EventId CalculatedVersion = new(32, InfoEventName);
    public static EventId TaskCompleted = new (33, InfoEventName);


    public const string WarningEventName = "WRN";
    public static EventId ProjectDirNotExist = new(41, WarningEventName);
    public static EventId PropertyTargetFileNameIsNull = new(42, WarningEventName);
    public static EventId PropertyProjectDirNameIsNull = new(43, WarningEventName);
    public static EventId PropertyProjectFileNameIsNull = new(44, WarningEventName);
    public static EventId PropertyGitRepositoryDirNameIsNull = new(45, WarningEventName);
    public static EventId TaskDisabled = new(46, WarningEventName);
    public static EventId CalculatedBreakingChangesVersion = new (47, WarningEventName);

    public const string ErrorEventName = "ERR";
    public static EventId VersioningCacheDirNotExist = new(51, ErrorEventName);
    public static EventId VersioningFailed = new (52, ErrorEventName);
    public static EventId ExternalProcessFailed = new(53, ErrorEventName);
    public static EventId InvalidLogLevel = new(54, ErrorEventName);
}