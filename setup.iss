; @created_on 2020-06-26
; @updated_on 2020-11-04

[Setup]
AppName=WelsonJS
AppVersion=0.1.3
WizardStyle=modern
DefaultDirName={pf}\WelsonJS
DefaultGroupName=WelsonJS
UninstallDisplayIcon={app}\UnInst.exe
Compression=lzma2
SolidCompression=yes
OutputDir=bin\installer
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

; [Registry]
; Root: HKCR; Subkey: "welsonjs"; ValueType: "string"; ValueData: "URL:{cm:AppName}"; Flags: uninsdeletekey
; Root: HKCR; Subkey: "welsonjs"; ValueType: "string"; ValueName: "URL Protocol"; ValueData: ""
; Root: HKCR; Subkey: "welsonjs\DefaultIcon"; ValueType: "string"; ValueData: "{app}\app\favicon.ico,0"
; Root: HKCR; Subkey: "welsonjs\shell\open\command"; ValueType: "string"; ValueData: "cscript ""{app}\app.js"" uriloader ""%1"""

[Files]
Source: "app.js"; DestDir: "{app}";
Source: "app.hta"; DestDir: "{app}";
Source: "start.bat"; DestDir: "{app}";
Source: "uriloader.js"; DestDir: "{app}";
Source: "webloader.js"; DestDir: "{app}";
Source: "testloader.js"; DestDir: "{app}";
Source: "shadow.js"; DestDir: "{app}";
Source: "bootstrap.js"; DestDir: "{app}";
Source: "shadow.js"; DestDir: "{app}";
Source: "shoutcut.js"; DestDir: "{app}";
Source: "bgloader.js"; DestDir: "{app}";
Source: "config.template.json"; DestDir: "{app}";
Source: "config.xml"; DestDir: "{app}";
Source: "versionid.txt"; DestDir: "{app}";
Source: "manual.pptx"; DestDir: "{app}";
Source: "app\*"; DestDir: "{app}/app"; Flags: ignoreversion recursesubdirs;
Source: "lib\*"; DestDir: "{app}/lib"; Flags: ignoreversion recursesubdirs;
Source: "bin\*"; DestDir: "{app}/bin"; Flags: ignoreversion recursesubdirs;
; Source: "node_modules\*"; DestDir: "{app}/node_modules"; Flags: ignoreversion recursesubdirs;

[Icons]
Name: "{group}\Start {cm:AppName}"; Filename: "{app}\start.bat"; AfterInstall: SetElevationBit('{group}\Start {cm:AppName}.lnk');
Name: "{group}\Uninstall {cm:AppName}"; Filename: "{uninstallexe}"; AfterInstall: SetElevationBit('{group}\Uninstall {cm:AppName}.lnk');

[Run]
Filename: {app}\start.bat;

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
