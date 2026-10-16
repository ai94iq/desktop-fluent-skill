#!/bin/sh
# The test.bat twin. Full output in artifacts/logs/test.log; the screen gets one line, or the
# error lines on failure. CI on Linux runs it under xvfb-run for tests that need a display.
set -e
cd "$(dirname "$0")"
mkdir -p artifacts/logs
if ! dotnet test --solution __Product__.slnx -c Release > artifacts/logs/test.log 2>&1; then
  echo "[test] FAILED - error lines below, full output in artifacts/logs/test.log"
  ./logs.sh test error
  exit 1
fi
echo "[test] OK"
