# WPF + WPF-UI, WinUI 3 or Avalonia

## Contents
1. Decision rule
2. Plain-language summary for the user
3. Comparison
4. What changes in code with WinUI 3
5. Control equivalents
6. Tray icon (optional)
7. Avalonia rules

## 1. Decision rule

The UI framework is chosen once, when the project is created (`new-project.bat ... --ui wpf|winui|avalonia`), and never changed afterwards. Switching later means rewriting every screen.

- Default to **WPF + WPF-UI**.
- Use **WinUI 3** only if the user explicitly wants it after hearing the summary below, or needs something only WinUI does well: touch and pen as the main input, or the exact Windows 11 look and motion.
- Use **Avalonia** when the product must also run on Linux or macOS. It's the only choice for that; read `cross-platform.md` with it.
- For apps that draw heavily (thousands of shapes), keep WPF: its custom-drawing path (`DrawingVisual`) handles that volume, and WinUI would need an extra library (Win2D) that isn't in the stack.

If the user hasn't chosen, ask once, using the summary below: recommend WPF-UI for Windows only, Avalonia when Linux or macOS is wanted.

## 2. Plain-language summary for the user

Relay this in the user's language, in your own words, in three or four short lines:

- **WPF-UI** (recommended): mature and fast to build with. It looks modern like Windows 11 and works the same on Windows 10. Smaller download. Supports Arabic-Indic digits. Best for apps with many forms, lists and large drawings.
- **WinUI 3**: Microsoft's newest technology, with the most native Windows 11 look, smoother animations and better touch support. The installer is noticeably bigger, development is slower, there are fewer ready-made add-ons, and some features (Arabic-Indic digits, very large custom drawings) need extra work.
- **Avalonia**: one app for Windows, Linux and macOS, with a Fluent look on all three. The Windows-only extras (Mica and Acrylic backgrounds, the exact Windows 11 controls) are partly missing or rebuilt, and each system needs its own testing and installer.

## 3. Comparison

| Topic | WPF + WPF-UI 4.x | WinUI 3 (Windows App SDK 2.x) | Avalonia 12 (Fluent theme) |
|---|---|---|
| Maturity | WPF since 2006, very stable; WPF-UI is a community library | Microsoft's current native UI; still evolving | Open source, cross-platform; Fluent theme is Fluent v1 |
| Look | Fluent styling recreated by WPF-UI | Native Fluent controls and motion | Avalonia's Fluent theme; the WinUI gaps are filled in `Themes/Exceptions.axaml` (section 7) |
| Windows 10 support | 10 1809+ and 11; Mica on 11 only | 10 1809+ and 11; Mica on 11 only | 10 and 11, plus Linux and macOS 15+; Mica and Acrylic on Windows only |
| Distribution | Self-contained exe, WiX MSI | Unpackaged + self-contained Windows App SDK, WiX MSI; larger output | Self-contained per OS: MSI, tarball/deb/rpm, app/dmg (`cross-platform.md` section 7) |
| Startup, memory | Fast with ReadyToRun; lower memory | Slightly heavier at startup | Not compared here; measure per app |
| XAML | `{Binding}`, DataTriggers, implicit DataTemplates, `StringFormat` | `{x:Bind}` (compiled, typed), VisualStateManager, no DataTriggers | `{Binding}` compiled by default (`AvaloniaUseCompiledBindingsByDefault`), style classes (`Classes="accent"`), no DataTriggers |
| Localization markup | `{l:Tr Key}` | `{l:Tr Key=Key}` (named arguments only) | `{l:Tr Key=Key}` |
| RTL | `FlowDirection` on the root, complete | `FlowDirection` on the root, complete | `FlowDirection` on each window from the culture, complete |
| Arabic-Indic digits | Yes (`NumberSubstitution`) | No built-in way; setting hidden | Not checked; keep the setting hidden until tested |
| Theme / accent | Live switching for both | Theme live; accent applied at startup (restart) | Both live (`{DynamicResource}` Fluent keys, `ThemeService`) |
| Dialogs | Native message box with RTL flags | `ContentDialog` (async, needs `XamlRoot`) | Drawn inside their window (an inline dialog control); no `ContentDialog` |
| Custom drawing at scale | `DrawingVisual`, fine for 3,000+ items | Shapes and Canvas up to ~3,000; beyond that needs Win2D (not allowed) | Not checked |
| Designer | Visual Studio XAML designer | No designer; hot reload only | Not checked |
| Third-party controls | Large ecosystem | Smaller ecosystem | Not checked |

## 4. What changes in code with WinUI 3

Everything in Core, Data, the tests, the docs, scripts and installer is identical. The scaffold puts the differences in `assets/template/winui/`.

- **Project:** `net10.0-windows10.0.19041.0`, `UseWinUI`, `WindowsPackageType=None`, `WindowsAppSDKSelfContained=true`, `RuntimeIdentifier=win-x64`. Assets such as fonts are `Content` and are referenced with `ms-appx:///`.
- **Version:** Windows App SDK 2.x follows semantic versioning, so breaking changes come only with a new major. The template caps it at major 2; a later major is a new ADR, not a bump. Experimental releases stay out.
- **Partial classes:** every class that derives from or implements a WinUI type (`Window`, `UserControl`, `MarkupExtension`, `IValueConverter`) must be `partial`. This is a platform requirement, not a way around the file-size limits.
- **Bindings:** use `{x:Bind ViewModel.Prop, Mode=OneWay}`. Give each page a typed `public XViewModel ViewModel { get; }` property set in its constructor. Use `{Binding}` only where `x:Bind` can't reach.
- **Windows:** `Window` isn't a `FrameworkElement`. Set `DataContext`, `FlowDirection`, `RequestedTheme` and `Language` on the root element (`ApplyCultureDirection` and `ThemeService` already do this).
- **Startup:** there's no `OnExit`. Clean-up runs on `MainWindow.Closed`. A second instance exits silently.
- **Dialogs:** `IDialogService` is async, and the WinUI version uses `ContentDialog` with the window's `XamlRoot`. Never create a `ContentDialog` elsewhere.
- **UI thread:** use `DispatcherQueue`. `DataChangeNotifier` is created on the UI thread at startup.
- **Accent:** `ThemeService.ApplyAccentResources` sets the `SystemAccentColor*` resources at the start of `OnLaunched`, before any window. Never in the `App` constructor: reading `Application.Resources` there throws 0x8000FFFF. Changing the accent requires a restart.
- **Backdrop:** use `MicaBackdrop` when `MicaController.IsSupported()`; otherwise apply the theme's page background brush.
- **Navigation:** the built-in `NavigationView` with a `Frame`. Pages are resolved from DI. The page `Frame` lives inside the `NavigationView`, so to show a page without the menu (a first-run wizard) hide only the pane (`IsPaneVisible`, `IsPaneToggleButtonVisible`); collapsing the whole `NavigationView` leaves a blank window.
- **Lists inside a scrolling page:** a `ListView` nested in the page's `ScrollViewer` made a full UI Automation tree walk (what a screen reader does) hang for 60 s. For a short list there, use an `ItemsControl` in its own `ScrollViewer` with a `MaxHeight`; keep `ListView` for long lists that sit in a `Grid` row and need virtualization.
- **Triggers:** WinUI has no DataTriggers. Use converters (`LoadStateVisible`) or VisualStateManager.

## 5. Control equivalents

| Need | WPF + WPF-UI | WinUI 3 | Avalonia |
|---|---|---|---|
| Window | `ui:FluentWindow` + `ui:TitleBar` | `Window` + `ExtendsContentIntoTitleBar` + `SetTitleBar` | `Window`, content into the title bar with `ElementRole` parts (section 7) |
| Text input | `ui:TextBox` (`PlaceholderText`) | `TextBox` (`Header`, `PlaceholderText`) | `TextBox` (`PlaceholderText`) |
| Number | `ui:NumberBox` | `NumberBox` | `NumericUpDown` |
| Password | `ui:PasswordBox` | `PasswordBox` (`PasswordRevealMode`) | `TextBox` with `PasswordChar` |
| Drop-down | `ComboBox` | `ComboBox` (`Header`) | `ComboBox` |
| Search with suggestions | `ui:AutoSuggestBox` | `AutoSuggestBox` | `AutoCompleteBox` |
| Date | `DatePicker` | `CalendarDatePicker` | `CalendarDatePicker` |
| On/off setting | `ui:ToggleSwitch` | `ToggleSwitch` | `ToggleSwitch` |
| Single choice | `RadioButton` group | `RadioButtons` | `RadioButton` group |
| Primary button | `ui:Button Appearance="Primary"` | `Button Style="{StaticResource AccentButtonStyle}"` | `Button Classes="accent"` |
| Inline message | `ui:InfoBar` | `InfoBar` | None in Fluent v1; a bordered panel styled from `Exceptions.axaml` |
| Progress | `ui:ProgressRing` | `ProgressRing` | `ProgressBar IsIndeterminate="True"` |
| Text styles | `ui:TextBlock FontTypography="Title"` | `Style="{StaticResource TitleTextBlockStyle}"` | Style classes defined once in the app's styles |
| Navigation | `ui:NavigationView` (per the WPF-UI 4.x sample) | `NavigationView` | None built in; a compact rail (`ListBox`) beside a content area |

For WPF-UI, check member names against the installed 4.x version (search its package XAML) before using a property for the first time. Never guess from 3.x examples.

## 6. Tray icon (optional)
7. Avalonia rules

WPF and WinUI have no tray icon built in; Avalonia does (`TrayIcon`, used on all three systems, see `cross-platform.md` section 4). Add one only when the product asks, and record it as an ADR first (agent rule 4).

- **Library (WPF, WinUI):** `H.NotifyIcon` 2.x - `H.NotifyIcon.Wpf` for WPF, `H.NotifyIcon.WinUI` for WinUI. Nothing else in the stack provides a tray icon.
- **Icons:** ship four `.ico` files under `Assets/` - filled and outline, each in a light and a dark variant (`tray-light.ico`, `tray-dark.ico`, `tray-outline-light.ico`, `tray-outline-dark.ico`). The notification area follows the **taskbar** theme, which can differ from the app theme, so pick the file from `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize` value `SystemUsesLightTheme` (not `AppsUseLightTheme`), never from `RequestedTheme`.
- **The taskbar theme can change while the app runs.** `ElementTheme.Default` follows Windows by itself, but the tray icon and any custom frame color do not. Watch for the change and reapply:
  - `WM_SETTINGCHANGE` with `lParam == "ImmersiveColorSet"` - Windows broadcasts it to top-level windows only, so a message-only window never sees it;
  - or a registry notification (`RegNotifyChangeKeyValue`) on the Personalize key, which fires immediately instead of waiting in the window message queue.
  Reapply on the UI thread (`DispatcherQueue` / `Dispatcher`), and only when the resolved icon file actually changed.
- Menu: Show/Hide, Settings, Exit; left-click does the default action. Register the icon in DI as a singleton and dispose it with the host - a collected instance loses the icon silently.

## 7. Avalonia rules

Avalonia projects follow `cross-platform.md` for structure; these are the view-side rules. Core, Data, `Tr`, `PageViewModel`, `AppIcon` and the testing rules are the same as for WPF and WinUI.

- **Tokens:** `<FluentTheme />` is the only theme and the only token source. Views use Fluent keys through `{DynamicResource}`, so theme and accent switch live. App-owned colors live only in `Themes/Exceptions.axaml`, one entry per theme variant, each with a comment naming the Fluent gap it fills (no success color, no card shadow, no caption close red). `App.axaml` has no ad-hoc setters.
- **Token test:** a Desktop test fails when a view uses a key that doesn't resolve in every theme variant, or when a literal color appears outside `Exceptions.axaml`. A color that looks wrong is fixed with a different Fluent key or an exceptions entry, never with a literal in the view.
- **Fluent v1 gaps:** Avalonia's Fluent theme is Fluent v1. There is no `ContentDialog`, `InfoBar`, `NavigationView` or `ProgressRing`, and the control fills differ from WinUI 3. Build the missing pieces once in `Controls/` (an inline dialog drawn inside its window, a settings card), and map control rest, hover and pressed fills to WinUI's values in `Exceptions.axaml` when the product must look like a Windows 11 app.
- **Title bar on Windows:** run content into the title bar (`WindowDecorations="BorderOnly"` with `ExtendClientAreaToDecorationsHint`) and draw your own title row. Mark each part with `WindowDecorationProperties.ElementRole` (`TitleBar`, `MinimizeButton`, `MaximizeButton`, `CloseButton`, `User` for a search box), so Windows treats them as caption parts, including snap layouts on maximize.
- **Title bar on macOS:** keep the system decorations (`WindowDecorations.Full`) and the system traffic lights. Hide the Windows-style caption buttons and the app name and icon, since the menu bar shows the name.
- **Names:** set `Application.Name` in `App.axaml`; macOS shows it in the menu and in Keychain prompts.
- **OS-only options:** hide Mica, Acrylic and other Windows-only settings on Linux and macOS (`cross-platform.md` section 3), don't show them disabled.
- **Bindings:** set `AvaloniaUseCompiledBindingsByDefault` in the project. Windows and pages get their services through constructors and are resolved from DI, never created by the XAML loader, so suppress `AVLN3001` with a comment saying so.
- **Direction:** each window sets `FlowDirection` from the culture in its constructor (`Culture.IsRtl`), the same rule as `ApplyCultureDirection`.
- **Fonts:** the system UI font, or the bundled Noto Sans Arabic as an `avares://` font family for an Arabic UI.
