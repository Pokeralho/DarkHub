#requires -Version 5.1

[CmdletBinding()]
param(
    [string] $RuntimeUrl = "https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe"
)

$ErrorActionPreference = "Stop"

$runtimeRoot = Join-Path $env:ProgramFiles "dotnet\shared\Microsoft.WindowsDesktop.App"
if (Test-Path -LiteralPath $runtimeRoot) {
    $installedRuntime = Get-ChildItem -LiteralPath $runtimeRoot -Directory -Filter "8.*" -ErrorAction SilentlyContinue |
        Select-Object -First 1

    if ($installedRuntime) {
        Write-Host ".NET 8 Desktop Runtime already installed: $($installedRuntime.Name)"
        exit 0
    }
}

$installerPath = Join-Path $env:TEMP "windowsdesktop-runtime-8.0-win-x64.exe"
Write-Host "Downloading .NET 8 Desktop Runtime from $RuntimeUrl"
Invoke-WebRequest -Uri $RuntimeUrl -OutFile $installerPath -UseBasicParsing

Write-Host "Installing .NET 8 Desktop Runtime"
$process = Start-Process -FilePath $installerPath -ArgumentList "/install", "/quiet", "/norestart" -Wait -PassThru
exit $process.ExitCode
