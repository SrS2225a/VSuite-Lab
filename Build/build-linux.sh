#!/usr/bin/env bash
set -e

APP_NAME="VSuiteLab"
DISPLAY_NAME="VSuite Lab"
VERSION="1.0.0"

RID="$1"

PUBLISH_DIR=publish/$RID

dotnet publish -c Release -f net8.0 -r $RID \
  -p:SelfContained=true \
  -p:UseAppHost=true \
  -p:PublishSingleFile=true \
  -o $PUBLISH_DIR
  
# ----------------------------
# Layout (Linux standard)
# ----------------------------
mkdir -p pkg/usr/lib/vsuitelab
mkdir -p pkg/usr/bin
mkdir -p pkg/usr/share/applications

cp -r $PUBLISH_DIR/* pkg/usr/lib/vsuitelab/
cp Build/Packaging/$APP_NAME.desktop pkg/usr/share/applications/

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
if [[ "$RID" == *"arm64"* ]]; then
  ARCH="arm64"
fi

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