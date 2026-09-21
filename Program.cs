using Avalonia;
using Avalonia.Media;
using System;

namespace PortBan;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new FontManagerOptions
            {
                DefaultFamilyName = OperatingSystem.IsWindows()
                    ? "Yu Gothic UI"
                    : "WenQuanYi Micro Hei",
                FontFallbacks =
                [
                    new FontFallback { FontFamily = new FontFamily("Yu Gothic UI") },
                    new FontFallback { FontFamily = new FontFamily("Meiryo") },
                    new FontFallback { FontFamily = new FontFamily("WenQuanYi Micro Hei") },
                    new FontFallback { FontFamily = new FontFamily("Droid Sans Fallback") },
                    new FontFallback { FontFamily = new FontFamily("Noto Sans CJK JP") },
                ],
            })
            .LogToTrace();
}
