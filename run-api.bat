@echo off
cd /d "d:\Projects\AadharLocationApp"
dotnet run --project src\AadharLocation.Api\AadharLocation.Api.csproj --launch-profile http
pause
