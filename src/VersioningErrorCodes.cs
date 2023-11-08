namespace Oleander.AssemblyVersioning;

public enum VersioningErrorCodes : int
{
    Success = 0,
    TargetFileNotExist = 1,
    TargetDirNotExist = 2,
    ProjectDirNotExist = 3,
    ProjectFileNotExist = 4,
    GitRepositoryDirNotExist = 5,
    GetGitHashFailed = 6,
    GetGitDiffNameOnlyFailed = 7,
}