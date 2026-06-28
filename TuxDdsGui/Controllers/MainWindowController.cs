using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using TuxDdsGui.Views;
using TuxDDSLib.Dds;
using TuxDdsLib.Export;

namespace TuxDdsGui.Controllers;

/// <summary>
/// Controller class for the main window view.
/// </summary>
/// <param name="mainWindow">Reference to the main window class</param>
public class MainWindowController(MainWindow mainWindow)
{
    private DdsTexture? _loadedDdsImageTexture;

    /// <summary>
    /// Method for handling the request to open a DDS image texture.
    /// </summary>
    /// <param name="statusCallback">Status callback method to return messages to the UI</param>
    /// <param name="displayCallback">Display callback to hand over the image to display to the UI</param>
    /// <param name="titleCallback">Title callback to set the window title to the loaded image path</param>
    public async Task OpenDdsImage(Action<string> statusCallback, Action<WriteableBitmap> displayCallback,
        Action<string> titleCallback)
    {
        // Create a file chooser dialog
        var topLevel = TopLevel.GetTopLevel(mainWindow);
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load a DDS image texture",
            AllowMultiple = false,
            FileTypeFilter =
            [
                // Filter for DDS images with a fallback to all files
                new FilePickerFileType("DDS Image")
                {
                    Patterns = ["*.dds", "*.DDS"]
                },
                FilePickerFileTypes.All
            ]
        });
        if (files.Count >= 1)
        {
            // Get the selected file
            var selectedFile = files[0].TryGetLocalPath();

            // Safety check if there is a selected DDS file
            if (selectedFile == null || !selectedFile.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
            {
                statusCallback("INFO: No DDS image was selected.");
                return;
            }

            // Load the specified image texture
            _loadedDdsImageTexture = DdsLoader.LoadDdsTexture(selectedFile, statusCallback);

            // Create a WriteableBitmap for the DDS texture
            var writeableBitmap = new WriteableBitmap(
                new PixelSize(_loadedDdsImageTexture!.Width, _loadedDdsImageTexture!.Height),
                new Vector(96, 96),
                PixelFormat.Rgba8888,
                AlphaFormat.Unpremul);

            using (var lockedFrameBuffer = writeableBitmap.Lock())
            {
                Marshal.Copy(_loadedDdsImageTexture.PreviewImageData, 0, lockedFrameBuffer.Address,
                    _loadedDdsImageTexture.PreviewImageData.Length);
            }

            // Use the display callback to display the image
            displayCallback(writeableBitmap);

            // Use the title callback to change the window title
            titleCallback(_loadedDdsImageTexture.FileName);
        }
    }

    
    /// <summary>
    /// Method for handling an export request from the UI.
    /// </summary>
    /// <param name="exportFormat">The requested format for the export</param>
    /// <param name="statusCallback">Status callback method to return messages to the UI</param>
    public async Task ExportImage(ExportFormats exportFormat, Action<string> statusCallback)
    {
        // Return if there is no loaded DDS texture
        if (_loadedDdsImageTexture == null)
        {
            statusCallback("INFO: No image to export.");
            return;
        }

        // Create the needed extension format
        var extension = $".{exportFormat.ToString().ToLower()}";
        
        // Create a file saver dialog and get the path to save the file
        var topLevel = TopLevel.GetTopLevel(mainWindow);
        var file = await topLevel?.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Export to {exportFormat.ToString()}",
            DefaultExtension = extension,
            ShowOverwritePrompt = true,
            FileTypeChoices =
            [
                new FilePickerFileType($"{exportFormat.ToString().ToUpper()} Image")
                {
                    Patterns = [$"*{extension}"]
                }
            ]
        })!;

        var localFilePath = file?.TryGetLocalPath();

        if (localFilePath != null)
        {
            if (!localFilePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                localFilePath += extension;
            }

            switch (exportFormat)
            {
                case ExportFormats.Png:
                    Exporter.ExportToPng(localFilePath, _loadedDdsImageTexture.PreviewImageData,
                        _loadedDdsImageTexture.Width, _loadedDdsImageTexture.Height, statusCallback);
                    break;
                case ExportFormats.Jpg:
                    Exporter.ExportToJpg(localFilePath, _loadedDdsImageTexture.PreviewImageData,
                        _loadedDdsImageTexture.Width, _loadedDdsImageTexture.Height, statusCallback);
                    break;
                default:
                    statusCallback($"INFO: Selected invalid export format: {exportFormat.ToString()}");
                    return;
            }
        }
    }
}