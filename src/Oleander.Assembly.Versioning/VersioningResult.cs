using Oleander.Assembly.Versioning.ExternalProcesses;

namespace Oleander.Assembly.Versioning;

internal class VersioningResult
{
    internal string TargetFileName = string.Empty;
    internal string ProjectDirName = string.Empty;
    internal string ProjectFileName = string.Empty;
    internal string GitRepositoryDirName = string.Empty;
    internal string VersioningCacheDir = string.Empty;

    public VersioningErrorCodes ErrorCode { get; internal set; } = VersioningErrorCodes.Success;

    public Version? CalculatedVersion { get; internal set; }

    public ExternalProcessResult? ExternalProcessResult { get; internal set; }
}