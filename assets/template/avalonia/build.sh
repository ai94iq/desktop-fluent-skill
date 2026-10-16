#!/bin/sh
# The build.bat twin: a self-contained publish for this machine's OS and CPU (linux-x64,
# osx-arm64, ...) in artifacts/publish/<rid>. Full output in artifacts/logs/build.log.
set -e
cd "$(dirname "$0")"
case "$(uname -s)" in
  Darwin) os=osx ;;
  *) os=linux ;;
esac
case "$(uname -m)" in
  arm64|aarch64) arch=arm64 ;;
  *) arch=x64 ;;
esac
OUT=artifacts/publish/$os-$arch
mkdir -p artifacts/logs
rm -rf "$OUT"
if ! dotnet publish src/__Product__.Desktop/__Product__.Desktop.csproj -c Release -r "$os-$arch" \
  --self-contained true -p:PublishReadyToRun=true -o "$OUT" -v q -nologo > artifacts/logs/build.log 2>&1; then
  echo "[build] FAILED - error lines below, full output in artifacts/logs/build.log"
  ./logs.sh build error
  exit 1
fi
echo "[build] OK: $OUT/__Product__"
