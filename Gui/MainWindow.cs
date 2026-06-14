using System;
using Gdk;
using Gtk;
using TuxDDS.Dds;
using UI = Gtk.Builder.ObjectAttribute;
using Window = Gtk.Window;

namespace TuxDDS.Gui
{
    public class MainWindow : Window
    {
        [UI] private Statusbar sbApplicationStatus;
        [UI] private Stack stkWindowContent;
        [UI] private Image imgDdsTexture;
        
        private readonly uint _statusBarContextId;
        
        private MainWindowController _controller;

        public MainWindow() : this(new Builder("MainWindow.glade")) {}

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            // Create a controller for this GUI
            _controller =  new MainWindowController(this);
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
            _controller.OpenDdsImage(UpdateApplicationStatus, DisplayDdsImage);
        }
        
        private void DisplayDdsImage(Pixbuf pixbuf)
        {
            // Switch the Stack to display the imageView
            stkWindowContent.VisibleChild = imgDdsTexture;
            // Create the image in the image view
            imgDdsTexture.Pixbuf = pixbuf;
        }
    }
}