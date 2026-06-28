using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using TuxDdsLib;
using TuxDDSLib.Dds;
using TuxDdsLib.Export;

namespace TuxDdsGui.Views;

public class MainWindowController(MainWindow mainWindow)
{
    private DdsTexture? _loadedDdsImageTexture;

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

            // Safety check if there is a selected file
            if (selectedFile == null)
            {
                statusCallback("ERROR: No DDS image was selected.");
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

    public async Task ExportImage(ExportFormats exportFormat, Action<string> statusCallback)
    {
        // Return if there is no loaded DDS texture
        if (_loadedDdsImageTexture == null)
        {
            statusCallback("No image to export.");
            return;
        }

        // Create a file saver dialog
        var topLevel = TopLevel.GetTopLevel(mainWindow);

        var file = await topLevel?.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Export to {exportFormat.ToString()}",
            DefaultExtension = $".{exportFormat.ToString().ToLower()}",
            ShowOverwritePrompt = true,
        })!;

        var localFilePath = file?.TryGetLocalPath();

        if (localFilePath != null)
        {
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
                    statusCallback($"Selected invalid export format: {exportFormat.ToString()}");
                    return;
            }
        }
    }
}