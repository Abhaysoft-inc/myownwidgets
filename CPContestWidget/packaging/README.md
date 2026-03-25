# Packaging

This project supports easy packaging with one script.

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

If Inno Setup is missing, ZIP is still produced and installer is skipped.
