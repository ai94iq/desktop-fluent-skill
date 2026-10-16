#!/bin/sh
# The new-project.bat twin for Linux and macOS: scaffolds a complete project from the skill template.
# Example: scripts/new-project.sh MyApp "My Company" ~/Projects/MyApp --ui avalonia
set -e
if [ -z "$3" ]; then
  echo "Usage: new-project.sh ProductName \"Company Name\" TargetFolder [--ui wpf|winui|avalonia] [--no-build]"
  exit 1
fi
skill="$(cd "$(dirname "$0")/.." && pwd)"
case "$3" in
  /*) target="$3" ;;
  *) target="$(pwd)/$3" ;;
esac
product="$1"
company="$2"
shift 3
# Run from the skill root so the helper compiles with the skill's .NET 10 SDK, not the caller's.
cd "$skill"
exec dotnet run "$skill/scripts/new-project.cs" -- "$skill/assets/template" "$product" "$company" "$target" "$@"
