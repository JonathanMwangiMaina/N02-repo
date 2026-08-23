<# 
.SYNOPSIS
    Cross-platform build script for BlackoutClause FPS (Windows PowerShell)

.DESCRIPTION
    Builds the solution, runs tests, and creates platform-specific artifacts.
    Run from games/blackoutclause-fps/ directory.

.PARAMETER Configuration
    Build configuration (Debug/Release). Default: Release

.PARAMETER Version
    Version string for assembly info. Default: 1.0.0

.EXAMPLE
    .\build.ps1 -Configuration Release -Version 1.0.0
#>

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'
$Solution = 'BlackoutClause.sln'
$ArtifactsDir = 'artifacts'

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Building BlackoutClause FPS - $Configuration v$Version" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Clean previous builds
if (Test-Path $ArtifactsDir) {
    Remove-Item $ArtifactsDir -Recurse -Force
}
New-Item -ItemType Directory -Path $ArtifactsDir -Force | Out-Null

# Restore
Write-Host "Restoring packages..." -ForegroundColor Yellow
dotnet restore $Solution

# Build Shared
Write-Host "Building Shared..." -ForegroundColor Yellow
dotnet build src/BlackoutClause.Shared/BlackoutClause.Shared.csproj -c $Configuration --no-restore -p:Version=$Version

# Build Server (NO TRIMMING - Godot uses reflection)
Write-Host "Building Server..." -ForegroundColor Yellow
dotnet publish src/BlackoutClause.Server/BlackoutClause.Server.csproj -c $Configuration -o "$ArtifactsDir/server" --no-restore -p:Version=$Version -p:PublishTrimmed=false

# Detect runtime
$Runtime = switch ($env:PROCESSOR_ARCHITECTURE) {
    'AMD64' { 'win-x64' }
    'ARM64' { 'win-arm64' }
    default { 'win-x64' }
}

Write-Host "Building Client for $Runtime..." -ForegroundColor Yellow
dotnet publish src/BlackoutClause.Client/BlackoutClause.Client.csproj -c $Configuration -r $Runtime --self-contained -o "$ArtifactsDir/client/$Runtime" --no-restore -p:Version=$Version

# Run tests
Write-Host "Running tests..." -ForegroundColor Yellow
dotnet test $Solution -c $Configuration --no-build --verbosity normal

Write-Host "==========================================" -ForegroundColor Green
Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "Artifacts in: $ArtifactsDir/" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green

# List artifacts
Get-ChildItem "$ArtifactsDir" -Recurse -File | Where-Object { 
    $_.Extension -in '.exe', '.dll', '' -and $_.Name -like 'BlackoutClause*' 
} | Select-Object -First 20 | Format-Table FullName, Length -AutoSize