using System;
using Microsoft.Build.Framework;
using TargetTask = Microsoft.Build.Utilities.Task;

namespace Oleander.Assembly.Versioning.BuildTask
{
    public class VersioningTask : TargetTask
    {
        [Required]
        public string TargetFileName { get; set; }
        public string ProjectDirName { get; set; }
        public string ProjectFileName { get; set; }
        public string GitRepositoryDirName { get; set; }

        [Output]
        public string Message { get; set; }

        //private readonly Versioning _versioning = new();

        public override bool Execute()
        {
            this.Message = $"[{DateTime.Now}] Hallo du! dornet2.1";

            if (this.TargetFileName == null) return false;

            //if (this.ProjectDirName != null && this.ProjectFileName != null && this.GitRepositoryDirName != null)
            //{
            //    this._versioning.UpdateAssemblyVersion(this.TargetFileName, this.ProjectDirName, this.ProjectFileName, this.GitRepositoryDirName);
            //}

            //if (this.ProjectDirName != null && this.ProjectFileName != null)
            //{
            //    this._versioning.UpdateAssemblyVersion(this.TargetFileName, this.ProjectDirName, this.ProjectFileName);
            //}

            //if (this.ProjectFileName != null)
            //{
            //    this._versioning.UpdateAssemblyVersion(this.TargetFileName, this.ProjectFileName);
            //}

            //else
            //{
            //    this._versioning.UpdateAssemblyVersion(this.TargetFileName);
            //}

            return true;
        }
    }
}
