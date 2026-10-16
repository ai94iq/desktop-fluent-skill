using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(__Product__.Desktop.Tests.TestApp))]

namespace __Product__.Desktop.Tests;

// Avalonia.Headless directly: check Avalonia.Headless.XUnit discovers tests on the installed
// xunit.v3 major before switching to it (cross-platform.md section 9).
public static class TestApp
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseSkia()                    // real text layout, not stub drawing
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

// Runs test bodies on the headless Avalonia UI thread; one app per test assembly.
internal static class Ui
{
    private static HeadlessUnitTestSession Session =>
        HeadlessUnitTestSession.GetOrStartForAssembly(typeof(Ui).Assembly);

    public static Task Run(Action body) =>
        Session.Dispatch(body, TestContext.Current.CancellationToken);
}
