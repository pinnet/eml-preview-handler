#Requires -RunAsAdministrator
<#
    Unregisters the preview handler (reverses register.ps1). Must be run elevated.
#>
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dll = Join-Path $root "bin\$Configuration\net48\EmlPreviewHandler.dll"

if (-not (Test-Path $dll)) {
    throw "Build output not found at $dll"
}

$regasm = Join-Path $env:windir "Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"
if (-not (Test-Path $regasm)) {
    throw "RegAsm.exe not found at $regasm - is .NET Framework 4.x installed?"
}

Write-Host "Unregistering $dll..." -ForegroundColor Cyan
& $regasm $dll /unregister
if ($LASTEXITCODE -ne 0) {
    throw "RegAsm /unregister failed with exit code $LASTEXITCODE"
}

Write-Host "Done." -ForegroundColor Green
