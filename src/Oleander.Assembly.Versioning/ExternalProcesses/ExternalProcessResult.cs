using System.Text;

namespace Oleander.Assembly.Versioning.ExternalProcesses
{
    public class ExternalProcessResult(string exeFileName, string arguments)
    {
        public string ExeFileName { get; } = Path.GetFileName(exeFileName);

        public string CommandLine { get; } = string.Concat(exeFileName, " ", arguments);

        public Win32ExitCodes Win32ExitCode { get; internal set; } = Win32ExitCodes.ERROR_SUCCESS;
        public int ExitCode { get; internal set; }
        public string? StandardOutput { get; internal set; }
        public string? StandardErrorOutput { get; internal set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("Cmd:".PadRight(20)).AppendLine(this.CommandLine);
            sb.Append("ExeFileName:".PadRight(20)).AppendLine(this.ExeFileName);
            sb.Append("ExitCode:".PadRight(20)).AppendLine(this.ExitCode.ToString());
            sb.Append("Win32ExitCode:".PadRight(20)).AppendLine(this.Win32ExitCode.ToString());
            
            if (!string.IsNullOrEmpty(this.StandardOutput)) sb.AppendLine(this.StandardOutput);
            if (!string.IsNullOrEmpty(this.StandardErrorOutput)) sb.AppendLine(this.StandardErrorOutput);

            return sb.ToString();
        }
    }
}