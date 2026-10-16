#!/bin/sh
# Builds __Product__.app for Apple Silicon and Intel and a .dmg for each in artifacts/macos. Runs on
# macOS (sips, iconutil, codesign, hdiutil). Ad-hoc signed, not notarized: Apple Silicon refuses
# unsigned code, and notarizing needs an Apple Developer ID (cross-platform.md section 7).
set -e
cd "$(dirname "$0")"
./test.sh
VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props)
OUT=artifacts/macos
rm -rf "$OUT"
mkdir -p "$OUT"

# The icon set from the 256 px PNG the Linux packages use.
ICONSET="$OUT/__product__.iconset"
mkdir -p "$ICONSET"
icon() { sips -z "$1" "$1" packaging/linux/__product__.png --out "$ICONSET/$2" >/dev/null; }
icon 16 icon_16x16.png
icon 32 icon_16x16@2x.png
icon 32 icon_32x32.png
icon 64 icon_32x32@2x.png
icon 128 icon_128x128.png
icon 256 icon_128x128@2x.png
icon 256 icon_256x256.png
iconutil -c icns "$ICONSET" -o "$OUT/__product__.icns"

for rid in osx-arm64 osx-x64; do
  APP="$OUT/$rid/__Product__.app"
  mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
  dotnet publish src/__Product__.Desktop/__Product__.Desktop.csproj -c Release -r $rid \
    --self-contained true -o "$APP/Contents/MacOS" -v q -nologo > artifacts/logs/package.log 2>&1 \
    || { echo "[package] FAILED - full output in artifacts/logs/package.log"; ./logs.sh package error; exit 1; }
  sed "s/@VERSION@/$VERSION/g" packaging/macos/Info.plist > "$APP/Contents/Info.plist"
  cp "$OUT/__product__.icns" "$APP/Contents/Resources/"
  codesign --force --deep --sign - "$APP"

  # Structural checks: a valid plist, a signature that verifies, an executable where the plist says.
  plutil -lint "$APP/Contents/Info.plist"
  codesign --verify --deep --strict "$APP"
  test -x "$APP/Contents/MacOS/__Product__"

  hdiutil create -quiet -volname __Product__ -srcfolder "$APP" -ov -format UDZO "$OUT/__Product__-$VERSION-macos-${rid#osx-}.dmg"
done

echo "[package] OK: $OUT"
