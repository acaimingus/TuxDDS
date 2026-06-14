using System;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace TuxDDS
{
    internal class Gui : Window
    {
        [UI] private Statusbar sbApplicationStatus;
        private readonly uint _statusBarContextId;
        
        private DdsTexture _loadedDdsImageTexture = null;
        
        public Gui() : this(new Builder("Gui.glade")) {}

        private Gui(Builder builder) : base(builder.GetRawOwnedObject("Gui"))
        {
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

        private void OpenDdsImage(object sender, EventArgs args)
        {
            // Create a file chooser dialog
            using var fileChooserDialog = new FileChooserDialog(
                "Select a DDS Image",
                this,
                FileChooserAction.Open,
                "Cancel", ResponseType.Cancel,
                "Open", ResponseType.Accept);
            
            // Add a filter for .DDS files
            var fileFilter = new FileFilter();
            fileFilter.Name = "DDS Image";
            fileFilter.AddPattern("*.[Dd][Dd][Ss]");
            fileChooserDialog.AddFilter(fileFilter);
            
            // Show the dialog
            if (fileChooserDialog.Run() == (int)ResponseType.Accept)
            {
                // Load the specified image texture
                _loadedDdsImageTexture = DdsLoader.LoadDdsTexture(fileChooserDialog.Filename, UpdateApplicationStatus);
            }
        }
    }
}