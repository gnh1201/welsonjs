; @created_on 2020-06-26
; @updated_on 2025-11-21
; @author Namhyeon Go <gnh1201@catswords.re.kr>

[Setup]
AppName=WelsonJS
AppVersion=0.2.7
WizardStyle=modern
; DefaultDirName={pf}\{cm:AppName}
DefaultDirName={commonpf32}\{cm:AppName}
DefaultGroupName={cm:AppName}
; UninstallDisplayIcon={app}\UnInst.exe
UninstallDisplayIcon={app}\unins000.exe
Compression=lzma2
SolidCompression=yes
OutputDir=bin\installer
PrivilegesRequired=admin
; ArchitecturesInstallIn64BitMode=x64
RestartIfNeededByRun=no
DisableStartupPrompt=true
DisableFinishedPage=true
DisableReadyMemo=true
DisableReadyPage=true
DisableWelcomePage=yes
DisableDirPage=yes
DisableProgramGroupPage=yes
LicenseFile=SECURITY.MD
ChangesAssociations=yes

[Registry]
; Root: HKCR; Subkey: "welsonjs"; ValueType: "string"; ValueData: "URL:{cm:AppName}"; Flags: uninsdeletekey
; Root: HKCR; Subkey: "welsonjs"; ValueType: "string"; ValueName: "URL Protocol"; ValueData: ""
; Root: HKCR; Subkey: "welsonjs\DefaultIcon"; ValueType: "string"; ValueData: "{app}\app\favicon.ico,0"
; Root: HKCR; Subkey: "welsonjs\shell\open\command"; ValueType: "string"; ValueData: "cscript ""{app}\app.js"" uriloader ""%1"""
Root: HKCR; Subkey: "{cm:AppName}.Script"; ValueType: string; ValueData: "{cm:AppName} Script"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{cm:AppName}.Script\DefaultIcon"; ValueType: string; ValueData: "{app}\app\favicon.ico,0"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{cm:AppName}.Script\shell"; ValueType: string; ValueData: "open"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "{cm:AppName}.Script\shell\open"; ValueType: string; ValueData: "Run with {cm:AppName}"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "{cm:AppName}.Script\shell\open\command"; ValueType: string; ValueData: """{userappdata}\{cm:AppName}\bin\WelsonJS.Launcher.exe"" --file ""%1"""; Flags: uninsdeletevalue
Root: HKCR; Subkey: "{cm:AppName}.Script\ScriptEngine"; ValueType: string; ValueData: "JScript"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "{cm:AppName}.Script\ScriptHostEncode"; ValueType: string; ValueData: "{{85131630-480C-11D2-B1F9-00C04F86C324}}"; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".js"; ValueType: string; ValueData: "{cm:AppName}.Script"; Flags: uninsdeletevalue

[Files]
Source: "app.js"; DestDir: "{app}";
Source: "app.hta"; DestDir: "{app}";
Source: "LICENSE"; DestDir: "{app}";
Source: "LICENSE_MSRL"; DestDir: "{app}";
Source: "*.md"; DestDir: "{app}";
Source: "bootstrap.bat"; DestDir: "{app}";
Source: "uriloader.js"; DestDir: "{app}";
Source: "webloader.js"; DestDir: "{app}";
Source: "testloader.js"; DestDir: "{app}";
Source: "encryptor.js"; DestDir: "{app}";
Source: "bootstrap.js"; DestDir: "{app}";
Source: "settings.example.ini"; DestDir: "{app}";
Source: "defaultService.example.js"; DestDir: "{app}";
Source: "installService.bat"; DestDir: "{app}";
Source: "uninstallService.bat"; DestDir: "{app}";
Source: "afterInstall.ps1"; DestDir: "{app}";
Source: "helloworld.*"; DestDir: "{app}";
Source: "app\*"; Excludes: "assets\img\_templates,assets\tessdata\*,assets\tessdata_best\*,assets\tessdata_fast\*"; DestDir: "{app}/app"; Flags: ignoreversion recursesubdirs;
Source: "lib\*"; DestDir: "{app}/lib"; Flags: ignoreversion recursesubdirs;
; Source: "bin\*"; Excludes: "installer\*"; DestDir: "{app}/bin"; Flags: ignoreversion recursesubdirs;
Source: "data\*"; Excludes: "*-apikey.txt"; DestDir: "{app}/data"; Flags: ignoreversion recursesubdirs;
; Source: "node_modules\*"; DestDir: "{app}/node_modules"; Flags: ignoreversion recursesubdirs;
; Source: "bower_components\*"; DestDir: "{app}/node_modules"; Flags: ignoreversion recursesubdirs;

