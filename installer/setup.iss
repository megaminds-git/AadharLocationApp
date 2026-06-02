#define MyAppName "AadharLocation"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Megaminds Technologies"
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

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Code]
var
  AppTypePage: TInputOptionWizardPage;
  ServerUrlPage: TInputQueryWizardPage;
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

  ServerUrlPage := CreateInputQueryPage(
    AppTypePage.ID,
    'Server Configuration',
    'Enter the API Server URL',
    'Enter the URL of the AadharLocation API server that this application will connect to. Example: http://192.168.1.10:5163'
  );
  ServerUrlPage.Add('Server URL:', False);
  ServerUrlPage.Values[0] := 'http://157.15.203.127:81';
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = AppTypePage.ID then
    AppTypeSelected := AppTypePage.SelectedValueIndex;
  if CurPageID = ServerUrlPage.ID then
  begin
    if Trim(ServerUrlPage.Values[0]) = '' then
    begin
      MsgBox('Please enter a valid Server URL.', mbError, MB_OK);
      Result := False;
    end;
  end;
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
    Result := False;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ServerUrl: String;
begin
  if CurStep = ssPostInstall then
  begin
    // Write a small marker so the app can detect how it was installed
    SaveStringToFile(
      ExpandConstant('{app}\install-mode.txt'),
      AppDisplayName,
      False
    );

    // Write server URL config so the app connects to the correct API on first launch
    ServerUrl := Trim(ServerUrlPage.Values[0]);
    SaveStringToFile(
      ExpandConstant('{app}\server_config.json'),
      '{"ApiBaseUrl":"' + ServerUrl + '"}',
      False
    );
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

[Run]
Filename: "{app}\{#AdminExeName}"; \
  Description: "Launch Admin Dashboard now"; \
  Flags: nowait postinstall skipifsilent; \
  Check: IsAdmin

Filename: "{app}\{#OperatorExeName}"; \
  Description: "Launch Operator Tracker now"; \
  Flags: nowait postinstall skipifsilent; \
  Check: IsOperator
