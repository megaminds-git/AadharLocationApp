#define AppName "Aadhar Location"
#define AppVersion "1.0.0"
#define AppPublisher "sstelecomjk"
#define AppURL "https://megamindstechnologies.com"
#define AdminExe "..\publish\sc\AdminDashboard\AadharLocation.AdminDashboard.exe"
#define AdminSettings "..\publish\sc\AdminDashboard\appsettings.json"
#define OperatorExe "..\publish\sc\OperatorTracker\AadharLocation.OperatorTracker.exe"
#define OperatorSettings "..\publish\sc\OperatorTracker\appsettings.json"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
DefaultDirName={autopf}\AadharLocation
DisableDirPage=no
DefaultGroupName=Aadhar Location
DisableProgramGroupPage=yes
OutputDir=..\publish\installer
OutputBaseFilename=AadharLocationSetup
SetupIconFile=
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
WizardSizePercent=120
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Components]
Name: "admin";    Description: "Admin Dashboard"
Name: "operator"; Description: "Operator Tracker"

[Files]
Source: {#AdminExe};         DestDir: "{app}"; DestName: "AadharLocation.AdminDashboard.exe";  Components: admin;    Flags: ignoreversion
Source: {#AdminSettings};    DestDir: "{app}"; DestName: "appsettings.json";                   Components: admin;    Flags: ignoreversion
Source: {#OperatorExe};      DestDir: "{app}"; DestName: "AadharLocation.OperatorTracker.exe"; Components: operator; Flags: ignoreversion
Source: {#OperatorSettings}; DestDir: "{app}"; DestName: "appsettings.json";                   Components: operator; Flags: ignoreversion

[Icons]
Name: "{group}\Aadhar Location - Admin Dashboard";  Filename: "{app}\AadharLocation.AdminDashboard.exe";  Components: admin
Name: "{commondesktop}\Aadhar Location (Admin)";    Filename: "{app}\AadharLocation.AdminDashboard.exe";  Components: admin;    Tasks: desktopicon
Name: "{group}\Aadhar Location - Operator Tracker"; Filename: "{app}\AadharLocation.OperatorTracker.exe"; Components: operator
Name: "{commondesktop}\Aadhar Location (Operator)"; Filename: "{app}\AadharLocation.OperatorTracker.exe"; Components: operator; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\AadharLocation.AdminDashboard.exe";   Description: "Launch Admin Dashboard";  Flags: nowait postinstall skipifsilent; Components: admin
Filename: "{app}\AadharLocation.OperatorTracker.exe";  Description: "Launch Operator Tracker"; Flags: nowait postinstall skipifsilent; Components: operator

[Code]

var
  RolePage:     TWizardPage;
  AdminRadio:   TRadioButton;
  OperatorRadio: TRadioButton;

procedure InitializeWizard;
var
  LblTitle, LblSub: TLabel;
begin
  RolePage := CreateCustomPage(wpWelcome,
    'Select Installation Type',
    'Choose which application you want to install on this computer.');

  LblTitle := TLabel.Create(RolePage);
  LblTitle.Parent  := RolePage.Surface;
  LblTitle.Left    := 0;
  LblTitle.Top     := 8;
  LblTitle.Width   := RolePage.SurfaceWidth;
  LblTitle.Caption := 'Who will be using this computer?';
  LblTitle.Font.Style := [fsBold];

  LblSub := TLabel.Create(RolePage);
  LblSub.Parent  := RolePage.Surface;
  LblSub.Left    := 0;
  LblSub.Top     := 28;
  LblSub.Width   := RolePage.SurfaceWidth;
  LblSub.Caption := 'Only the selected application will be installed.';

  AdminRadio := TRadioButton.Create(RolePage);
  AdminRadio.Parent  := RolePage.Surface;
  AdminRadio.Left    := 8;
  AdminRadio.Top     := 68;
  AdminRadio.Width   := RolePage.SurfaceWidth - 8;
  AdminRadio.Height  := 24;
  AdminRadio.Caption := 'Admin  —  Manage operators, view live map, receive alerts';
  AdminRadio.Checked := True;

  OperatorRadio := TRadioButton.Create(RolePage);
  OperatorRadio.Parent  := RolePage.Surface;
  OperatorRadio.Left    := 8;
  OperatorRadio.Top     := 100;
  OperatorRadio.Width   := RolePage.SurfaceWidth - 8;
  OperatorRadio.Height  := 24;
  OperatorRadio.Caption := 'Operator  —  Send my location to the admin server';
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := PageID = wpSelectComponents;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = RolePage.ID then
  begin
    if (not AdminRadio.Checked) and (not OperatorRadio.Checked) then
    begin
      MsgBox('Please select a role before continuing.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    if AdminRadio.Checked then
      WizardSelectComponents('admin')
    else
      WizardSelectComponents('operator');
  end;
end;

function InitializeUninstall(): Boolean;
var
  AppPath: String;
  ResultCode: Integer;
begin
  Result := True;
  AppPath := ExpandConstant('{app}') + '\AadharLocation.OperatorTracker.exe';

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

  // Kill the running tracker instance so it is removed from the taskbar and tray
  Exec(ExpandConstant('{sys}') + '\taskkill.exe',
       '/F /IM AadharLocation.OperatorTracker.exe', '',
       SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;
