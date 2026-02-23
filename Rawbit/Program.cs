using Avalonia;
using Avalonia.ReactiveUI;

namespace Rawbit;

sealed class Program
{
    public static void Main(string[] args)
        => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions
            {
                RenderingMode = [Win32RenderingMode.Vulkan, Win32RenderingMode.Wgl, Win32RenderingMode.Software]
            })
            .With(new X11PlatformOptions()
            {
                RenderingMode = [X11RenderingMode.Vulkan, X11RenderingMode.Glx, X11RenderingMode.Software]
            })
            .UseSkia()
            .LogToTrace()
            .UseReactiveUI();
}