#ifndef PayloadDir
  #error PayloadDir must point to the staged installer payload
#endif
#ifndef OutputFolder
  #error OutputFolder must point to the installer output directory
#endif
#ifndef HitVersion
  #define HitVersion "2v2910"
#endif
#ifndef NumericVersion
  #define NumericVersion "2026.10.29"
#endif

[Setup]
#ifdef TestBuild
AppId=HoldItTogether.InstallerTest
#else
AppId={{5626D455-48AC-48EA-B6FC-5F057A51CC16}
#endif
AppName=Hold It Together
AppVersion={#NumericVersion}
AppVerName=Hold It Together {#HitVersion}
AppPublisher=Hold It Together
DefaultDirName={localappdata}\Programs\HoldItTogether
DefaultGroupName=Hold It Together
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
WizardStyle=modern
DisableWelcomePage=no
DisableDirPage=no
DisableProgramGroupPage=yes
SetupIconFile={#PayloadDir}\App\Resources\hit.ico
UninstallDisplayIcon={app}\HoldItTogether.exe
OutputDir={#OutputFolder}
OutputBaseFilename=HIT-{#HitVersion}-win-x64-Setup
Compression=lzma2
SolidCompression=yes
CloseApplications=yes
RestartApplications=no
UsePreviousLanguage=no
#ifdef TestBuild
CreateUninstallRegKey=no
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "{#PayloadDir}\App\*"; DestDir: "{app}"; Excludes: "Docs,Docs\*,README.txt"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PayloadDir}\Developer\*"; DestDir: "{app}\Developer"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: IsDevProfile
Source: "{#PayloadDir}\Chess.hitmenu"; DestDir: "{app}\Optional"; Flags: ignoreversion; Check: IsChessSelected
Source: "{#PayloadDir}\WebView2Setup.exe"; Flags: dontcopy

[Icons]
Name: "{group}\Hold It Together"; Filename: "{app}\HoldItTogether.exe"; WorkingDir: "{app}"; Check: CreateShortcuts
Name: "{code:GetDesktopDirectory}\HIT"; Filename: "{app}\HoldItTogether.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\HoldItTogether.exe"; Description: "Launch Hold It Together"; Flags: nowait postinstall skipifsilent

[Code]
var
  ProfilePage, ChessPage: TInputOptionWizardPage;

function IsDevProfile: Boolean;
begin
  Result := ProfilePage.SelectedValueIndex = 1;
end;

function IsChessSelected: Boolean;
begin
  Result := ChessPage.Values[0];
end;

function CreateShortcuts: Boolean;
begin
  Result := True;
  #ifdef TestBuild
  Result := False;
  #endif
end;

function GetDesktopDirectory(Param: String): String;
begin
  #ifdef TestBuild
  Result := ExpandConstant('{app}\TestDesktop');
  #else
  Result := ExpandConstant('{autodesktop}');
  #endif
end;

procedure InitializeWizard;
begin
  ProfilePage := CreateInputOptionPage(wpSelectDir, 'Choose your installation',
    'Select Release or Dev.', 'Release is for everyday use. Dev adds the SDK, example modules, source code, documentation and build tools.', True, False);
  ProfilePage.Add('Release - HIT only');
  ProfilePage.Add('Dev - HIT and developer kit');
  ProfilePage.SelectedValueIndex := 0;
  if Lowercase(ExpandConstant('{param:PROFILE|release}')) = 'dev' then
    ProfilePage.SelectedValueIndex := 1;
  ChessPage := CreateInputOptionPage(ProfilePage.ID, 'Optional Chess module',
    'Would you like to install Chess?', 'Chess is a separate .hitmenu extension. It is optional in both Release and Dev.', False, False);
  ChessPage.Add('Install Chess');
  ChessPage.Values[0] := ExpandConstant('{param:CHESS|0}') = '1';
end;

function HasWebView2: Boolean;
var
  Version: String;
  Key: String;
begin
  Key := 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';
  Result := RegQueryStringValue(HKLM32, Key, 'pv', Version) and (Version <> '') and (Version <> '0.0.0.0');
  if not Result then
    Result := RegQueryStringValue(HKCU32, Key, 'pv', Version) and (Version <> '') and (Version <> '0.0.0.0');
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo,
  MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
begin
  Result := MemoDirInfo + NewLine + NewLine;
  if IsDevProfile then
    Result := Result + 'Profile: Dev (HIT and developer kit)'
  else
    Result := Result + 'Profile: Release (HIT only)';
  Result := Result + NewLine;
  if IsChessSelected then
    Result := Result + 'Chess: Install optional module'
  else
    Result := Result + 'Chess: Do not install';
  if MemoTasksInfo <> '' then Result := Result + NewLine + NewLine + MemoTasksInfo;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ExitCode: Integer;
begin
  Result := '';
  if not HasWebView2 then begin
    #ifdef TestBuild
    Result := 'Installer integration tests require an existing WebView2 runtime.';
    Exit;
    #endif
    ExtractTemporaryFile('WebView2Setup.exe');
    WizardForm.StatusLabel.Caption := 'Installing Microsoft Edge WebView2 Runtime...';
    if not Exec(ExpandConstant('{tmp}\WebView2Setup.exe'), '/silent /install', '', SW_HIDE, ewWaitUntilTerminated, ExitCode) then begin
      Result := 'Could not start the WebView2 installer. Install Microsoft Edge WebView2 Runtime and run Setup again.';
      Exit;
    end;
    if not HasWebView2 then
      Result := 'WebView2 installation did not complete. Check your Internet connection, install Microsoft Edge WebView2 Runtime and run Setup again.';
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ExitCode: Integer;
  Arguments: String;
begin
  if CurStep = ssPostInstall then begin
    if IsDevProfile then
      SetIniString('Installation', 'Profile', 'Dev', ExpandConstant('{app}\installation.ini'))
    else
      SetIniString('Installation', 'Profile', 'Release', ExpandConstant('{app}\installation.ini'));
    if IsChessSelected then begin
      Arguments := '--hit-install-package "' + ExpandConstant('{app}\Optional\Chess.hitmenu') + '"';
      #ifdef TestBuild
      Arguments := Arguments + ' --hit-module-root "' + ExpandConstant('{app}\TestModules') + '"';
      #endif
      if not Exec(ExpandConstant('{app}\HoldItTogether.exe'), Arguments, ExpandConstant('{app}'), SW_HIDE, ewWaitUntilTerminated, ExitCode) then
        RaiseException('HIT was installed, but the Chess installer could not start. You can import Optional\Chess.hitmenu from Extensions.');
      if ExitCode <> 0 then
        RaiseException('HIT was installed, but Chess could not be installed. Check Extensions before importing Optional\Chess.hitmenu.');
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Command: String;
begin
  #ifndef TestBuild
  if CurUninstallStep = usUninstall then
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'HoldItTogether', Command) then
      if Pos(Lowercase(ExpandConstant('{app}\HoldItTogether.exe')), Lowercase(Command)) > 0 then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'HoldItTogether');
  #endif
end;

[UninstallDelete]
Type: files; Name: "{app}\installation.ini"
