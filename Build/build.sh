#!/usr/bin/env bash
set -e

APP_NAME="VSuiteLab"
DISPLAY_NAME="VSuite Lab"

RID="$1"
VERSION="$2"

if [ -z "$RID" ] || [ -z "$VERSION" ]; then
  echo "Usage: build.sh <RID> <VERSION>"
  exit 1
fi

PUBLISH_DIR="publish/$RID"

echo "Building $APP_NAME ($RID) version $VERSION"

# ----------------------------
# Publish
# ----------------------------
dotnet publish -c Release -f net8.0 -r "$RID" \
  -p:SelfContained=true \
  -p:UseAppHost=true \
  -p:PublishSingleFile=true \
  -p:Version="$VERSION" \
  -p:InformationalVersion="$VERSION" \
  -o "$PUBLISH_DIR"

# ----------------------------
# Platform-specific packaging
# ----------------------------
case "$RID" in
  linux-*)
    ./Build/pack-linux.sh "$RID" "$VERSION"
    ;;
  win-*)
    pwsh -NoProfile -ExecutionPolicy Bypass -File ./Build/pack-win.ps1 \
      -RID "$RID" \
      -VERSION "$VERSION"
    ;;
  osx-*)
    ./Build/pack-macos.sh "$RID" "$VERSION"
    ;;
  *)
    echo "Unknown RID: $RID"
    exit 1
    ;;
esac