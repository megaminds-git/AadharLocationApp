#define AppName "Aadhar Location"
#define AppVersion "1.0.0"
#define AppPublisher "Megaminds Technologies"
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

[Types]
Name: "admin";    Description: "Admin Dashboard"
Name: "operator"; Description: "Operator Tracker"

[Components]
Name: "admin";    Description: "Admin Dashboard - Manage operators, geofences and view live location"  ; Types: admin
Name: "operator"; Description: "Operator Tracker - Track and report your location to admin"            ; Types: operator

[Files]
; Admin Dashboard
Source: {#AdminExe};       DestDir: "{app}"; DestName: "AadharLocation.AdminDashboard.exe"; Components: admin; Flags: ignoreversion
Source: {#AdminSettings};  DestDir: "{app}"; DestName: "appsettings.json";                 Components: admin; Flags: ignoreversion onlyifdoesntexist

; Operator Tracker
Source: {#OperatorExe};      DestDir: "{app}"; DestName: "AadharLocation.OperatorTracker.exe"; Components: operator; Flags: ignoreversion
Source: {#OperatorSettings}; DestDir: "{app}"; DestName: "appsettings.json";                   Components: operator; Flags: ignoreversion onlyifdoesntexist

[Icons]
; Admin shortcut
Name: "{group}\Aadhar Location - Admin Dashboard";   Filename: "{app}\AadharLocation.AdminDashboard.exe";   Components: admin
Name: "{commondesktop}\Aadhar Location (Admin)";     Filename: "{app}\AadharLocation.AdminDashboard.exe";   Components: admin; Tasks: desktopicon

; Operator shortcut
Name: "{group}\Aadhar Location - Operator Tracker";  Filename: "{app}\AadharLocation.OperatorTracker.exe"; Components: operator
Name: "{commondesktop}\Aadhar Location (Operator)";  Filename: "{app}\AadharLocation.OperatorTracker.exe"; Components: operator; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
; Launch after install - admin
Filename: "{app}\AadharLocation.AdminDashboard.exe";   Description: "Launch Admin Dashboard";   Flags: nowait postinstall skipifsilent; Components: admin
; Launch after install - operator
Filename: "{app}\AadharLocation.OperatorTracker.exe";  Description: "Launch Operator Tracker";  Flags: nowait postinstall skipifsilent; Components: operator

[Code]
{ ---- Custom wizard page: pick Admin or Operator before the component page ---- }

var
  RolePage: TWizardPage;
  AdminRadio: TRadioButton;
  OperatorRadio: TRadioButton;

procedure InitializeWizard;
var
  LabelTitle, LabelSub: TLabel;
begin
  RolePage := CreateCustomPage(wpWelcome, 'Select Installation Type',
    'Choose which application you want to install.');

  LabelTitle := TLabel.Create(RolePage);
  LabelTitle.Parent := RolePage.Surface;
  LabelTitle.Left   := 0;
  LabelTitle.Top    := 8;
  LabelTitle.Width  := RolePage.SurfaceWidth;
  LabelTitle.Caption := 'Who will be using this computer?';
  LabelTitle.Font.Style := [fsBold];

  LabelSub := TLabel.Create(RolePage);
  LabelSub.Parent := RolePage.Surface;
  LabelSub.Left   := 0;
  LabelSub.Top    := 28;
  LabelSub.Width  := RolePage.SurfaceWidth;
  LabelSub.Caption := 'Select the appropriate role to install only the required application.';

  AdminRadio := TRadioButton.Create(RolePage);
  AdminRadio.Parent  := RolePage.Surface;
  AdminRadio.Left    := 8;
  AdminRadio.Top     := 70;
  AdminRadio.Width   := RolePage.SurfaceWidth - 8;
  AdminRadio.Height  := 20;
  AdminRadio.Caption := 'Admin  —  Install the Admin Dashboard (manage operators, view map, alerts)';
  AdminRadio.Checked := True;

  OperatorRadio := TRadioButton.Create(RolePage);
  OperatorRadio.Parent  := RolePage.Surface;
  OperatorRadio.Left    := 8;
  OperatorRadio.Top     := 100;
  OperatorRadio.Width   := RolePage.SurfaceWidth - 8;
  OperatorRadio.Height  := 20;
  OperatorRadio.Caption := 'Operator  —  Install the Operator Tracker (send location to admin)';
end;

{ Apply the radio selection to the components list automatically }
procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectComponents then
  begin
    if AdminRadio.Checked then
      WizardSelectComponents('admin')
    else
      WizardSelectComponents('operator');
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  { Ensure at least one radio is checked (always true, just safety) }
  if CurPageID = RolePage.ID then
  begin
    if (not AdminRadio.Checked) and (not OperatorRadio.Checked) then
    begin
      MsgBox('Please select a role before continuing.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;
