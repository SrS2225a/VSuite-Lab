$ErrorActionPreference = "Stop"

$APP_NAME = "VSuiteLab"
$DISPLAY_NAME = "VSuite Lab"
$VERSION = "1.0.0"

$RID = $args[0]
if (-not $RID) {
    throw "Missing RID (win-x64 / win-arm64)"
}

Write-Host "Building $DISPLAY_NAME ($RID)..."

# ----------------------------
# Paths
# ----------------------------
$PUBLISH_DIR = Join-Path $PWD "publish\$RID"

$TOOLS_DIR = Join-Path $PWD ".tools"
$IS_DIR = Join-Path $TOOLS_DIR "innosetup"
$ISCC = Join-Path $IS_DIR "ISCC.exe"
$IS_URL = "https://jrsoftware.org/download.php/is.exe"

# ----------------------------
# Publish
# ----------------------------
dotnet publish -c Release -f net8.0 -r $RID `
    -p:SelfContained=true `
    -p:UseAppHost=true `
    -p:PublishSingleFile=true `
    -o $PUBLISH_DIR

# ----------------------------
# Ensure Inno Setup exists (FIXED + COMPLETE)
# ----------------------------
if (-not (Test-Path $ISCC)) {
    Write-Host "Inno Setup not found. Downloading..."

    New-Item -ItemType Directory -Force -Path $IS_DIR | Out-Null

    $installer = Join-Path $TOOLS_DIR "is.exe"

    Invoke-WebRequest -Uri $IS_URL -OutFile $installer

    Write-Host "Installing Inno Setup silently..."

    Start-Process -FilePath $installer `
        -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR=$IS_DIR" `
        -Wait

    Remove-Item $installer -Force
}

if (-not (Test-Path $ISCC)) {
    throw "Failed to install Inno Setup"
}

# ----------------------------
# Generate .iss (safe approach)
# ----------------------------
$ISS_PATH = Join-Path $PWD "Build\Packaging\generated-$RID.iss"

$ISS = @"
[Setup]
AppName=$DISPLAY_NAME
AppVersion=$VERSION
DefaultDirName={pf}\$DISPLAY_NAME
DefaultGroupName=$DISPLAY_NAME
OutputBaseFilename=$APP_NAME-$RID

[Files]
Source: "$PUBLISH_DIR\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\$DISPLAY_NAME"; Filename: "{app}\vsuitelab.exe"
Name: "{commondesktop}\$DISPLAY_NAME"; Filename: "{app}\vsuitelab.exe"
"@

Set-Content -Path $ISS_PATH -Value $ISS -Encoding UTF8

# ----------------------------
# Build installer
# ----------------------------
Write-Host "Running Inno Setup compiler..."
& $ISCC $ISS_PATH