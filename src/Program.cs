namespace Oleander.AssemblyVersioning;

internal class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0) return -1;
        var versioning = new Versioning(args[0]);
        versioning.CalculateAssemblyVersion();
        var exitCode = versioning.ExternalProcessResult?.ExitCode ?? 0;
        return exitCode != 0 ? exitCode :  (int)versioning.ErrorCode;
    }
}