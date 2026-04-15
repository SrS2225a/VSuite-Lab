#define MyAppRid GetEnv("MyAppRid")

[Setup]
AppName=V Suite Lab
AppVersion=1.0.0
DefaultDirName={pf}\V Suite Lab
DefaultGroupName=V Suite Lab
OutputBaseFilename=vsuitelab-{#MyAppRid}-installer

[Files]
Source: "publish\{#MyAppRid}\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\V Suite Lab"; Filename: "{app}\vsuitelab.exe"
Name: "{commondesktop}\V Suite Lab"; Filename: "{app}\vsuitelab.exe"