@echo off
cd /d "%~dp0\RaktarNet.Web"
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
pause
