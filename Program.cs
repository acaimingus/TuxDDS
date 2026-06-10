using System;
using Gtk;

namespace TuxDDS
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
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