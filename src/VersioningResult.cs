using Oleander.AssemblyVersioning.ExternalProcesses;

namespace Oleander.AssemblyVersioning;

public class VersioningResult
{
    public VersioningErrorCodes ErrorCode { get; internal set; } = VersioningErrorCodes.Success;

    public Version? CalculatedVersion { get; internal set; }

    public ExternalProcessResult? ExternalProcessResult { get; internal set; }
}