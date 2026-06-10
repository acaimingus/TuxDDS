using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace TuxDDS
{
    internal class Gui : Window
    {
        [UI] private Statusbar sbApplicationStatus;
        
        public Gui() : this(new Builder("Gui.glade"))
        {
        }

        private Gui(Builder builder) : base(builder.GetRawOwnedObject("Gui"))
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