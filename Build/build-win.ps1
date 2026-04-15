$ErrorActionPreference = "Stop"

$APP_NAME = "vsuitelab"
$DISPLAY_NAME = "VSuite Lab"
$VERSION = "1.0.0"

$RIDS = @("win-x64", "win-arm64")

# ----------------------------
# Config: Inno Setup download
# ----------------------------
$TOOLS_DIR = "$PSScriptRoot\.tools"
$IS_DIR = "$TOOLS_DIR\innosetup"
$ISCC = "$IS_DIR\ISCC.exe"
$IS_URL = "https://jrsoftware.org/download.php/is.exe"

# ----------------------------
# Ensure Inno Setup exists
# ----------------------------
if (-not (Test-Path $ISCC)) {
    Write-Host "Inno Setup not found. Downloading..."

    New-Item -ItemType Directory -Force -Path $IS_DIR | Out-Null

    $installer = "$TOOLS_DIR\is.exe"

    Invoke-WebRequest -Uri $IS_URL -OutFile $installer

    Write-Host "Installing Inno Setup silently..."

    Start-Process -FilePath $installer -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR=$IS_DIR" -Wait

    Remove-Item $installer -Force
}

if (-not (Test-Path $ISCC)) {
    throw "Failed to install Inno Setup"
}

# ----------------------------
# Build loop
# ----------------------------
$RID = $args[0]

Write-Host "Building $DISPLAY_NAME ($RID)..."

$PUBLISH_DIR = "publish/$RID"

dotnet publish -c Release -f net8.0 -r $RID `
    --self-contained true `
    -p:UseAppHost=true `
    -o $PUBLISH_DIR

& $ISCC `
    "/DAppName=$DISPLAY_NAME" `
    "/DAppVersion=$VERSION" `
    "/DAppRid=$RID" `
    "/DPublishDir=$PUBLISH_DIR" `
    "Build\Packaging\VSuiteLab.iss"