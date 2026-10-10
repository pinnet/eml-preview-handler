#Requires -RunAsAdministrator
<#
    Builds the preview handler and registers it with regasm so Explorer can load it.
    Must be run elevated, and with the same bitness as Explorer/prevhost.exe (x64 on
    modern Windows), since COM registration and the handler DLL are both x64 here.
#>
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "Building $Configuration..." -ForegroundColor Cyan
dotnet build $root -c $Configuration

$dll = Join-Path $root "bin\$Configuration\net48\EmlPreviewHandler.dll"
if (-not (Test-Path $dll)) {
    throw "Build output not found at $dll"
}

$regasm = Join-Path $env:windir "Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"
if (-not (Test-Path $regasm)) {
    throw "RegAsm.exe not found at $regasm - is .NET Framework 4.x installed?"
}

Write-Host "Registering $dll..." -ForegroundColor Cyan
& $regasm $dll /codebase /tlb:nul
if ($LASTEXITCODE -ne 0) {
    throw "RegAsm failed with exit code $LASTEXITCODE"
}

# Optional: render HTML mail in IE11 "Edge" document mode instead of IE7 quirks mode.
# Per-user, no elevation required on its own - harmless if prevhost.exe hosts other handlers too.
$featureControlKey = "HKCU:\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"
New-Item -Path $featureControlKey -Force | Out-Null
New-ItemProperty -Path $featureControlKey -Name "PREVHOST.EXE" -Value 11001 -PropertyType DWord -Force | Out-Null

Write-Host "Done. Enable 'Show preview handlers in preview pane' in Explorer's Folder Options if needed." -ForegroundColor Green
