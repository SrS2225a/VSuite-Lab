#!/usr/bin/env bash
set -e

APP_NAME="VSuiteLab"
DISPLAY_NAME="VSuite Lab"
BINARY="VSuiteLab"

RID="$1"
VERSION="$2"

PUBLISH_DIR=publish/$RID

# ----------------------------
# App bundle structure
# ----------------------------
APP_DIR="$DISPLAY_NAME-$RID.app/Contents"
mkdir -p "$APP_DIR/MacOS"
mkdir -p "$APP_DIR/Resources"

cp "$PUBLISH_DIR/$BINARY" "$APP_DIR/MacOS/"
cp Build/Packaging/Info.plist "$APP_DIR/"

mkdir icon.iconset

sips -z 16 16     Assets/icon.png --out icon.iconset/icon_16x16.png
sips -z 32 32     Assets/icon.png --out icon.iconset/icon_16x16@2x.png
sips -z 32 32     Assets/icon.png --out icon.iconset/icon_32x32.png
sips -z 64 64     Assets/icon.png --out icon.iconset/icon_32x32@2x.png
sips -z 128 128   Assets/icon.png --out icon.iconset/icon_128x128.png
sips -z 256 256   Assets/icon.png --out icon.iconset/icon_128x128@2x.png
sips -z 256 256   Assets/icon.png --out icon.iconset/icon_256x256.png
sips -z 512 512   Assets/icon.png --out icon.iconset/icon_256x256@2x.png
sips -z 512 512   Assets/icon.png --out icon.iconset/icon_512x512.png
sips -z 1024 1024 Assets/icon.png --out icon.iconset/icon_512x512@2x.png

iconutil -c icns icon.iconset -o Assets/icon.icns
rm -rf icon.iconset

cp Assets/icon.icns "$APP_DIR/Resources/"

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