<#
.SYNOPSIS
    Publishes both WPF apps and compiles the Inno Setup installer.
.PARAMETER Version
    The version string embedded in the installer (default: 1.0.0).
.PARAMETER SkipPublish
    Skip dotnet publish and use whatever is already in publish\Admin and publish\Operator.
.EXAMPLE
    .\build-installer.ps1
    .\build-installer.ps1 -Version 1.2.0
    .\build-installer.ps1 -SkipPublish
#>
param(
    [string]$Version = "1.0.0",
    [switch]$SkipPublish
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$InstallerDir  = $PSScriptRoot
$RepoRoot      = Split-Path $InstallerDir -Parent
$SrcDir        = Join-Path $RepoRoot "src"
$PublishAdmin  = Join-Path $InstallerDir "publish\Admin"
$PublishOp     = Join-Path $InstallerDir "publish\Operator"
$OutputDir     = Join-Path $InstallerDir "output"
$ISCC          = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

function Write-Step([string]$msg) {
    Write-Host "`n==> $msg" -ForegroundColor Cyan
}

# ── 1. Verify Inno Setup is installed ────────────────────────────────────────
if (-not (Test-Path $ISCC)) {
    Write-Error @"
Inno Setup 6 not found at:
  $ISCC

Download and install it from https://jrsoftware.org/isdl.php, then re-run this script.
"@
}

# ── 2. Publish both WPF apps ─────────────────────────────────────────────────
if (-not $SkipPublish) {
    Write-Step "Publishing Admin Dashboard..."
    dotnet publish "$SrcDir\AadharLocation.AdminDashboard\AadharLocation.AdminDashboard.csproj" `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        --output "$PublishAdmin"
    if ($LASTEXITCODE -ne 0) { Write-Error "Admin Dashboard publish failed (exit $LASTEXITCODE)" }

    Write-Step "Publishing Operator Tracker..."
    dotnet publish "$SrcDir\AadharLocation.OperatorTracker\AadharLocation.OperatorTracker.csproj" `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        --output "$PublishOp"
    if ($LASTEXITCODE -ne 0) { Write-Error "Operator Tracker publish failed (exit $LASTEXITCODE)" }
} else {
    Write-Step "Skipping publish (-SkipPublish flag set)"
}

# ── 3. Verify publish output exists ──────────────────────────────────────────
foreach ($dir in @($PublishAdmin, $PublishOp)) {
    if (-not (Test-Path $dir)) {
        Write-Error "Expected publish output not found: $dir`nRun without -SkipPublish first."
    }
}

# ── 4. Compile the installer ─────────────────────────────────────────────────
Write-Step "Compiling Inno Setup installer (v$Version)..."
New-Item -ItemType Directory -Force $OutputDir | Out-Null

& $ISCC `
    "/DMyAppVersion=$Version" `
    "/O$OutputDir" `
    "$InstallerDir\setup.iss"

if ($LASTEXITCODE -ne 0) { Write-Error "Inno Setup compilation failed (exit $LASTEXITCODE)" }

$Artifact = Join-Path $OutputDir "AadharLocationSetup.exe"
Write-Host "`nInstaller ready: $Artifact" -ForegroundColor Green
