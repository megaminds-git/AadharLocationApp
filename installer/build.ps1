# Build script: publish Admin + Operator, then compile Inno Setup installer
$ErrorActionPreference = "Stop"
$Version = "1.0.1"   # <-- update this for each release
$Root    = Split-Path $PSScriptRoot -Parent
$Publish = "$PSScriptRoot\publish"
$ISCC    = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

Write-Host "==> Cleaning previous publish output..." -ForegroundColor Cyan
if (Test-Path $Publish) { Remove-Item $Publish -Recurse -Force }
New-Item "$Publish\Admin"    -ItemType Directory | Out-Null
New-Item "$Publish\Operator" -ItemType Directory | Out-Null

Write-Host "==> Publishing Admin Dashboard..." -ForegroundColor Cyan
dotnet publish "$Root\src\AadharLocation.AdminDashboard\AadharLocation.AdminDashboard.csproj" `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output "$Publish\Admin" `
    /p:PublishSingleFile=false `
    /p:TrimSelf=false `
    /p:Version=$Version
if ($LASTEXITCODE -ne 0) { throw "Admin Dashboard publish failed (exit $LASTEXITCODE)" }

Write-Host "==> Publishing Operator Tracker..." -ForegroundColor Cyan
dotnet publish "$Root\src\AadharLocation.OperatorTracker\AadharLocation.OperatorTracker.csproj" `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output "$Publish\Operator" `
    /p:PublishSingleFile=false `
    /p:TrimSelf=false `
    /p:Version=$Version
if ($LASTEXITCODE -ne 0) { throw "Operator Tracker publish failed (exit $LASTEXITCODE)" }

Write-Host "==> Compiling Inno Setup installer..." -ForegroundColor Cyan
& $ISCC "$PSScriptRoot\setup.iss" "/DMyAppVersion=$Version"
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed (exit $LASTEXITCODE)" }

Write-Host ""
Write-Host "==> Done! Installer written to:" -ForegroundColor Green
Write-Host "    $PSScriptRoot\output\AadharLocationSetup.exe" -ForegroundColor Green
