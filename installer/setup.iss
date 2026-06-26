#define MyAppName "AadharLocation"
#ifndef MyAppVersion
  #define MyAppVersion "1.0.1"
#endif
#define MyAppPublisher "sstelecomjk"
#define MyAppURL "https://megamindstechnologies.com"
#define AdminExeName "AadharLocation.AdminDashboard.exe"
#define OperatorExeName "AadharLocation.OperatorTracker.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=output
OutputBaseFilename=AadharLocationSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64os
MinVersion=10.0.19041
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#AdminExeName}
CloseApplications=force
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Code]
var
  AppTypePage: TInputOptionWizardPage;
  AppTypeSelected: Integer;  // 0 = Admin, 1 = Operator

procedure InitializeWizard;
begin
  AppTypePage := CreateInputOptionPage(
    wpWelcome,
    'Select Application Type',
    'Which application do you want to install on this computer?',
    'Choose the role for this workstation and click Next to continue.',
    True,
    False
  );
  AppTypePage.Add('Admin Dashboard  –  Monitor operators, view live map, manage users');
  AppTypePage.Add('Operator Tracker  –  Report location and receive field assignments');
  AppTypePage.SelectedValueIndex := 0;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = AppTypePage.ID then
    AppTypeSelected := AppTypePage.SelectedValueIndex;
end;

function IsAdmin: Boolean;
begin
  Result := AppTypeSelected = 0;
end;

function IsOperator: Boolean;
begin
  Result := AppTypeSelected = 1;
end;

function AppDisplayName: String;
begin
  if IsAdmin then
    Result := 'Admin Dashboard'
  else
    Result := 'Operator Tracker';
end;

function AppExeName: String;
begin
  if IsAdmin then
    Result := '{#AdminExeName}'
  else
    Result := '{#OperatorExeName}';
end;

function InitializeUninstall(): Boolean;
var
  AppPath: String;
  ResultCode: Integer;
begin
  Result := True;

  // Kill admin dashboard if running (prevents any file-lock during cleanup)
  Exec(ExpandConstant('{sys}') + '\taskkill.exe',
       '/F /IM {#AdminExeName}', '',
       SW_HIDE, ewWaitUntilTerminated, ResultCode);

  AppPath := ExpandConstant('{app}') + '\{#OperatorExeName}';

  if not FileExists(AppPath) then
    Exit;

  if not Exec(AppPath, '--verify-uninstall', ExpandConstant('{app}'),
              SW_SHOW, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox('Could not launch uninstall verification. Uninstall cancelled.',
           mbError, MB_OK);
    Result := False;
    Exit;
  end;

  if ResultCode <> 0 then
  begin
    Result := False;
    Exit;
  end;

  // Verification passed — clear operator session so login page shows on next install
  DeleteFile(ExpandConstant('{localappdata}\AadharLocation\tracker.dat'));

  // Kill the running tracker instance so it is removed from the taskbar and tray
  Exec(ExpandConstant('{sys}') + '\taskkill.exe',
       '/F /IM AadharLocation.OperatorTracker.exe', '',
       SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    DeleteFile(ExpandConstant('{userappdata}\AadharLocation\admin-auth.json'));
    DeleteFile(ExpandConstant('{localappdata}\AadharLocation\tracker.dat'));
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    SaveStringToFile(
      ExpandConstant('{app}\install-mode.txt'),
      AppDisplayName,
      False
    );

    SaveStringToFile(
      ExpandConstant('{app}\server_config.json'),
      '{"ApiBaseUrl":"http://157.15.203.127:81"}',
      False
    );

    // Always clear stale session on fresh install so login screen is shown
    if IsAdmin then
      DeleteFile(ExpandConstant('{userappdata}\AadharLocation\admin-auth.json'));
    if IsOperator then
      DeleteFile(ExpandConstant('{localappdata}\AadharLocation\tracker.dat'));
  end;
end;

[Files]
; Admin Dashboard — installed only when Admin is selected
Source: "publish\Admin\*"; \
  DestDir: "{app}"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  Check: IsAdmin

; Operator Tracker — installed only when Operator is selected
Source: "publish\Operator\*"; \
  DestDir: "{app}"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  Check: IsOperator

[Icons]
; Admin shortcuts
Name: "{group}\Admin Dashboard"; \
  Filename: "{app}\{#AdminExeName}"; \
  Comment: "Open AadharLocation Admin Dashboard"; \
  Check: IsAdmin

Name: "{commondesktop}\AadharLocation Admin"; \
  Filename: "{app}\{#AdminExeName}"; \
  Comment: "Open AadharLocation Admin Dashboard"; \
  Check: IsAdmin

; Operator shortcuts
Name: "{group}\Operator Tracker"; \
  Filename: "{app}\{#OperatorExeName}"; \
  Comment: "Open AadharLocation Operator Tracker"; \
  Check: IsOperator

Name: "{commondesktop}\AadharLocation Operator"; \
  Filename: "{app}\{#OperatorExeName}"; \
  Comment: "Open AadharLocation Operator Tracker"; \
  Check: IsOperator

; Uninstall
Name: "{group}\Uninstall {#MyAppName}"; \
  Filename: "{uninstallexe}"

[Registry]
Root: HKCU; \
  Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; \
  ValueName: "AadharLocationTracker"; \
  ValueData: """{app}\{#OperatorExeName}"""; \
  Check: IsOperator; \
  Flags: uninsdeletevalue

[UninstallDelete]
; Files written by the installer itself
Type: files;          Name: "{app}\server_config.json"
Type: files;          Name: "{app}\install-mode.txt"

; Operator Tracker runtime logs (written to install dir)
Type: filesandordirs; Name: "{app}\logs"

; Admin Dashboard — AppData\Roaming\AadharLocation
Type: files;          Name: "{userappdata}\AadharLocation\admin-prefs.json"
Type: files;          Name: "{userappdata}\AadharLocation\theme.txt"
Type: files;          Name: "{userappdata}\AadharLocation\server_config.json"
Type: files;          Name: "{userappdata}\AadharLocation\crash.log"
Type: filesandordirs; Name: "{userappdata}\AadharLocation\logs"
Type: dirifempty;     Name: "{userappdata}\AadharLocation"

; Operator Tracker + Admin — AppData\Local\AadharLocation
Type: filesandordirs; Name: "{localappdata}\AadharLocation\WebView2Cache"
Type: dirifempty;     Name: "{localappdata}\AadharLocation"

; Temp map HTML files
Type: filesandordirs; Name: "{%TEMP}\AadharLocationMaps"

; Remove install dir itself when empty
Type: dirifempty;     Name: "{app}"

[Run]
Filename: "{app}\{#AdminExeName}"; \
  Description: "Launch Admin Dashboard now"; \
  Flags: nowait postinstall skipifsilent; \
  Check: IsAdmin

Filename: "{app}\{#OperatorExeName}"; \
  Description: "Launch Operator Tracker now"; \
  Flags: nowait postinstall skipifsilent; \
  Check: IsOperator
