[Setup]
AppName=V Suite Lab
AppVersion=1.0.0
DefaultDirName={pf}\V Suite Lab
DefaultGroupName=V Suite Lab
OutputBaseFilename=vsuitelab-{param:DMyAppRid}-installer

[Files]
Source: "{param:DPublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs