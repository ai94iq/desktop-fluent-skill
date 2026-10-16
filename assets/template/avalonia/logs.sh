#!/bin/sh
# The logs.bat twin. Usage: ./logs.sh <script> [error|all]
#   <script> = log name under artifacts/logs without extension (test, build, package).
#   Default kind: error, which prints the error and warning lines only.
cd "$(dirname "$0")"
if [ -z "$1" ]; then
  echo "Usage: ./logs.sh <script> [error|all]"
  exit 1
fi
LOG=artifacts/logs/$1.log
if [ ! -f "$LOG" ]; then
  echo "[logs] no such log: $LOG"
  exit 1
fi
if [ "$2" = "all" ]; then
  cat "$LOG"
  exit 0
fi
grep -i -E "error|warning|failed" "$LOG" || true
echo
echo "[logs] full log: $LOG"
