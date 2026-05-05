#!/usr/bin/env bash
set -e

APP_NAME="VSuiteLab"
DISPLAY_NAME="VSuite Lab"
RID="$1"
VERSION="$2"

PUBLISH_DIR=publish/$RID
  
# ----------------------------
# Layout (Linux standard)
# ----------------------------
mkdir -p pkg/usr/lib/vsuitelab
mkdir -p pkg/usr/bin
mkdir -p pkg/usr/share/applications
mkdir -p pkg/usr/share/icons/hicolor/512x512/apps

cp -r $PUBLISH_DIR/* pkg/usr/lib/vsuitelab/
cp Build/Packaging/$APP_NAME.desktop pkg/usr/share/applications/
cp Assets/icon.png pkg/usr/share/icons/hicolor/512x512/apps/vsuitelab.png

# ----------------------------
# Launcher (clean + minimal)
# ----------------------------
cat > pkg/usr/bin/vsuitelab <<EOF
#!/usr/bin/env bash
exec /usr/lib/vsuitelab/VSuiteLab "\$@"
EOF

chmod +x pkg/usr/bin/vsuitelab

# ----------------------------
# Architecture mapping
# ----------------------------
ARCH="amd64"
[[ "$RID" == *"arm64"* ]] && ARCH="arm64"

# ----------------------------
# DEB
# ----------------------------
fpm -s dir -t deb -n $APP_NAME -v $VERSION \
  --architecture $ARCH \
  --maintainer "SrS2225a <info@fenriris.net>" \
  --vendor "VSuite Lab" \
  --description "Get the most out of your journals, notes & tasks" \
  --prefix / \
  --package ${APP_NAME}-${RID}.deb \
  -C pkg .

# ----------------------------
# RPM
# ----------------------------
fpm -s dir -t rpm -n $APP_NAME -v $VERSION \
  --architecture $ARCH \
  --maintainer "SrS2225a <info@fenriris.net>" \
  --vendor "VSuite Lab" \
  --description "Get the most out of your journals, notes & tasks" \
  --prefix / \
  --package ${APP_NAME}-${RID}.rpm \
  -C pkg .

# ----------------------------
# Arch Linux package
# ----------------------------
fpm -s dir -t pacman -n $APP_NAME -v $VERSION \
  --architecture $ARCH \
  --maintainer "SrS2225a <info@fenriris.net>" \
  --vendor "VSuite Lab" \
  --description "Get the most out of your journals, notes & tasks" \
  --prefix / \
  --package ${APP_NAME}-${RID}.pkg.tar.zst \
  -C pkg .

rm -rf pkg