using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Cryptology.UiChecks.TestAppBuilder))]

namespace Cryptology.UiChecks;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<Cryptology.Desktop.App>()
        .UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
