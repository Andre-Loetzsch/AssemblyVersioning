using System.Text;

namespace Oleander.Assembly.Versioning.Extensionss;

internal static class ExceptionExtensions
{
    public static string GetAllMessages(this Exception exception)
    {
        var sb = new StringBuilder();
        var ex = exception;

        while (ex != null)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append('[').Append(ex.GetType()).Append("] ").Append(ex.Message);
            ex = ex.InnerException;
        }

        return sb.ToString();
    }
}