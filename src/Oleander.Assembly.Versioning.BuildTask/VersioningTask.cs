using Microsoft.Build.Framework;
using TargetTask = Microsoft.Build.Utilities.Task;
using NuGet.Common;

namespace Oleander.Assembly.Versioning.BuildTask
{
    public class VersioningTask : TargetTask
    {
        [Required]
        public string? TargetFileName { get; set; }
        public string? ProjectDirName { get; set; }
        public string? ProjectFileName { get; set; }
        public string? GitRepositoryDirName { get; set; }

        private readonly Versioning _versioning;

        public VersioningTask()
        {
            this._versioning = new(new TaskLogger(this));
        }


        public override bool Execute()
        {
            VersioningResult result;
          
            if (this.TargetFileName == null) return false;

            if (this.ProjectDirName != null && this.ProjectFileName != null && this.GitRepositoryDirName != null)
            {
                result = this._versioning.UpdateAssemblyVersion(this.TargetFileName, this.ProjectDirName, this.ProjectFileName, this.GitRepositoryDirName);
            }
            else if (this.ProjectDirName != null && this.ProjectFileName != null)
            {
                result = this._versioning.UpdateAssemblyVersion(this.TargetFileName, this.ProjectDirName, this.ProjectFileName);
            }
            else if (this.ProjectFileName != null)
            {
                result = this._versioning.UpdateAssemblyVersion(this.TargetFileName, this.ProjectFileName);
            }
            else 
            {
                result = this._versioning.UpdateAssemblyVersion(this.TargetFileName);
            }

            if (result.ErrorCode != VersioningErrorCodes.Success)
            {
                this.Log.LogError(subcategory: "OAVT",
                    errorCode: $"OAVT:{(int)result.ErrorCode:000}",
                    helpKeyword: null,
                    file: string.Empty,
                    lineNumber: 0,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: $"Error: {result.ErrorCode}");

                return false;
            }

            if (result.ExternalProcessResult != null && result.ExternalProcessResult.ExitCode != 0)
            {
                this.Log.LogError(subcategory: "OAVT",
                    errorCode: $"OAVT:{result.ExternalProcessResult.ExitCode:000}",
                    helpKeyword: null,
                    file: string.Empty,
                    lineNumber: 0,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: result.ExternalProcessResult.ToString());

                return false;
            }

            this.Log.LogMessage(MessageImportance.Normal, $"OAVT: Version: {result.CalculatedVersion}");
            MSBuildLogFormatter.CreateMSBuildMessage("AVTM", $"VersioningTask -> {result}", "VersioningTask");

            return true;
        }
    }
}
