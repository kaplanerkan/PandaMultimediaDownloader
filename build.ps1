<#
.SYNOPSIS
    Produces a ready-to-ship folder: self-contained single exe + tools + zip.
.EXAMPLE
    .\build.ps1                # publish to .\dist\PandaMultimediaDownloader and zip it
    .\build.ps1 -NoTools       # skip yt-dlp / ffmpeg / deno download
#>
[CmdletBinding()]
param(
    [string] $Output = (Join-Path $PSScriptRoot 'dist'),
    [switch] $NoTools,
    [switch] $NoZip,
    [switch] $Installer
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'src\PandaMultimediaDownloader\PandaMultimediaDownloader.csproj'
$publishDir = Join-Path $Output 'PandaMultimediaDownloader'

Write-Host "Publishing to $publishDir"
dotnet publish $project -c Release -o $publishDir "-p:DownloadTools=$(-not $NoTools)"
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1

if (-not $NoZip) {
    $zip = Join-Path $Output "PandaMultimediaDownloader-v$version-win-x64.zip"
    Remove-Item $zip -ErrorAction SilentlyContinue
    Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zip
    Write-Host "Created $zip ($([math]::Round((Get-Item $zip).Length / 1MB, 1)) MB)"
}

if ($Installer) {
    $iscc = @(
        (Get-Command iscc.exe -ErrorAction SilentlyContinue).Source
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    ) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
    if (-not $iscc) { throw 'Inno Setup 6 not found. Install it: winget install JRSoftware.InnoSetup' }

    $iss = Join-Path $PSScriptRoot 'installer\PandaMultimediaDownloader.iss'
    & $iscc /Q "/DAppVersion=$version" "/DSourceDir=$publishDir" "/DOutputDir=$Output" $iss
    if ($LASTEXITCODE -ne 0) { throw "ISCC failed with exit code $LASTEXITCODE" }
    $setup = Join-Path $Output "PandaMultimediaDownloader-v$version-win-x64-setup.exe"
    Write-Host "Created $setup ($([math]::Round((Get-Item $setup).Length / 1MB, 1)) MB)"
}
