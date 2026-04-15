#!/usr/bin/env bash
set -e

APP_NAME="vsuitelab"
DISPLAY_NAME="VSuite Lab"
BINARY="vsuitelab"
VERSION="1.0.0"

RID="$1"

PUBLISH_DIR=publish/$RID

dotnet publish -c Release -f net8.0 -r $RID \
  -p:SelfContained=true \
  -p:UseAppHost=true \
  -p:PublishSingleFile=true \
  -o $PUBLISH_DIR

# ----------------------------
# App bundle structure
# ----------------------------
APP_DIR="$DISPLAY_NAME-$RID.app/Contents"
mkdir -p "$APP_DIR/MacOS"
mkdir -p "$APP_DIR/Resources"

cp "$PUBLISH_DIR/$BINARY" "$APP_DIR/MacOS/"
cp Build/Packaging/Info.plist "$APP_DIR/"

chmod +x "$APP_DIR/MacOS/$BINARY"

# ----------------------------
# Package
# ----------------------------
pkgbuild \
  --root "$DISPLAY_NAME-$RID.app" \
  --identifier "com.vsuitelab.app" \
  --version "$VERSION" \
  --install-location "/" \
  "${APP_NAME}-${RID}.pkg"