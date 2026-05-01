#requires -Version 5.1

[CmdletBinding()]
param(
    [switch] $SkipMediaTools,

    [string] $ThirdPartyToolsDirectory,

    [string] $InstalledAssetsDirectory = (Join-Path ${env:ProgramFiles(x86)} "DarkHub\assets"),

    [string] $YtDlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe",

    [string] $FfmpegZipUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
)

$ErrorActionPreference = "Stop"

function Download-File {
    param(
        [string] $Url,
        [string] $Destination
    )

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Destination) | Out-Null

    $curl = Get-Command curl.exe -ErrorAction SilentlyContinue
    if ($curl) {
        & $curl.Source -L --fail --show-error --connect-timeout 30 --retry 3 --retry-delay 2 --speed-time 60 --speed-limit 1024 --output $Destination $Url
        if ($LASTEXITCODE -ne 0) {
            throw "Download failed: $Url"
        }
        return
    }

    Invoke-WebRequest -Uri $Url -OutFile $Destination
}

function Copy-IfFound {
    param(
        [string] $Root,
        [string] $FileName,
        [string] $Destination
    )

    $match = Get-ChildItem -LiteralPath $Root -Filter $FileName -Recurse -File -ErrorAction SilentlyContinue |
        Select-Object -First 1

    if ($match) {
        Copy-Item -LiteralPath $match.FullName -Destination $Destination -Force
        Write-Host "Restored $FileName"
    }
    else {
        Write-Warning "$FileName was not found under $Root"
    }
}

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$assetsDir = Join-Path $repoRoot "assets"
$cacheDir = Join-Path $repoRoot "redist\cache"

New-Item -ItemType Directory -Force -Path $assetsDir, $cacheDir | Out-Null

if (-not $SkipMediaTools) {
    $ytDlpPath = Join-Path $assetsDir "yt-dlp.exe"
    Write-Host "Downloading yt-dlp.exe"
    Download-File -Url $YtDlpUrl -Destination $ytDlpPath

    $ffmpegZipPath = Join-Path $cacheDir "ffmpeg-release-essentials.zip"
    $ffmpegExtractDir = Join-Path $cacheDir "ffmpeg"
    Write-Host "Downloading ffmpeg essentials"
    Download-File -Url $FfmpegZipUrl -Destination $ffmpegZipPath

    if (Test-Path -LiteralPath $ffmpegExtractDir) {
        Remove-Item -LiteralPath $ffmpegExtractDir -Recurse -Force
    }

    Expand-Archive -LiteralPath $ffmpegZipPath -DestinationPath $ffmpegExtractDir -Force
    $ffmpegExe = Get-ChildItem -LiteralPath $ffmpegExtractDir -Filter ffmpeg.exe -Recurse -File |
        Select-Object -First 1

    if (-not $ffmpegExe) {
        throw "ffmpeg.exe was not found in $ffmpegZipPath"
    }

    Copy-Item -LiteralPath $ffmpegExe.FullName -Destination (Join-Path $assetsDir "ffmpeg.exe") -Force
    Write-Host "Restored ffmpeg.exe"
}

if (-not $ThirdPartyToolsDirectory -and (Test-Path -LiteralPath $InstalledAssetsDirectory)) {
    $ThirdPartyToolsDirectory = $InstalledAssetsDirectory
    Write-Host "Using installed DarkHub assets from $ThirdPartyToolsDirectory"
}

if ($ThirdPartyToolsDirectory) {
    if (-not (Test-Path -LiteralPath $ThirdPartyToolsDirectory)) {
        throw "Third-party tools directory not found: $ThirdPartyToolsDirectory"
    }

    foreach ($fileName in @("CPU-Z.exe", "GPU-Z.exe", "HWiNFO64.exe", "DDU.exe", "SpaceSniffer.exe", "ffmpeg.exe", "yt-dlp.exe")) {
        Copy-IfFound -Root $ThirdPartyToolsDirectory -FileName $fileName -Destination (Join-Path $assetsDir $fileName)
    }

    $settingsSource = Join-Path $ThirdPartyToolsDirectory "settings"
    if (Test-Path -LiteralPath $settingsSource) {
        Copy-Item -LiteralPath $settingsSource -Destination (Join-Path $assetsDir "settings") -Recurse -Force
        Write-Host "Restored assets\settings"
    }
    else {
        Write-Warning "No settings directory found under $ThirdPartyToolsDirectory"
    }
}
else {
    Write-Warning "CPU-Z.exe, GPU-Z.exe, HWiNFO64.exe, DDU.exe, and assets\settings are vendor/local files. Put a backup or extracted release folder in -ThirdPartyToolsDirectory to restore them."
}

Write-Host "Local asset restore finished."
