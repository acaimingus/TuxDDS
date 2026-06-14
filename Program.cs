using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Gtk;

namespace TuxDDS
{
    internal static class Program
    {
        [STAThread]
        public static void Main()
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
            
            Application.Init();

            var app = new Application("io.github.acaimingus.TuxDDS", GLib.ApplicationFlags.None);
            app.Register(GLib.Cancellable.Current);

            var win = new Gui();
            app.AddWindow(win);

            win.Show();
            Application.Run();
        }
    }
}