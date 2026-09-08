<#
.SYNOPSIS
    Downloads yt-dlp.exe, ffmpeg.exe and deno.exe into a tools folder.
.DESCRIPTION
    Called automatically by "dotnet publish" (see DownloadTools target in the csproj).
    Existing files are kept unless -Force is given. Safe to run manually:
        .\scripts\download-tools.ps1 -Destination .\tools
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Destination,

    [switch] $Force
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$ProgressPreference = 'SilentlyContinue'

New-Item -ItemType Directory -Force -Path $Destination | Out-Null
$Destination = (Resolve-Path $Destination).Path

function Get-File([string] $Url, [string] $Target) {
    Write-Host "  downloading $Url"
    Invoke-WebRequest -Uri $Url -OutFile $Target -UseBasicParsing
}

function Get-FromZip([string] $Url, [string] $EntryName, [string] $Target) {
    $zip = Join-Path ([IO.Path]::GetTempPath()) ([IO.Path]::GetRandomFileName() + '.zip')
    try {
        Get-File $Url $zip
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $archive = [IO.Compression.ZipFile]::OpenRead($zip)
        try {
            $entry = $archive.Entries | Where-Object { $_.Name -ieq $EntryName } | Select-Object -First 1
            if (-not $entry) { throw "'$EntryName' not found in $Url" }
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $Target, $true)
        }
        finally { $archive.Dispose() }
    }
    finally { Remove-Item $zip -ErrorAction SilentlyContinue }
}

$tools = @(
    @{ Name = 'yt-dlp.exe'; Url = 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe' },
    @{ Name = 'ffmpeg.exe'; Url = 'https://github.com/BtbN/FFmpeg-Builds/releases/latest/download/ffmpeg-master-latest-win64-gpl.zip'; Entry = 'ffmpeg.exe' },
    @{ Name = 'deno.exe';   Url = 'https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip'; Entry = 'deno.exe' }
)

Write-Host "Tools folder: $Destination"
foreach ($tool in $tools) {
    $target = Join-Path $Destination $tool.Name
    if ((Test-Path $target) -and -not $Force) {
        Write-Host "  $($tool.Name) already present, skipping"
        continue
    }

    if ($tool.Entry) { Get-FromZip $tool.Url $tool.Entry $target }
    else             { Get-File $tool.Url $target }

    $size = [math]::Round((Get-Item $target).Length / 1MB, 1)
    Write-Host "  $($tool.Name) ready ($size MB)"
}
