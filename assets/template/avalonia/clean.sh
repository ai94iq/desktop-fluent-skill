#!/bin/sh
# The clean.bat twin: removes every bin, obj and the artifacts folder.
set -e
cd "$(dirname "$0")"
find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
rm -rf artifacts
echo "[clean] OK"
