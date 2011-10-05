@echo off
rem Deployment script for all environments
rem It publishes to ./Deployment folder and is expected to be run out of /build within the web project.

rem set variables
set MSBUILDPATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
set DEPLOYDIR=..\..\Deployment
set SLNFILE=..\..\Modules.UrlMapper.Solution\Unic.SitecoreCMS.Modules.UrlMapper.sln

echo %1

IF "%1"=="test" (
    goto :test
)


:test
%MSBUILDPATH% deploy.targets /t:Deploy /p:SolutionFile=%SLNFILE%;DeployDir=%DEPLOYDIR%;Environment=Test.Author;Configuration=Release

goto :END
    
:END
echo finished deploying
PAUSE
