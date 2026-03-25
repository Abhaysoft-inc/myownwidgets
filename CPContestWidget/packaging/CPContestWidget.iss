#define MyAppId "CPContestWidget"
#define MyAppName "CP Contest Widget"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Widgets"
#define MyAppExeName "CPContestWidget.exe"
#define MyAppURL "https://github.com"
#ifndef MyRuntimeModel
#define MyRuntimeModel "self-contained"
#endif
#ifndef MyDotnetMajor
#define MyDotnetMajor "10"
#endif
#ifndef MyPublishDir
#define MyPublishDir "..\\bin\\Release\\net10.0-windows\\win-x64\\publish"
#endif

[Setup]
AppId={{#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes
LicenseFile=
PrivilegesRequired=lowest
OutputDir=..\\artifacts
OutputBaseFilename=CPContestWidget-Setup-{#MyAppVersion}
SetupIconFile=..\\AppIcon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\\{#MyAppExeName}
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked
Name: "startup"; Description: "Start CP Contest Widget when I sign in"; GroupDescription: "Startup options:"; Flags: unchecked

[Files]
Source: "{#MyPublishDir}\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
#ifdef MyDotnetRuntimeInstaller
Source: "{#MyDotnetRuntimeInstaller}"; DestDir: "{tmp}"; DestName: "dotnet-desktop-runtime.exe"; Flags: deleteafterinstall; Check: NeedsDotnetRuntime
#endif

[Icons]
Name: "{group}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"
Name: "{group}\\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\\Microsoft\\Windows\\CurrentVersion\\Run"; ValueType: string; ValueName: "CPContestWidget"; ValueData: """{app}\\{#MyAppExeName}"""; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
function RuntimeBootstrapEnabled(): Boolean;
begin
	Result := CompareText('{#MyRuntimeModel}', 'framework-dependent') = 0;
end;

function HasMatchingDesktopRuntime(): Boolean;
var
	DotnetPath: string;
	TempFile: string;
	Lines: TArrayOfString;
	I: Integer;
	Needle: string;
	ResultCode: Integer;
begin
	Result := False;
	DotnetPath := ExpandConstant('{cmd}');
	TempFile := ExpandConstant('{tmp}\\dotnet-runtimes.txt');

	if not Exec(DotnetPath,
			'/C dotnet --list-runtimes > "' + TempFile + '"',
			'', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
		Exit;

	if not LoadStringsFromFile(TempFile, Lines) then
		Exit;

	Needle := 'Microsoft.WindowsDesktop.App {#MyDotnetMajor}.';

	for I := 0 to GetArrayLength(Lines) - 1 do
	begin
		if Pos(Needle, Trim(Lines[I])) = 1 then
		begin
			Result := True;
			Exit;
		end;
	end;
end;

function NeedsDotnetRuntime(): Boolean;
begin
	Result := RuntimeBootstrapEnabled() and (not HasMatchingDesktopRuntime());
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
	ResultCode: Integer;
begin
	if CurStep <> ssPostInstall then
		Exit;

	if not NeedsDotnetRuntime() then
		Exit;

#ifdef MyDotnetRuntimeInstaller
	if not Exec(ExpandConstant('{tmp}\\dotnet-desktop-runtime.exe'), '/install /quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
	begin
		MsgBox('.NET Desktop Runtime installation could not be started. Please install the runtime manually.', mbError, MB_OK);
		Abort;
	end;

	if (ResultCode <> 0) and (ResultCode <> 3010) then
	begin
		MsgBox('.NET Desktop Runtime installation failed. Setup will exit now.' + #13#10 + 'Exit code: ' + IntToStr(ResultCode), mbError, MB_OK);
		Abort;
	end;
#else
	MsgBox('.NET Desktop Runtime is missing and no bootstrap installer was bundled.' + #13#10 +
				 'Install .NET Desktop Runtime {#MyDotnetMajor}.x (x64) and run setup again.', mbError, MB_OK);
	Abort;
#endif
end;
