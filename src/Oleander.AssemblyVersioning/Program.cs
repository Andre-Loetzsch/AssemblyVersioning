namespace Oleander.Assembly.Versioning;

internal class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0) return -1;
        var versioning = new Versioning();
        var result = versioning.UpdateAssemblyVersion(args[0]);
        var exitCode = result.ExternalProcessResult?.ExitCode ?? 0;
        return exitCode != 0 ? exitCode :  (int)result.ErrorCode;
    }
}