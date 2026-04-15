#define MyAppRid ExpandConstant('{param:MyAppRid}')
#define PublishDir ExpandConstant('{param:PublishDir}')

[Setup]
AppName=V Suite Lab
AppVersion=1.0.0
DefaultDirName={pf}\V Suite Lab
DefaultGroupName=V Suite Lab
OutputBaseFilename=vsuitelab-{#MyAppRid}-installer

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs