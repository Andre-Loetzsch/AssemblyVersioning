rem dotnet remove package Oleander.Assembly.Versioning.BuildTask
dotnet add package Oleander.Assembly.Versioning.BuildTask -s %~dp0\..\..\src\Oleander.Assembly.Versioning.BuildTask\bin\Debug --prerelease
rem dotnet restore -f
pause

