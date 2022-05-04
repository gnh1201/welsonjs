; @created_on 2020-06-26
; @updated_on 2022-03-04

[Setup]
AppName=WelsonJS
AppVersion=0.2.3-dev
WizardStyle=modern
; DefaultDirName={pf}\{cm:AppName}
DefaultDirName={commonpf}\{cm:AppName}
DefaultGroupName={cm:AppName}
; UninstallDisplayIcon={app}\UnInst.exe
UninstallDisplayIcon={app}\unins000.exe
Compression=lzma2
SolidCompression=yes
OutputDir=bin\installer
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
RestartIfNeededByRun=no
DisableStartupPrompt=true
DisableFinishedPage=true
DisableReadyMemo=true
DisableReadyPage=true
DisableWelcomePage=yes
DisableDirPage=yes
DisableProgramGroupPage=yes

; [Registry]
; Root: HKCR; Subkey: "welsonjs"; ValueType: "string"; ValueData: "URL:{cm:AppName}"; Flags: uninsdeletekey
; Root: HKCR; Subkey: "welsonjs"; ValueType: "string"; ValueName: "URL Protocol"; ValueData: ""
; Root: HKCR; Subkey: "welsonjs\DefaultIcon"; ValueType: "string"; ValueData: "{app}\app\favicon.ico,0"
; Root: HKCR; Subkey: "welsonjs\shell\open\command"; ValueType: "string"; ValueData: "cscript ""{app}\app.js"" uriloader ""%1"""

[Files]
Source: "app.js"; DestDir: "{app}";
Source: "app.hta"; DestDir: "{app}";
Source: "Default_HTA.reg"; DestDir: "{app}";
Source: "LICENSE"; DestDir: "{app}";
Source: "*.md"; DestDir: "{app}";
Source: "start.bat"; DestDir: "{app}";
Source: "uriloader.js"; DestDir: "{app}";
Source: "webloader.js"; DestDir: "{app}";
Source: "bootstrap.js"; DestDir: "{app}";
Source: "app\*"; DestDir: "{app}/app"; Flags: ignoreversion recursesubdirs;
Source: "lib\*"; DestDir: "{app}/lib"; Flags: ignoreversion recursesubdirs;
Source: "bin\*"; DestDir: "{app}/bin"; Flags: ignoreversion recursesubdirs;
Source: "data\*"; DestDir: "{app}/data"; Flags: ignoreversion recursesubdirs;
; Source: "node_modules\*"; DestDir: "{app}/node_modules"; Flags: ignoreversion recursesubdirs;
; Source: "bower_components\*"; DestDir: "{app}/node_modules"; Flags: ignoreversion recursesubdirs;

[Dirs]
Name: "{app}\tmp";

[Icons]
Name: "{group}\Start {cm:AppName}"; Filename: "{app}\start.bat"; AfterInstall: SetElevationBit('{group}\Start {cm:AppName}.lnk');
Name: "{group}\Uninstall {cm:AppName}"; Filename: "{uninstallexe}"; AfterInstall: SetElevationBit('{group}\Uninstall {cm:AppName}.lnk');

[Run]
; Filename: {app}\bin\gtk2-runtime-2.24.33-2021-01-30-ts-win64.exe;
Filename: {app}\bin\nmap-7.92\VC_redist.x86.exe;
Filename: {app}\bin\nmap-7.92\npcap-1.50.exe;
Filename: {app}\IEMaxScriptStatements.bat;
Filename: {app}\start.bat;

[UninstallRun]
; Filename: {code:GetProgramFiles}\GTK2-Runtime Win64\gtk2_runtime_uninst.exe;
Filename: {code:GetProgramFiles}\Npcap\Uninstall.exe;
Filename: {app}\bin\nmap-7.92\VC_redist.x86.exe;

[CustomMessages]
AppName=WelsonJS

[Code]
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
