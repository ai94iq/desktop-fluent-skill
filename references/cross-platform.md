# Cross-platform apps: Windows, Linux and macOS

Read this when the product must run on Linux or macOS as well as Windows. The UI is then Avalonia (`ui-frameworks.md` section 7), and everything below the views stays the same as in a Windows-only app: Core, Data, SQLite with Dapper, `Tr` and resx, `PageViewModel`, Serilog, the Generic Host and the test-first rules. These rules come from an app that moved from WinUI 3 to Avalonia and shipped on all three systems.

## Contents

1. Decision and scope
2. Projects
3. Platform interfaces
4. Native services per OS
5. Data, keys and single instance
6. Scripts: .bat and .sh twins
7. Packaging per OS
8. CI on three systems
9. Tests per OS
10. Pitfalls
11. Porting an existing app

## 1. Decision and scope

- Choose Avalonia at scaffold time when Linux or macOS is a requirement, and record it in the ADR. Don't port a WPF or WinUI app "in case": a port rewrites every view.
- Supported targets: Windows 10/11 x64, Linux x64 (X11, and Wayland through XWayland), macOS 15 or later on Apple Silicon and Intel. macOS 15 is the oldest release on the .NET 10 supported-OS list; check that list before lowering or raising the floor.
- MAUI and Uno stay out. Flatpak and Snap stay out too: their sandboxes block clipboard watching, global hotkeys and sending keys to other apps.

## 2. Projects

| Project | Target | Holds | Never holds |
|---|---|---|---|
| `MyApp.Core` | `net10.0` | Models, interfaces, pure logic | UI or SQL |
| `MyApp.Data` | `net10.0` | SQLite + Dapper repositories, migrations, backups | UI |
| `MyApp.Presentation` | `net10.0` | ViewModels, settings, `Tr` and resx, portable services, the platform interfaces | Avalonia, Win32 or any OS type |
| `MyApp.Desktop` | `net10.0` | Avalonia views, `Themes/`, `Platform/Windows`, `Platform/Linux`, `Platform/MacOS`, `Hosting/ServiceRegistration.cs` | Business rules |

- Every project targets plain `net10.0`, never `net10.0-windows`. OS-only code is guarded with `[SupportedOSPlatform]` and `OperatingSystem.Is*()`, so the platform analyzer catches a Win32 call reachable from Linux.
- ViewModels move out of the UI project into Presentation. That is what lets Presentation tests run on every OS without a display.
- Tests: `MyApp.Core.Tests`, `MyApp.Data.Tests`, `MyApp.Presentation.Tests` (no UI) and `MyApp.Desktop.Tests` (Avalonia headless, section 9).

## 3. Platform interfaces

An OS feature is an interface in Presentation (or in Desktop when only views use it) with one implementation per OS under `Desktop/Platform/<OS>/`. Code shared by two OSes (the POSIX ones, usually) sits in `Platform/` itself. `ServiceRegistration` picks the set once at startup:

```csharp
public static IServiceCollection AddPlatformServices(this IServiceCollection services)
{
    services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();   // shared by every OS
    if (OperatingSystem.IsWindows()) return services.AddWindows();
    if (OperatingSystem.IsLinux()) return services.AddLinux();
    if (OperatingSystem.IsMacOS()) return services.AddMacOS();
    throw new PlatformNotSupportedException("MyApp runs on Windows, Linux and macOS.");
}

[SupportedOSPlatform("linux")]
private static IServiceCollection AddLinux(this IServiceCollection services) { /* ... */ }
```

- No `if (OperatingSystem.IsMacOS())` inside a ViewModel or view. A behavior that differs per OS is a member of a platform interface, or a capability flag on one (`IPasteBack.CanPaste`), and the view shows or hides its option from that.
- Keep the rules in one shared class and put only the OS call behind the interface. Example: one `ClipboardService` holds the capture rules for every OS over an `IClipboardBackend` with a Win32, an X11 and a macOS implementation.
- Settings that exist on one OS only (Mica and Acrylic, the Windows tray double-click) are hidden on the others, not shown disabled.
- `IUiDispatcher` wraps `Dispatcher.UIThread`; ViewModels post through it, never through an Avalonia type.

## 4. Native services per OS

What each OS offers for the services a desktop utility usually needs, and the limit to design around:

