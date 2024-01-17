# AssemblyVersioning

Assembly versioning is a tool that calculates the next version of a dotnet assembly. 
To do this, the public API of the assembly is compared with the last (reference version) compiled version.



## Oleander.Assembly.Comparers
Based on Telerik's JustDecompile, the public API of two dotnet assemblies is compared. 

### Result:
- **VersionChange.None:** no changes
- **VersionChange.Revision:** not used here
- **VersionChange.Build:** changes do not affect the public API
- **VersionChange.Minor:** API has been changed but is backwards compatible
- **VersionChange.Major:** The API has been changed and is not backwards compatible (BreakingChange)

- **ToXml():** Changes in XML format

## Oleander.Assembly.Versioning
Includes the logic and infrastructure.

- **Tries to download the NuGet package with the highest version as a reference assembly**
- **Updates the project or AssemblyInfo.cs file**
    - AssemblyVersion: calculated version
    - FileVersion: calculated version
    - Version: calculated version + VersionSuffix
    - SourceRevisionId: git commit hash number 
    - VersionSuffix: alpha, beta

 - **Customizing**

     - **.gitdiff**<br><br> File containing the file extensions whose build number should be increased. Each line corresponds to a file extension.

     - **.versioningIgnore**<br><br>
     File in the project or git repository directory with the API names that should be ignored when calculating the version.


## Oleander.Assembly.Versioning.BuildTask
Microsoft.Build.Utilities.Task: Adds a target to MSBuild. To calculate the version for each build process, add the task to your project: **dotnet add package Oleander.Assembly.Versioning.BuildTask --version {version\}**

## Oleander.Assembly.Versioning.Tool
A dotnet tool for comparing dotnet assemblies and updating your project version.

dotnet tool install --global dotnet-oleander-versioning-tool

