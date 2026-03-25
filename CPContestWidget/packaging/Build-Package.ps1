param(
    [ValidateSet('installer','zip','both')]
    [string]$Mode = 'both',
    [string]$Version = '1.0.0',
    [ValidateSet('self-contained','framework-dependent')]
    [string]$RuntimeModel = 'self-contained',
    [bool]$SingleFile = $true,
    [int]$DotnetMajor = 10
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$artifactsDir = Join-Path $projectRoot 'artifacts'
$issFile = Join-Path $PSScriptRoot 'CPContestWidget.iss'
$cacheDir = Join-Path $PSScriptRoot 'cache'
$publishDirName = if ($RuntimeModel -eq 'self-contained') { 'publish-sc' } else { 'publish-fdd' }
$singleSuffix = if ($SingleFile) { 'single' } else { 'nosingle' }
$publishDir = Join-Path $projectRoot "bin/Release/$publishDirName-$singleSuffix"
$selfContained = if ($RuntimeModel -eq 'self-contained') { 'true' } else { 'false' }

if ($RuntimeModel -eq 'framework-dependent' -and -not $SingleFile) {
    $zipTag = 'fdd-nosingle'
}
elseif ($RuntimeModel -eq 'framework-dependent' -and $SingleFile) {
    $zipTag = 'fdd-single'
}
elseif ($RuntimeModel -eq 'self-contained' -and -not $SingleFile) {
    $zipTag = 'sc-nosingle'
}
else {
    $zipTag = 'sc-single'
}

New-Item -ItemType Directory -Path $artifactsDir -Force | Out-Null
New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null

Write-Host 'Publishing Release single-file build...'
dotnet publish "$projectRoot/CPContestWidget.csproj" -c Release -r win-x64 --self-contained $selfContained -p:PublishSingleFile=$SingleFile -p:IncludeNativeLibrariesForSelfExtract=$SingleFile -p:DebugType=None -o $publishDir

if ($Mode -in @('zip', 'both')) {
    $zipPath = Join-Path $artifactsDir "CPContestWidget-$Version-win-x64-$zipTag.zip"
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
        $innoPublishDir = $publishDir.Replace('/', '\\')

        $innoArgs = @(
            "/DMyAppVersion=$Version",
            "/DMyPublishDir=$innoPublishDir",
            "/DMyRuntimeModel=$RuntimeModel",
            "/DMyDotnetMajor=$DotnetMajor"
        )

        if ($RuntimeModel -eq 'framework-dependent') {
            $runtimeInstaller = Join-Path $cacheDir "windowsdesktop-runtime-$DotnetMajor-win-x64.exe"
            if (-not (Test-Path $runtimeInstaller)) {
                $runtimeUrl = "https://aka.ms/dotnet/$DotnetMajor.0/windowsdesktop-runtime-win-x64.exe"
                Write-Host "Downloading .NET Desktop Runtime bootstrapper: $runtimeUrl"
                Invoke-WebRequest -Uri $runtimeUrl -OutFile $runtimeInstaller
            }

            $runtimeInstallerInno = $runtimeInstaller.Replace('/', '\\')
            $innoArgs += "/DMyDotnetRuntimeInstaller=$runtimeInstallerInno"
        }

        $innoArgs += $issFile
        & $iscc.FullName @innoArgs
    }
    else {
        Write-Warning 'Inno Setup not found. ZIP was created, but installer was skipped.'
        Write-Warning 'Install Inno Setup 6, then rerun: .\\packaging\\Build-Package.ps1 -Mode installer'
    }
}

Write-Host "Done. Artifacts are in: $artifactsDir"
