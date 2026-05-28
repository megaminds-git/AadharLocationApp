@echo off
cd /d "d:\Projects\AadharLocationApp"
dotnet run --project src\AadharLocation.AdminDashboard\AadharLocation.AdminDashboard.csproj --configuration Debug > dashboard.log 2>&1
echo Exit code: %ERRORLEVEL%
type dashboard.log
pause
