

set solutionDir=%1
set configuration=%2
set targetPath=%3

set exePath="%solutionDir%src\Oleander.Assembly.Versioning\bin\%configuration%\net7.0\Oleander.Assembly.Versioning.exe"
echo %exePath%

rem if exist %exePath%
rem call %exePath% "%targetPath%"
 