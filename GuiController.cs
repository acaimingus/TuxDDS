using System;
using Gtk;

namespace TuxDDS;

public class GuiController(Gui gui)
{
    private Gui _gui = gui;
    private DdsTexture _loadedDdsImageTexture = null;

    public void OpenDdsImage(Action<string> statusCallback)
    {
        // Create a file chooser dialog
        using var fileChooserDialog = new FileChooserDialog(
            "Select a DDS Image",
            _gui,
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
            _loadedDdsImageTexture = DdsLoader.LoadDdsTexture(fileChooserDialog.Filename, statusCallback);
        }
    }
}