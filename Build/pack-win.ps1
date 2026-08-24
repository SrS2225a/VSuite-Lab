param(
[string]$RID,
[string]$VERSION
)

$ErrorActionPreference = "Stop"

if (-not $RID -or -not $VERSION) {
    throw "Usage: pack-win.ps1 <RID> <VERSION>"
}

$APP_NAME = "VSuiteLab"
$DISPLAY_NAME = "VSuite Lab"

Write-Host "Packaging Windows ($RID) version $VERSION"

# ----------------------------
# Paths
# ----------------------------
$ROOT = $PWD
$PUBLISH_DIR = Join-Path $ROOT "publish\$RID"

$OUTPUT_DIR = Join-Path $ROOT "Build\Packaging\Output"
New-Item -ItemType Directory -Force -Path $OUTPUT_DIR | Out-Null

$ICON_PATH = Join-Path $ROOT "Assets\icon.ico"

# ----------------------------
# Inno Setup
# ----------------------------
$TOOLS_DIR = Join-Path $ROOT ".tools"
$IS_DIR = Join-Path $TOOLS_DIR "innosetup"
$ISCC = Join-Path $IS_DIR "ISCC.exe"
$IS_URL = "https://github.com/jrsoftware/issrc/releases/download/is-7_1_0/innosetup-7.1.0-x64.exe"

if (-not (Test-Path $ISCC)) {
    Write-Host "Installing Inno Setup..."

    New-Item -ItemType Directory -Force -Path $IS_DIR | Out-Null
    $installer = Join-Path $TOOLS_DIR "is.exe"

    Invoke-WebRequest -Uri $IS_URL -OutFile $installer

    Start-Process -FilePath $installer `
        -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR=$IS_DIR" `
        -Wait

    Remove-Item $installer -Force
}

# ----------------------------
# Ensure icon exists
# ----------------------------
if (-not (Test-Path $ICON_PATH)) {
    throw "Missing Assets/icon.ico"
}

# Copy icon into app folder
Copy-Item $ICON_PATH (Join-Path $PUBLISH_DIR "icon.ico") -Force

# ----------------------------
# Generate installer script
# ----------------------------
$ISS_PATH = Join-Path $ROOT "Build\Packaging\generated-$RID.iss"

$EXE_NAME = "VSuiteLab.exe"

$ISS = @"
[Setup]
AppName=$DISPLAY_NAME
AppVersion=$VERSION
DefaultDirName={pf}\$DISPLAY_NAME
DefaultGroupName=$DISPLAY_NAME
OutputDir=$OUTPUT_DIR
OutputBaseFilename=$APP_NAME-$RID
SetupIconFile="$ICON_PATH"
Compression=lzma
SolidCompression=yes

[Files]
Source: "$PUBLISH_DIR\*"; DestDir: "{app}"; Flags: recursesubdirs
Source: "$ICON_PATH"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\$DISPLAY_NAME"; Filename: "{app}\$EXE_NAME"; IconFilename: "{app}\icon.ico"
Name: "{commondesktop}\$DISPLAY_NAME"; Filename: "{app}\$EXE_NAME"; IconFilename: "{app}\icon.ico"

[Run]
Filename: "{app}\$EXE_NAME"; Description: "Launch $DISPLAY_NAME"; Flags: nowait postinstall skipifsilent
"@

Set-Content -Path $ISS_PATH -Value $ISS -Encoding UTF8

# ----------------------------
# Build installer
# ----------------------------
Write-Host "Building installer..."
& $ISCC $ISS_PATH

Write-Host "Done → $OUTPUT_DIR"