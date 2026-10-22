; @created_on 2020-06-26
; @updated_on 2026-10-05
; @author Namhyeon Go <gnh1201@catswords.re.kr> and Catswords OSS contributors.

[Setup]
AppName=WelsonJS
AppVersion=0.2.7
WizardStyle=modern
; DefaultDirName={pf}\{cm:AppName}
DefaultDirName={commonpf32}\{cm:AppName}
DefaultGroupName={cm:AppName}
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

[Components]
; Add an optional component for the user to select during installation
Name: "fileassoc"; Description: "Associate .js files to run with WelsonJS"; Types: full compact custom;
Name: "artifacts"; Description: "WelsonJS Launcher and Windows Service"; Types: full compact custom;
Name: "mcp"; Description: "Configure WelsonJS MCP server for supported AI CLIs"; Types: full compact custom;
Name: "python"; Description: "Download Python Windows embeddable package"; Types: full;
Name: "curl"; Description: "Download cURL (Universal HTTP client)"; Types: full;
Name: "websocat"; Description: "Download websocat (Command-line WebSocket client)"; Types: full;
Name: "yara"; Description: "Download YARA (Binary pattern matching library)"; Types: custom;
Name: "wamr"; Description: "Download WebAssembly Micro Runtime (Add support *.wasm file)"; Types: custom;
Name: "tessdata"; Description: "Download Tesseract OCR pre-trained data"; Types: custom;
Name: "tessdata_best"; Description: "Download the pre-trained Tesseract OCR data (most accurate)"; Types: custom;
Name: "tessdata_fast"; Description: "Download the pre-trained Tesseract OCR data (faster)"; Types: custom;
Name: "gtk3runtime"; Description: "Download and install GTK3 runtime for Windows"; Types: custom;
Name: "gtkserver"; Description: "Download GTK-server (GTK GUI interpreter)"; Types: custom;
Name: "nmap"; Description: "Download Nmap and Npcap"; Types: custom;
Name: "windivert"; Description: "Download WinDivert (Windows Packet Divert)"; Types: custom;
Name: "android_platform_tools"; Description: "Download Android Platform Tools"; Types: custom;
Name: "hwp_automation"; Description: "Download HWP (Word Processor for Korean) Automation"; Types: custom;
Name: "tls13"; Description: "Enable TLS 1.3 support"; Types: custom;
Name: "tun2socks"; Description: "Download tun2socks proxy tunnel binaries"; Types: custom;
Name: "sendboxie"; Description: "Download Sandboxie-Plus installer"; Types: custom;
Name: "ldplayer"; Description: "Download LDPlayer Android emulator installer"; Types: custom;
Name: "tap_windows6"; Description: "Download OpenVPN TAP Windows driver packages"; Types: custom;
Name: "thc_hydra"; Description: "Download THC Hydra Windows build"; Types: custom;
Name: "shadowsocks_libev"; Description: "Download Shadowsocks-libev Windows build"; Types: custom;
Name: "winlibs_mingw"; Description: "Download WinLibs GCC and MinGW-w64 toolchain"; Types: custom;
Name: "golang"; Description: "Download Go language archives"; Types: custom;
Name: "x86dbg"; Description: "Download x64dbg snapshot"; Types: custom;
Name: "w7zip"; Description: "Download 7-Zip installer"; Types: custom;
Name: "hashcat"; Description: "Download hashcat archive"; Types: custom;
Name: "microsoft_jdk"; Description: "Download Microsoft OpenJDK installer"; Types: custom;
Name: "nuget"; Description: "Download NuGet command-line tool"; Types: custom;
Name: "ripgrep"; Description: "Download ripgrep (fast text search tool)"; Types: custom;

[Registry]
Root: HKCR; Subkey: "{cm:AppName}.Script"; ValueType: string; ValueData: "{cm:AppName} Script"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{cm:AppName}.Script\DefaultIcon"; ValueType: string; ValueData: "{app}\app\favicon.ico,0"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{cm:AppName}.Script\shell"; ValueType: string; ValueData: "open"; Components: artifacts; Flags: uninsdeletevalue
Root: HKCR; Subkey: "{cm:AppName}.Script\shell\open"; ValueType: string; ValueData: "Run with {cm:AppName}"; Components: artifacts; Flags: uninsdeletevalue
Root: HKCR; Subkey: "{cm:AppName}.Script\shell\open\command"; ValueType: string; ValueData: """{app}\bin\x86\WelsonJS.Launcher.exe"" --file ""%1"""; Components: artifacts; Flags: uninsdeletevalue
Root: HKCR; Subkey: "{cm:AppName}.Script\ScriptEngine"; ValueType: string; ValueData: "JScript"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "{cm:AppName}.Script\ScriptHostEncode"; ValueType: string; ValueData: "{{85131630-480C-11D2-B1F9-00C04F86C324}}"; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".js"; ValueType: string; ValueData: "{cm:AppName}.Script"; Components: fileassoc; Flags: uninsdeletevalue;

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
Source: "postInstall.ps1"; DestDir: "{app}";
Source: "helloworld.*"; DestDir: "{app}";
Source: "app\*"; DestDir: "{app}/app"; Flags: ignoreversion recursesubdirs;
Source: "lib\*"; DestDir: "{app}/lib"; Flags: ignoreversion recursesubdirs;
; Source: "bin\*"; Excludes: "installer\*"; DestDir: "{app}/bin"; Flags: ignoreversion recursesubdirs;
Source: "bin\x86\7zr.exe"; DestDir: "{app}/bin/x86"; Flags: ignoreversion recursesubdirs;
Source: "bin\x86\curl.exe"; DestDir: "{app}/bin/x86"; Flags: ignoreversion recursesubdirs;
Source: "bin\x86\curl-ca-bundle.crt"; DestDir: "{app}/bin/x86"; Flags: ignoreversion recursesubdirs;
Source: "bin\x86\WelsonJS.Launcher.exe"; DestDir: "{app}/bin/x86"; Flags: ignoreversion recursesubdirs;
Source: "bin\x86\WelsonJS.Launcher.exe.config"; DestDir: "{app}/bin/x86"; Flags: ignoreversion recursesubdirs;
Source: "bin\x86\WelsonJS.Esent.dll"; DestDir: "{app}/bin/x86"; Flags: ignoreversion recursesubdirs;
Source: "bin\x86\WelsonJS.ManagedObject.dll"; DestDir: "{app}/bin/x86"; Flags: ignoreversion recursesubdirs;
Source: "bin\x86\Catswords.Phantomizer.dll"; DestDir: "{app}/bin/x86"; Flags: ignoreversion recursesubdirs;
Source: "bin\x86\ChakraCore.dll"; DestDir: "{app}/bin/x86"; Flags: ignoreversion recursesubdirs;
Source: "data\*"; Excludes: "*-apikey.txt,.pipe_*"; DestDir: "{app}/data"; Flags: ignoreversion recursesubdirs;
; Source: "node_modules\*"; DestDir: "{app}/node_modules"; Flags: ignoreversion recursesubdirs;
; Source: "bower_components\*"; DestDir: "{app}/node_modules"; Flags: ignoreversion recursesubdirs;

[Dirs]
Name: "{app}\tmp";

; [InstallDelete]
; Type: files; Name: "{app}\settings.ini"
; Type: files; Name: "{app}\defaultService.js"

[Icons]
Name: "{group}\Start {cm:AppName} Launcher"; Filename: "{app}\bin\x86\WelsonJS.Launcher.exe"; Components: artifacts; AfterInstall: SetElevationBit('{group}\Start {cm:AppName} Launcher.lnk');
Name: "{group}\Test {cm:AppName}"; Filename: "{app}\bootstrap.bat"; AfterInstall: SetElevationBit('{group}\Test {cm:AppName}.lnk');
Name: "{group}\Uninstall {cm:AppName}"; Filename: "{uninstallexe}"; AfterInstall: SetElevationBit('{group}\Uninstall {cm:AppName}.lnk');

