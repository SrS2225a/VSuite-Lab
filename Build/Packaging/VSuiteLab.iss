[Setup]
AppName=V Suite Lab
AppVersion=1.0.0
DefaultDirName={pf}\V Suite Lab
DefaultGroupName=V Suite Lab
OutputBaseFilename=vsuitelab-{param:MyAppRid}-installer

[Files]
Source: "{param:PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs