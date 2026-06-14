using System;
using Gdk;
using Gtk;
using TuxDDS.Dds;

namespace TuxDDS.Gui;

public class MainWindowController(MainWindow mainWindow)
{
    private MainWindow _mainWindow = mainWindow;
    private DdsTexture _loadedDdsImageTexture = null;

    public void OpenDdsImage(Action<string> statusCallback, Action<Pixbuf> displayCallback)
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
            
            // Create a PixBuf for the DDS texture
            var hasAlpha = _loadedDdsImageTexture.BytesPerPixel > 3;
            var rowStride = _loadedDdsImageTexture.Width *  _loadedDdsImageTexture.BytesPerPixel;
            // TODO: DO NOT ASSUME THE BITS PER SAMPLE ARE 8!!!
            var pixbuf = new Pixbuf(
                _loadedDdsImageTexture.ImageData,
                Colorspace.Rgb,
                hasAlpha, 
                8,
                _loadedDdsImageTexture.Width,
                _loadedDdsImageTexture.Height,
                rowStride);
            
            // Use the display callback to display the image
            displayCallback(pixbuf);
        }
    }
}