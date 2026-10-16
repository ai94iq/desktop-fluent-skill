#!/bin/sh
# The run.bat twin: starts the app in Debug and waits until it is closed.
set -e
cd "$(dirname "$0")"
dotnet run --project src/__Product__.Desktop/__Product__.Desktop.csproj -c Debug -v q || { echo "[run] FAILED"; exit 1; }