| Service | Windows | Linux | macOS |
|---|---|---|---|
| Clipboard change | `AddClipboardFormatListener` | XFixes selection events (X11 and XWayland) | No event: poll `NSPasteboard.changeCount` every 300 ms |
| Clipboard read/write | Win32 or Avalonia | Avalonia | Avalonia |
| Global hotkey | `RegisterHotKey` | `XGrabKey`; on Wayland it fires only while an app window has focus | Carbon `RegisterEventHotKey`, no permission needed |
| Send keys to another app | `SendInput` | XTest on X11; impossible on Wayland | `CGEventPost`, needs the Accessibility permission |
| Active app name | `GetForegroundWindow` + process | X11 `_NET_ACTIVE_WINDOW`; blank for Wayland apps | `NSWorkspace.frontmostApplication` |
| Secret storage | DPAPI (current user) | Secret Service via libsecret | Login Keychain |
| Run at sign-in | `HKCU\...\Run` value | `~/.config/autostart/<app>.desktop` | Per-user LaunchAgent plist |
| Session end | `WM_ENDSESSION` on a hidden top-level window | SIGTERM | SIGTERM and Avalonia `ShutdownRequested` |
| Tray | Avalonia `TrayIcon` | Avalonia `TrayIcon` (StatusNotifierItem); needs a `StatusNotifierWatcher` on D-Bus | Avalonia `TrayIcon` in the menu bar |
| System theme | Registry + `WM_SETTINGCHANGE` | Avalonia `PlatformSettings` | Avalonia `PlatformSettings` |

Design the product around the weakest column and tell the user in the UI:
- Wayland: explain in Settings and the welcome window how to bind a system shortcut to `myapp --toggle`, and after a pick that can't paste, show a hint to press Ctrl+V.
- GNOME without the AppIndicator extension has no tray. Detect the missing watcher and open the main window at launch instead.
- macOS without the Accessibility permission: copy the pick, show the Cmd+V hint, and ask for the permission once (`AXIsProcessTrustedWithOptions`).

## 5. Data, keys and single instance

- Data folder: `%LOCALAPPDATA%\MyApp` on Windows, `~/.local/share/MyApp` on Linux (`$XDG_DATA_HOME` when set), `~/Library/Application Support/MyApp` on macOS. `AppPaths` is the only place that knows them.
- Keyring keys: one `KeyringKeyStore` with the same rules over libsecret and the Keychain. Without a keyring service (minimal desktops, WSL), fall back to a key file readable only by the user (mode 0600) and log a warning. When the key is recorded as being in the keyring and the keyring doesn't return it (locked, denied), stop with a clear message. Never create a new key then, or the existing data becomes unreadable.
- Single instance: a named pipe is a Unix socket on Linux and macOS. Name it per user (not per `SessionId`, which differs per terminal there), and claim it with an `flock` lock file, because another process can re-bind a socket path. macOS caps socket paths at 104 characters and its temp folder is long, so use a short hashed path.
- Set `Application.Name`. The Keychain prompt and the macOS menu show it, and otherwise read "Avalonia Application".

## 6. Scripts: .bat and .sh twins

- Every repo script has a POSIX twin with the same name and job: `build`, `test`, `run`, `clean` and `package`. macOS adds `package-macos.sh` (it needs `codesign`, `iconutil` and `hdiutil`).
- `.sh` files are `#!/bin/sh` with `set -e` and `cd "$(dirname "$0")"`, LF line endings, and the same 60-line budget as `.bat`. Set `.gitattributes`: `*.bat eol=crlf`, `*.sh eol=lf`, plus `eol=lf` for `.plist`, `.spec` and `.desktop`.
- Twins stay in step: when one changes, change the other in the same commit. `package.sh` runs `test.sh` first, as `package.bat` runs `test.bat`. The `--no-pause` flag exists only on `.bat`, since shell scripts never pause.
- `build.sh` picks the runtime identifier from `uname -s` and `uname -m` (`linux-x64`, `osx-arm64`, ...). It publishes self-contained into `artifacts/publish/<rid>`.

## 7. Packaging per OS

| OS | Output | Built by | Notes |
|---|---|---|---|
| Windows | MSI (`installer.md`), winget | `package.bat` | Unchanged from a Windows-only app |
| Linux | tarball, `.deb`, `.rpm` | `package.sh` (needs `dpkg-deb`, `rpmbuild`) | One staged tree: the app in `/opt/myapp`, a `/usr/bin/myapp` symlink, a `.desktop` entry and a 256 px icon. List the native libraries in `Depends` (libX11, libXtst, libXfixes, libsecret, ICU, OpenSSL) |
| macOS | `MyApp.app` and a `.dmg` per architecture | `package-macos.sh` | `Info.plist` with `LSMinimumSystemVersion`; icon via `sips` and `iconutil`; ad-hoc `codesign --sign -`, then `plutil -lint` and `codesign --verify --deep --strict` |

- Without an Apple Developer ID the app is not notarized, and Gatekeeper blocks the first launch. The README explains Privacy & Security > Open Anyway. Ad-hoc signatures change with every build, so after each update macOS asks again for Keychain access and the Accessibility permission. List both in `docs/known-issues.md`.
- A rename or a replaced package declares `Conflicts`/`Replaces` (deb) and `Obsoletes` (rpm), so the old package doesn't stay installed beside the new one.

## 8. CI on three systems