[Dirs]
Name: "{app}\tmp";

[InstallDelete]
Type: files; Name: "{app}\settings.ini"
Type: files; Name: "{app}\defaultService.js"

[Icons]
Name: "{group}\Start {cm:AppName} Launcher"; Filename: "{userappdata}\{cm:AppName}\bin\WelsonJS.Launcher.exe"; AfterInstall: SetElevationBit('{group}\Start {cm:AppName} Launcher.lnk');
Name: "{group}\Test {cm:AppName}"; Filename: "{app}\bootstrap.bat"; AfterInstall: SetElevationBit('{group}\Test {cm:AppName}.lnk');
Name: "{group}\Uninstall {cm:AppName}"; Filename: "{uninstallexe}"; AfterInstall: SetElevationBit('{group}\Uninstall {cm:AppName}.lnk');

[Run]
; Filename: {app}\bin\gtk2-runtime-2.24.33-2021-01-30-ts-win64.exe;
; Filename: {app}\bin\nmap-7.92\VC_redist.x86.exe;
; Filename: {app}\bin\nmap-7.92\npcap-1.50.exe;
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -NoProfile -File ""{app}\afterInstall.ps1"""; WorkingDir: "{app}"; Flags: waituntilterminated
Filename: {app}\installService.bat; Flags: nowait
Filename: "{userappdata}\{cm:AppName}\bin\WelsonJS.Launcher.exe"; Flags: nowait

[UninstallRun]
Filename: {app}\uninstallService.bat;
; Filename: {code:GetProgramFiles}\GTK2-Runtime Win64\gtk2_runtime_uninst.exe;
; Filename: {code:GetProgramFiles}\Npcap\Uninstall.exe;
; Filename: {app}\bin\nmap-7.92\VC_redist.x86.exe;

[CustomMessages]
AppName=WelsonJS

[Code]
const
  UninstSiteURL = 'https://github.com/gnh1201/welsonjs';

procedure SetElevationBit(Filename: string);
var
  Buffer: string;
  Stream: TStream;
begin
  Filename := ExpandConstant(Filename);
  Log('Setting elevation bit for ' + Filename);

  Stream := TFileStream.Create(FileName, fmOpenReadWrite);
  try
    Stream.Seek(21, soFromBeginning);
    SetLength(Buffer, 1);
    Stream.ReadBuffer(Buffer, 1);
    Buffer[1] := Chr(Ord(Buffer[1]) or $20);
    Stream.Seek(-1, soFromCurrent);
    Stream.WriteBuffer(Buffer, 1);
  finally
    Stream.Free;
  end;
end;

function GetProgramFiles(Param: string): string;
begin
  if IsWin64 then Result := ExpandConstant('{commonpf64}')
    else Result := ExpandConstant('{commonpf32}')
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ErrorCode: Integer;
begin
  if CurUninstallStep = usDone then
    ShellExec('open', UninstSiteURL, '', '', SW_SHOW, ewNoWait, ErrorCode);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    CopyFile(ExpandConstant('{app}\settings.example.ini'), ExpandConstant('{app}\settings.ini'), False);
    CopyFile(ExpandConstant('{app}\defaultService.example.js'), ExpandConstant('{app}\defaultService.js'), False);
  end;
end;

