using System;
using Gtk;
using TuxDDS.Dds;

namespace TuxDDS.Gui;

public class MainWindowController(MainWindow mainWindow)
{
    private MainWindow _mainWindow = mainWindow;
    private DdsTexture _loadedDdsImageTexture = null;

    public void OpenDdsImage(Action<string> statusCallback)
    {
        // Create a file chooser dialog
        using var fileChooserDialog = new FileChooserDialog(
            "Select a DDS Image",
            _mainWindow,
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