# __Product__

TODO: one sentence on what the app does and who it's for.

## Requirements

- Windows 10 22H2 or Windows 11 (x64), Linux x64 (X11, or Wayland through XWayland), or macOS 15 or later
- For development: the .NET 10 SDK version pinned in `global.json`

## Scripts

Each `.bat` script has a `.sh` twin for Linux and macOS (`./test.sh`); change both together.

| Script | Purpose |
|---|---|
| `run.bat` / `run.sh` | Run the app in Debug |
| `build.bat` / `build.sh` | Publish a self-contained app to `artifacts/publish/<rid>` |
| `test.bat` / `test.sh` | Run all tests |
| `package.bat` | Test, build and create the MSI in `artifacts\installer` |
| `package.sh` | Linux: test, then a tarball, `.deb` and `.rpm` in `artifacts/linux` |
| `package-macos.sh` | macOS: test, then an ad-hoc-signed `.app` and `.dmg` per CPU in `artifacts/macos` |
| `logs.bat` / `logs.sh` | Show the error lines of a script's last log |
| `clean.bat` / `clean.sh` | Remove all build output |

Pass `--no-pause` to the `.bat` scripts to skip the final key press (used by automation).

## Project structure

| Project | Responsibility |
|---|---|
| `src/__Product__.Desktop` | __UiStack__ UI: views, startup, theme, assets |
| `src/__Product__.Presentation` | View models, settings, localization, portable services (no UI types) |
| `src/__Product__.Core` | Domain models, interfaces, pure logic (no UI, no SQL) |
| `src/__Product__.Data` | SQLite + Dapper repositories and migrations |
| `tests/*` | xUnit v3 tests per project; Desktop tests run on Avalonia.Headless |
| `installer` | WiX v7 MSI (Windows) |
| `packaging` | Linux menu entry, icon and rpm spec; macOS `Info.plist` |

Details: [docs/architecture.md](docs/architecture.md).

## Data locations

All user data is in `%LOCALAPPDATA%\__Product__\` on Windows, `~/.local/share/__Product__/` on Linux and `~/Library/Application Support/__Product__/` on macOS:

| Path | Contents |
|---|---|
| `data.db` | SQLite database |
| `backups` | Automatic backups (newest 14) |
| `logs` | Daily log files (newest 14) |
| `cache` | Rebuildable cache, safe to delete |
| `settings.json` | User settings |

Uninstalling keeps this folder.

## Localization

Arabic is the default language and English is secondary. Strings live in `src/__Product__.Presentation/Resources/Strings.resx` (Arabic) and `Strings.en.resx` (English) with identical keys. Use `{l:Tr Key=Key}` in XAML and `Tr.Get("Key")` in C#. See [docs/localization.md](docs/localization.md).

## Contributing

- Commit title: `<project>: <type>: <short title>`, 60 characters at most, imperative, English. Projects: `app`, `core`, `data`, `tests`, `installer`, `docs`, `repo`. Types: `feat`, `fix`, `perf`, `refactor`, `test`, `docs`, `build`, `chore`, `i18n`, `style`.
- Commit body: what changed and why, wrapped at 72 characters.
- Before committing, `build` and `test` must pass, and the affected docs must be updated.
- Decisions: [docs/decisions](docs/decisions).

## Releases

Version is set in `Directory.Build.props`. Process: [docs/release.md](docs/release.md). History: [CHANGELOG.md](CHANGELOG.md).

## License

TODO: state the license, and the same SPDX id in `packaging/linux/__product__.spec`. Bundled fonts are under the SIL Open Font License (`src/__Product__.Desktop/Assets/Fonts/OFL.txt`).
