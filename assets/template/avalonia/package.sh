#!/bin/sh
# The package.bat twin for Linux: tests, a self-contained publish, then a tarball, a .deb (needs
# dpkg-deb) and an .rpm (needs rpmbuild) in artifacts/linux. macOS uses package-macos.sh.
set -e
cd "$(dirname "$0")"
./test.sh
VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props)
OUT=artifacts/linux
STAGE=$OUT/stage
rm -rf "$OUT"
if ! dotnet publish src/__Product__.Desktop/__Product__.Desktop.csproj -c Release -r linux-x64 --self-contained true \
  -p:PublishReadyToRun=true -o "$OUT/publish" -v q -nologo > artifacts/logs/package.log 2>&1; then
  echo "[package] FAILED - error lines below, full output in artifacts/logs/package.log"
  ./logs.sh package error
  exit 1
fi

# The file system tree both packages install: the app in /opt, a command, a menu entry and icon.
mkdir -p "$STAGE/opt/__product__" "$STAGE/usr/bin" "$STAGE/usr/share/applications" "$STAGE/usr/share/icons/hicolor/256x256/apps"
cp -a "$OUT/publish/." "$STAGE/opt/__product__/"
ln -s /opt/__product__/__Product__ "$STAGE/usr/bin/__product__"
cp packaging/linux/__product__.desktop "$STAGE/usr/share/applications/"
cp packaging/linux/__product__.png "$STAGE/usr/share/icons/hicolor/256x256/apps/"

# Tarball: the app folder plus the menu entry and icon, for any distribution.
TAR=__product__-$VERSION
mkdir -p "$OUT/$TAR"
cp -a "$OUT/publish/." packaging/linux/__product__.desktop packaging/linux/__product__.png "$OUT/$TAR/"
tar -C "$OUT" -czf "$OUT/$TAR-linux-x64.tar.gz" "$TAR"

# Debian and Ubuntu. X11 and fontconfig are Avalonia's; ICU and OpenSSL are the .NET runtime's.
if command -v dpkg-deb >/dev/null 2>&1; then
  mkdir -p "$STAGE/DEBIAN"
  printf '%s\n' "Package: __product__" "Version: $VERSION" "Architecture: amd64" "Maintainer: __Company__" \
    "Depends: libx11-6, libice6, libsm6, libfontconfig1, libicu72 | libicu74 | libicu76 | libicu77 | libicu78, libssl3 | libssl3t64" \
    "Section: utils" "Priority: optional" "Description: __Product__" > "$STAGE/DEBIAN/control"
  dpkg-deb --root-owner-group --build "$STAGE" "$OUT/__product___${VERSION}_amd64.deb" >/dev/null
  rm -rf "$STAGE/DEBIAN"
else
  echo "[package] no .deb: dpkg-deb is not installed"
fi

# Fedora and openSUSE.
if command -v rpmbuild >/dev/null 2>&1; then
  rpmbuild -bb packaging/linux/__product__.spec --quiet --nodeps --define "_topdir $(pwd)/$OUT/rpmbuild" \
    --define "version $VERSION" --define "stage $(pwd)/$STAGE"
  cp "$OUT"/rpmbuild/RPMS/*/__product__-*.rpm "$OUT/"
else
  echo "[package] no .rpm: rpmbuild is not installed"
fi

echo "[package] OK: $OUT"
