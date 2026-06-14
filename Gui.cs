using System;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace TuxDDS
{
    public class Gui : Window
    {
        [UI] private Statusbar sbApplicationStatus;
        private readonly uint _statusBarContextId;
        
        private GuiController _controller;

        public Gui() : this(new Builder("Gui.glade")) {}

        private Gui(Builder builder) : base(builder.GetRawOwnedObject("Gui"))
        {
            // Create a controller for this GUI
            _controller =  new GuiController(this);
            builder.Autoconnect(this);
            
            DeleteEvent += Window_DeleteEvent;

            // Set a debug message for the status bar
            _statusBarContextId = sbApplicationStatus.GetContextId("main");
            UpdateApplicationStatus("No status.");
        }

        private static void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        private void UpdateApplicationStatus(string message)
        {
            sbApplicationStatus.Push(_statusBarContextId, message);
        }

        private void OnMenuBarOpenOptionClicked(object sender, EventArgs args)
        {
            _controller.OpenDdsImage(UpdateApplicationStatus);
        }
    }
}