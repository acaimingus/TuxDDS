using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace TuxDDS;

public partial class App : Application
{
    public override void Initialize()
    {
        // Load the wrapper library
        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), (libraryName, _, _) =>
        {
            if (libraryName == "DirectXTexWrapper")
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var libraryPath = Path.Combine(baseDirectory, "lib", "libDirectXTexWrapper.so");

                if (File.Exists(libraryPath))
                {
                    return NativeLibrary.Load(libraryPath);
                }
            }
            return IntPtr.Zero;
        });
        
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}