- One workflow with three jobs: `windows-latest` runs `.\test.bat --no-pause`, `ubuntu-latest` runs `xvfb-run -a ./test.sh` after installing `xvfb` and the native libraries the tests load (`libsecret-1-0`), and `macos-latest` runs `./test.sh` then `./package-macos.sh`.
- Set up .NET from `global.json` (`global-json-file`), and cache `~/.nuget/packages` keyed on `Directory.Packages.props`.
- Job logs need a signed-in reader. On failure, the Linux and macOS jobs echo the failing tests as an `::error` annotation (strip color codes, take the lines after `failed`), so the result can be read without signing in.
- Release jobs for Linux and macOS depend on the Windows release job, and every release job runs only after the test jobs pass for that commit (`installer.md` section 9).
- macOS runners are the only Mac many teams have. Live tests there (Keychain, pasteboard, frontmost app, hotkey registration) are the proof that the macOS layer works. Say so in the ADR, and mark interactive behavior as unverified until someone runs it on a Mac.

## 9. Tests per OS

- Presentation, Core and Data tests run everywhere with no display.
- Desktop tests use `Avalonia.Headless` directly: an `[assembly: AvaloniaTestApplication(typeof(TestApp))]` whose `BuildAvaloniaApp()` calls `.UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })`, and a helper that runs each test body on `HeadlessUnitTestSession.GetOrStartForAssembly(...)`. Before using `Avalonia.Headless.XUnit`, check it discovers tests on the installed xunit.v3 major. Version 12.1.4 found none on xunit.v3 4.x.
- An OS-only test starts with `Assert.SkipUnless(OperatingSystem.IsLinux(), "libsecret")`, never with `#if` or a trait. The run then reports it as skipped, not as missing.
- Live X11 tests skip without `DISPLAY` and skip on Wayland (`WAYLAND_DISPLAY` set). CI gives them a server through `xvfb-run`.
- A test that changes process-wide state (`CultureInfo.DefaultThreadCurrentCulture`, environment variables) goes in a non-parallel collection. On a 2-core CI runner it races the others.
- Timing windows in tests are at least 500 ms. 30-40 ms windows passed on a laptop and failed on macOS runners.

## 10. Pitfalls

macOS:
- `objc_getClass` finds `NSPasteboard` and `NSWorkspace` only once AppKit is loaded. The app gets it from Avalonia, but a test host doesn't, so load it once yourself (`NativeLibrary.TryLoad("/System/Library/Frameworks/AppKit.framework/AppKit", out _)`). Otherwise a test passes only when an earlier test happened to load it.
- Starting while the display sleeps aborts Avalonia's render timer (CVReturn -6661). Wait for the display in `Main`, and log and flush unhandled exceptions so a crash leaves a trace.
- A menu-bar app sets its activation policy to accessory at launch (`ShowInDock` off alone didn't hold after a login start) and to regular (Dock icon, its own menu) while its main window is open. Don't set `LSUIElement` in `Info.plist`: with it, macOS ignored the switch.
- Keep the system traffic lights (`WindowDecorations.Full`). Hide the Windows-style caption buttons and the app name in the title bar, since the menu bar already shows the name.

Linux:
- Under Wayland, Avalonia runs through XWayland. The XWayland bridge mirrors Wayland-side copies to X, so one X11 layer serves both session types. Source app names stay blank for Wayland apps, and that is expected.
- Messages sent before a listener subscribes are lost (single-instance commands at startup). Queue them until the first subscriber.
- Develop and test on WSLg or Xvfb, but list tray, keyring, activation and paste-back on real GNOME and KDE as unverified in `docs/known-issues.md` until someone checks them.

All systems:
- A static initializer in `AppPaths` that migrates or moves folders runs against the developer's real data the moment a test touches `AppPaths`. Keep migrations in their own class and test them on temp folders only.
- A product rename keeps its data: move the old data folder on first start, read the old keyring entry when the new one is empty, and claim the old single-instance name first so old and new never run together.

## 11. Porting an existing app

- Port one surface per batch (shell, then each page, then dialogs, then theming) and get the owner's review of each batch on screen before the next one starts. A first port went through three visual directions with each view picking its own colors, was judged broken everywhere, and was restarted under these rules.
- One token layer: `FluentTheme` is the only theme, views use `{DynamicResource}` Fluent keys, and app-owned colors live only in `Themes/Exceptions.axaml`, each with a comment naming the Fluent gap it fills. A token test fails on a literal color outside that file (`ui-frameworks.md` section 7).
- The old UI project stays in git history and is deleted at the swap commit, not kept alongside.
- Testing in a macOS VM: build and install over SSH if you like, but start the app from the VM's own Terminal or Spotlight and quit it with Cmd+Q. Apps opened from an SSH session never finish checking in with LaunchServices: the Dock icon bounces, activation calls fail, and killed instances leave ghosts until a reboot. Hours went into "fixing" app behavior that was only this.
