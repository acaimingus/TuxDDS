using System;
using Cairo;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace TuxDDS
{
    internal class MainWindow : Window
    {
        [UI] private Statusbar sbApplicationStatus;
        
        public MainWindow() : this(new Builder("MainWindow.glade"))
        {
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            DeleteEvent += Window_DeleteEvent;
            
            // Set a debug message for the status bar
            var statusBarContextId = sbApplicationStatus.GetContextId("main");
            sbApplicationStatus.Push(statusBarContextId, "No status.");
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }
    }
}