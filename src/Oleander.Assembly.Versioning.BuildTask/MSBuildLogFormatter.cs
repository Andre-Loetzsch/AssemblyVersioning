using System.Runtime.CompilerServices;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable InconsistentNaming

namespace Oleander.Assembly.Versioning.BuildTask;

internal static class MSBuildLogFormatter
{
    public static void CreateMSBuildWarning(this TaskLogger logger, string code, string text, string subCategory, [CallerLineNumber] int line = 0)
    {
        logger.Log.LogWarning(subcategory: "OAVT",
            warningCode: $"OAVT{code}",
            helpKeyword: null,
            file: string.Empty,
            lineNumber: line,
            columnNumber: 0,
            endLineNumber: 0,
            endColumnNumber: 0,
            message: text);
    }


    public static void CreateMSBuildError(this TaskLogger logger, string code, string text, int line, string subCategory)
    {
        CreateMSBuildError(logger, code, text, subCategory, line);
    }

    public static void CreateMSBuildError(this TaskLogger logger, string code, string text, string subCategory, [CallerLineNumber] int line = 0)
    {
        logger.Log.LogError(subcategory: "OAVT",
            errorCode: $"OAVT:{code}",
            helpKeyword: null,
            file: FileName,
            lineNumber: line,
            columnNumber: 0,
            endLineNumber: 0,
            endColumnNumber: 0,
            message: text);
    }


    private static string FileName => Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
}