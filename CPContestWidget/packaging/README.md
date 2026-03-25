# Packaging

This project supports easy packaging with one script.

## Runtime Bootstrap Installer (small installer + smooth first run)

If you use `framework-dependent` mode, installer builds now bundle a .NET Desktop Runtime bootstrapper.
During install, setup checks for `Microsoft.WindowsDesktop.App` runtime and silently installs it if missing.

Use:

```powershell
.\packaging\Build-Package.ps1 -Mode installer -Version 1.0.0 -RuntimeModel framework-dependent -SingleFile:$false
```

Notes:
- Bootstrapper is downloaded from `https://aka.ms/dotnet/<major>.0/windowsdesktop-runtime-win-x64.exe`.
- Download is cached in `packaging/cache/`.
- Change runtime major with `-DotnetMajor` if needed.

## Recommended: Installer (Inno Setup)

1. Install Inno Setup 6.
2. Run:

```powershell
.\packaging\Build-Package.ps1 -Mode installer -Version 1.0.0
```

Output: `artifacts/CPContestWidget-Setup-1.0.0.exe`

The installer includes:
- Start Menu shortcut
- Uninstall entry
- Optional desktop shortcut
- Optional start-with-Windows registry entry

## Fastest: ZIP

```powershell
.\packaging\Build-Package.ps1 -Mode zip -Version 1.0.0
```

Output: `artifacts/CPContestWidget-1.0.0-win-x64.zip`

## Both

```powershell
.\packaging\Build-Package.ps1 -Mode both -Version 1.0.0
```

## Reduce package size

Use framework-dependent mode. This requires target PCs to have .NET Desktop Runtime installed, but gives much smaller packages.

Smallest ZIP option:

```powershell
.\packaging\Build-Package.ps1 -Mode zip -Version 1.0.0 -RuntimeModel framework-dependent -SingleFile:$false
```

Small installer with runtime bootstrap:

```powershell
.\packaging\Build-Package.ps1 -Mode installer -Version 1.0.0 -RuntimeModel framework-dependent -SingleFile:$false
```

Portable larger option (no runtime install needed):

```powershell
.\packaging\Build-Package.ps1 -Mode zip -Version 1.0.0 -RuntimeModel self-contained -SingleFile:$true
```

ZIP names include mode tags:
- `sc-single` (largest, most portable)
- `fdd-nosingle` (smallest)

If Inno Setup is missing, ZIP is still produced and installer is skipped.
