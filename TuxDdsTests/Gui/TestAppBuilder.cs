using Avalonia;
using Avalonia.Headless;
using TuxDdsGui;
using TuxDdsTests.Gui;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace TuxDdsTests.Gui;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = false
        });
}
