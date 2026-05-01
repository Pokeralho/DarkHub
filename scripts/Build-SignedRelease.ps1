#requires -Version 5.1

[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",

    [string] $Runtime = "win-x64",

    [string] $OutputDir,

    [string] $CertificatePath = $env:DARKHUB_SIGN_CERT_PATH,

    [string] $CertificatePassword = $env:DARKHUB_SIGN_CERT_PASSWORD,

    [string] $CertificateThumbprint = $env:DARKHUB_SIGN_CERT_THUMBPRINT,

    [string] $TimestampUrl = "http://timestamp.digicert.com",

    [string] $SignToolPath = $env:SIGNTOOL_PATH,

    [switch] $SelfContained,

    [switch] $SkipSigning
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $PSScriptRoot "..\redist\publish\win-x64"
}

function Resolve-SignTool {
    param([string] $ExplicitPath)

    if ($ExplicitPath -and (Test-Path -LiteralPath $ExplicitPath)) {
        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }

    $command = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $windowsKitRoots = @(
        (Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"),
        (Join-Path $env:ProgramFiles "Windows Kits\10\bin")
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

    $tools = foreach ($root in $windowsKitRoots) {
        Get-ChildItem -LiteralPath $root -Filter signtool.exe -Recurse -ErrorAction SilentlyContinue
    }

    $preferredTool = $tools |
        Sort-Object @{ Expression = { $_.FullName -notmatch "\\x64\\signtool\.exe$" } }, FullName -Descending |
        Select-Object -First 1

    if (-not $preferredTool) {
        throw "signtool.exe was not found. Install the Windows SDK or set SIGNTOOL_PATH."
    }

    return $preferredTool.FullName
}

function Invoke-CodeSign {
    param(
        [string] $SignTool,
        [string] $FilePath
    )

    $signArgs = @("sign", "/fd", "SHA256", "/tr", $TimestampUrl, "/td", "SHA256")

    if ($CertificatePath) {
        if (-not (Test-Path -LiteralPath $CertificatePath)) {
            throw "Certificate file not found: $CertificatePath"
        }

        $signArgs += @("/f", (Resolve-Path -LiteralPath $CertificatePath).Path)
        if ($CertificatePassword) {
            $signArgs += @("/p", $CertificatePassword)
        }
    }
    elseif ($CertificateThumbprint) {
        $signArgs += @("/sha1", $CertificateThumbprint)
    }
    else {
        throw "Set DARKHUB_SIGN_CERT_PATH or DARKHUB_SIGN_CERT_THUMBPRINT, or pass -SkipSigning."
    }

    $signArgs += $FilePath
    & $SignTool @signArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Signing failed for $FilePath"
    }

    & $SignTool verify /pa /v $FilePath
    if ($LASTEXITCODE -ne 0) {
        throw "Signature verification failed for $FilePath"
    }
}

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "DarkHub.csproj"
$resolvedOutputDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDir)

New-Item -ItemType Directory -Force -Path $resolvedOutputDir | Out-Null

$selfContainedValue = if ($SelfContained) { "true" } else { "false" }
$publishArgs = @(
    "publish", $projectPath,
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", $selfContainedValue,
    "-p:PublishSingleFile=false",
    "-p:PublishReadyToRun=true",
    "-o", $resolvedOutputDir
)

Write-Host "Publishing DarkHub to $resolvedOutputDir"
dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

$darkHubExe = Join-Path $resolvedOutputDir "DarkHub.exe"
if (-not (Test-Path -LiteralPath $darkHubExe)) {
    throw "Expected executable was not produced: $darkHubExe"
}

if ($SkipSigning) {
    Write-Warning "Build completed without Authenticode signing because -SkipSigning was used."
}
else {
    $signTool = Resolve-SignTool -ExplicitPath $SignToolPath
    Invoke-CodeSign -SignTool $signTool -FilePath $darkHubExe
}

Write-Host "Release output: $resolvedOutputDir"
