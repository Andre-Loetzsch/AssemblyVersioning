using Oleander.Assembly.Comparers;
using Oleander.Assembly.Versioning.ExternalProcesses;

namespace Oleander.Assembly.Versioning;

internal class VersioningResult
{
    public VersioningErrorCodes ErrorCode { get; internal set; } = VersioningErrorCodes.Success;

    public Version? CalculatedVersion { get; internal set; }
    public VersionChange VersionChange { get; internal set; } = VersionChange.None;

    public ExternalProcessResult? ExternalProcessResult { get; internal set; }
}