[Run]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -NoProfile -File ""{app}\postInstall.ps1"" -TelemetryProvider posthog -TelemetryApiKey ""{cm:PostHogApiKey}"" -Version ""{cm:AppVersion}"" -DistinctId ""{computername}"" -Components ""{code:GetSelectedComponents}"""; WorkingDir: "{app}"; Flags: waituntilterminated
Filename: "{app}\installService.bat"; Components: artifacts; Flags: nowait
Filename: "{app}\bin\x86\WelsonJS.Launcher.exe"; Components: artifacts; Flags: nowait

[UninstallRun]
Filename: {app}\uninstallService.bat; Components: artifacts; Flags: waituntilterminated

[CustomMessages]
AppName=WelsonJS
AppVersion=0.2.7.61
PostHogApiKey=phc_pmRHJ0aVEhtULRT4ilexwCjYpGtE9VYRhlA05fwiYt8

[Code]
const
  UninstSiteURL = 'https://catswords.z12.web.core.windows.net/?utm_source=welsonjs_uninstall';
  SalesforceOrgID = '00DdJ000004Vdh7';
  SalesforceURL = 'https://webto.salesforce.com/servlet/servlet.WebToLead?encoding=UTF-8&orgId=' + SalesforceOrgID;
  SalesforceLeadSource = 'Web';

var
  CustomerPage: TInputQueryWizardPage;
  CustomerConsentCheckBox: TNewCheckBox;
  CustomerSubmitted: Boolean;

// Get selected components
function GetSelectedComponents(Value: string): string;
begin
  Result := WizardSelectedComponents(False);
end;

// Set elevation bit
procedure SetElevationBit(Filename: string);
var
  Buffer: string;
  Stream: TStream;
begin
  Filename := ExpandConstant(Filename);
  Log('Setting elevation bit for ' + Filename);

  Stream := TFileStream.Create(Filename, fmOpenReadWrite);
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

// Get Program Files directory
function GetProgramFiles(Param: string): string;
begin
  if IsWin64 then
    Result := ExpandConstant('{commonpf64}')
  else
    Result := ExpandConstant('{commonpf32}');
end;

// Create customer information page
procedure InitializeWizard;
begin
  CustomerPage := CreateInputQueryPage(
    wpWelcome,
    'Customer Information',
    'Please provide your contact information.',
    'Optional. Enter your contact details for future technical support. They will be sent to Catswords OSS via Salesforce after installation if you consent below.'
  );

  CustomerPage.Add('Name', False);
  CustomerPage.Add('Email', False);
  CustomerPage.Add('Company', False);

  CustomerConsentCheckBox := TNewCheckBox.Create(WizardForm);
  CustomerConsentCheckBox.Parent := CustomerPage.Surface;
  CustomerConsentCheckBox.Caption := 'I consent to the collection and use of this information for technical support.';
  CustomerConsentCheckBox.Left := CustomerPage.PromptLabels[0].Left;
  CustomerConsentCheckBox.Top := CustomerPage.Edits[2].Top +
    CustomerPage.Edits[2].Height + ScaleY(8);
  CustomerConsentCheckBox.Width := CustomerPage.Surface.Width;
  CustomerConsentCheckBox.Height := ScaleY(32);
  CustomerConsentCheckBox.Anchors := [akLeft, akTop, akRight];
  CustomerConsentCheckBox.Checked := False;

  CustomerSubmitted := False;
end;

function HexDigit(Value: Integer): string;
begin
  if Value < 10 then
    Result := Chr(Ord('0') + Value)
  else
    Result := Chr(Ord('A') + Value - 10);
end;

// URL-encode a string as UTF-8
function UrlEncode(Value: string): string;
var
  I, B: Integer;
  S: AnsiString;
begin
  Result := '';
  S := UTF8Encode(Value);

  for I := 1 to Length(S) do
  begin
    B := Ord(S[I]);

    if ((B >= Ord('A')) and (B <= Ord('Z'))) or
       ((B >= Ord('a')) and (B <= Ord('z'))) or
       ((B >= Ord('0')) and (B <= Ord('9'))) or
       (B = Ord('-')) or
       (B = Ord('_')) or
       (B = Ord('.')) or
       (B = Ord('~')) then
      Result := Result + Chr(B)
    else if B = Ord(' ') then
      Result := Result + '+'
    else
      Result := Result + '%' + HexDigit(B div 16) + HexDigit(B mod 16);
  end;
end;

// Check whether any customer information was entered
function HasCustomerInformation: Boolean;
var
  I: Integer;
begin
  Result := False;

  for I := 0 to 2 do
  begin
    if Trim(CustomerPage.Values[I]) <> '' then
    begin
      Result := True;
      Exit;
    end;
  end;
end;

// Check the current user's Windows UI language, including Korean variants
function IsKoreanUILanguage: Boolean;
begin
  Result := (GetUILanguage and $3FF) = $12;
end;

// Submit customer information to Salesforce Web-to-Lead
function SubmitCustomerInfo: Boolean;
var
  Http: Variant;
  Body: string;
  FullName: string;
  FirstName: string;
  LastName: string;
  NamePart: string;
  LastSpace: Integer;
  StatusCode: Integer;
begin
  Result := False;

  FullName := Trim(CustomerPage.Values[0]);
  LastSpace := RPos(' ', FullName);
  if LastSpace > 0 then
  begin
    FirstName := Trim(Copy(FullName, 1, LastSpace - 1));
    LastName := Trim(Copy(FullName, LastSpace + 1, Length(FullName)));

    if IsKoreanUILanguage and (Length(FirstName) <= 5) then
    begin
      NamePart := FirstName;
      FirstName := LastName;
      LastName := NamePart;
    end;
  end
  else
  begin
    // If the name has no spaces, its given/family name boundary is unclear.
    // Keep it in Salesforce's first_name field by default.
    FirstName := FullName;
    LastName := '';
  end;

  Body :=
    'oid=' + UrlEncode(SalesforceOrgID) +
    '&first_name=' + UrlEncode(FirstName) +
    '&last_name=' + UrlEncode(LastName) +
    '&email=' + UrlEncode(Trim(CustomerPage.Values[1])) +
    '&company=' + UrlEncode(Trim(CustomerPage.Values[2])) +
    '&lead_source=' + UrlEncode(SalesforceLeadSource);

  try
    Http := CreateOleObject('WinHttp.WinHttpRequest.5.1');

    Http.SetTimeouts(5000, 5000, 10000, 10000);

    Http.Open('POST', SalesforceURL, False);

    Http.SetRequestHeader(
      'Content-Type',
      'application/x-www-form-urlencoded; charset=UTF-8'
    );

    Http.Send(Body);

    StatusCode := Http.Status;
    Result := (StatusCode >= 200) and (StatusCode < 300);

    // A successful HTTP response confirms delivery to the endpoint only;
    // Web-to-Lead does not provide a reliable machine-readable lead result.
    Log('Salesforce Web-to-Lead HTTP status (not a lead-creation confirmation): ' +
      IntToStr(StatusCode));
  except
    Log('Salesforce Web-to-Lead request failed: ' + GetExceptionMessage);
    Result := False;
  end;
end;

// Handle installation steps
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Preserve existing configuration file initialization
    CopyFile(
      ExpandConstant('{app}\settings.example.ini'),
      ExpandConstant('{app}\settings.ini'),
      False
    );

    CopyFile(
      ExpandConstant('{app}\defaultService.example.js'),
      ExpandConstant('{app}\defaultService.js'),
      False
    );

    // Submit customer information only once
    if not CustomerSubmitted then
    begin
      CustomerSubmitted := True;

      if HasCustomerInformation and CustomerConsentCheckBox.Checked then
      begin
        if not SubmitCustomerInfo then
          MsgBox(
            'Could not confirm that the customer information was submitted.' + #13#10 +
            'Salesforce may still have received the request if the connection was interrupted.' + #13#10 +
            'Installation will continue.',
            mbInformation,
            MB_OK
          )
        else
          MsgBox(
            'The information was sent to Catswords OSS via Salesforce.' + #13#10 +
            'This confirms the request was sent, but not that a contact record was created.',
            mbInformation,
            MB_OK
          );
      end;
    end;
  end;
end;

// Preserve existing uninstall behavior
procedure CurUninstallStepChanged(
  CurUninstallStep: TUninstallStep);
var
  ErrorCode: Integer;
begin
  if CurUninstallStep = usDone then
    ShellExec(
      'open',
      UninstSiteURL,
      '',
      '',
      SW_SHOW,
      ewNoWait,
      ErrorCode
    );
end;

