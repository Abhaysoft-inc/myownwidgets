param(
    [ValidateSet('installer','zip','both')]
    [string]$Mode = 'both',
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $projectRoot 'bin/Release/net10.0-windows/win-x64/publish'
$artifactsDir = Join-Path $projectRoot 'artifacts'
$issFile = Join-Path $PSScriptRoot 'CPContestWidget.iss'

New-Item -ItemType Directory -Path $artifactsDir -Force | Out-Null

Write-Host 'Publishing Release single-file build...'
dotnet publish "$projectRoot/CPContestWidget.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

if ($Mode -in @('zip', 'both')) {
    $zipPath = Join-Path $artifactsDir "CPContestWidget-$Version-win-x64.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Write-Host "Creating ZIP package: $zipPath"
    Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath
}

if ($Mode -in @('installer', 'both')) {
    $iscc = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if (-not $iscc) {
        $commonPath = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
        if (Test-Path $commonPath) {
            $iscc = Get-Item $commonPath
        }
    }

    if ($iscc) {
        Write-Host 'Building installer with Inno Setup...'
        & $iscc.FullName "/DMyAppVersion=$Version" $issFile
    }
    else {
        Write-Warning 'Inno Setup not found. ZIP was created, but installer was skipped.'
        Write-Warning 'Install Inno Setup 6, then rerun: .\\packaging\\Build-Package.ps1 -Mode installer'
    }
}

Write-Host "Done. Artifacts are in: $artifactsDir"
