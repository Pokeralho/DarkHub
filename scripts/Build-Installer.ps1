#requires -Version 5.1

[CmdletBinding()]
param(
    [string] $Version = "1.1.9",

    [switch] $SkipSigning,

    [string] $SignToolPath = $env:SIGNTOOL_PATH,

    [string] $CertificatePath = $env:DARKHUB_SIGN_CERT_PATH,

    [string] $CertificatePassword = $env:DARKHUB_SIGN_CERT_PASSWORD,

    [string] $CertificateThumbprint = $env:DARKHUB_SIGN_CERT_THUMBPRINT,

    [string] $TimestampUrl = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"

function Resolve-Iscc {
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\Antigravity\resources\app\node_modules\innosetup\bin\ISCC.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    )

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return $candidate
        }
    }

    throw "ISCC.exe was not found. Install Inno Setup 6 or add ISCC.exe to PATH."
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
$publishDir = Join-Path $repoRoot "redist\publish\win-x64"
$installerDir = Join-Path $repoRoot "redist\installer"
$installerScript = Join-Path $repoRoot "installer\DarkHub.iss"
$setupPath = Join-Path $installerDir "DarkHub.Setup.exe"

$releaseScript = Join-Path $PSScriptRoot "Build-SignedRelease.ps1"
if (Test-Path -LiteralPath $publishDir) {
    $resolvedPublishDir = (Resolve-Path -LiteralPath $publishDir).Path
    if (-not $resolvedPublishDir.StartsWith($repoRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean publish directory outside the repository: $resolvedPublishDir"
    }

    Remove-Item -LiteralPath $resolvedPublishDir -Recurse -Force
}

if ($SkipSigning) {
    & $releaseScript -Configuration Release -Runtime win-x64 -OutputDir $publishDir -SkipSigning
}
else {
    & $releaseScript -Configuration Release -Runtime win-x64 -OutputDir $publishDir
}

$logsDir = Join-Path $publishDir "logs"
if (Test-Path -LiteralPath $logsDir) {
    Remove-Item -LiteralPath $logsDir -Recurse -Force
}

Get-ChildItem -LiteralPath $publishDir -Recurse -File -Filter "*.pdb" -ErrorAction SilentlyContinue |
    Remove-Item -Force

New-Item -ItemType Directory -Force -Path $installerDir | Out-Null

$iscc = Resolve-Iscc
Write-Host "Building Inno Setup installer with $iscc"
& $iscc "/DAppVersion=$Version" $installerScript
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed."
}

if (-not (Test-Path -LiteralPath $setupPath)) {
    throw "Expected setup was not produced: $setupPath"
}

if ($SkipSigning) {
    Write-Warning "Installer completed without Authenticode signing because -SkipSigning was used."
}
else {
    $signTool = Resolve-SignTool -ExplicitPath $SignToolPath
    Invoke-CodeSign -SignTool $signTool -FilePath $setupPath
}

Write-Host "Installer output: $setupPath"
