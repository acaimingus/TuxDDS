using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using TuxDdsGui.Views;
using TuxDDSLib.Dds;
using TuxDdsLib.Export;

namespace TuxDdsGui.Controllers;

/// <summary>
/// Controller class for the main window view.
/// </summary>
public class MainWindowController
{
    private DdsTexture? _loadedDdsImageTexture;
    private readonly MainWindow _mainWindow;
    private readonly ILogger<MainWindowController> _logger;

    public MainWindowController(MainWindow mainWindow, ILogger<MainWindowController> logger)
    {
        _mainWindow = mainWindow;
        _logger = logger;
    }

    /// <summary>
    /// Method for handling the request to open a DDS image texture.
    /// </summary>
    /// <param name="displayCallback">Display callback to hand over the image to display to the UI</param>
    /// <param name="titleCallback">Title callback to set the window title to the loaded image path</param>
    public async Task OpenDdsImage(Action<WriteableBitmap> displayCallback, Action<string> titleCallback)
    {
        // Create a file chooser dialog
        var topLevel = TopLevel.GetTopLevel(_mainWindow);
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "TuxDDS - Load a DDS image texture",
            SuggestedStartLocation = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Desktop),
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
                _logger.LogInformation("No DDS image selected.");
                return;
            }

            // Load the specified image texture
            _loadedDdsImageTexture = DdsLoader.LoadDdsTexture(selectedFile, _logger);
            
            if (_loadedDdsImageTexture == null) return;

            // Create a WriteableBitmap for the DDS texture
            var writeableBitmap = new WriteableBitmap(
                new PixelSize(_loadedDdsImageTexture.Width, _loadedDdsImageTexture.Height),
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
    public async Task ExportImage(ExportFormats exportFormat)
    {
        // Return if there is no loaded DDS texture
        if (_loadedDdsImageTexture == null)
        {
            _logger.LogInformation("No image to export.");
            return;
        }

        // Create the needed extension format
        var extension = $".{exportFormat.ToString().ToLower()}";
        
        // Create a file saver dialog and get the path to save the file
        var topLevel = TopLevel.GetTopLevel(_mainWindow);
        var file = await topLevel?.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"TuxDDS - Export to {exportFormat.ToString()}",
            SuggestedStartLocation = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Desktop),
            DefaultExtension = extension,
            ShowOverwritePrompt = true,
            FileTypeChoices =
            [
                new FilePickerFileType($"{exportFormat.ToString()} Image")
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
                case ExportFormats.PNG:
                    Exporter.ExportToPng(localFilePath, _loadedDdsImageTexture.PreviewImageData,
                        _loadedDdsImageTexture.Width, _loadedDdsImageTexture.Height, _logger);
                    break;
                case ExportFormats.JPG:
                    Exporter.ExportToJpg(localFilePath, _loadedDdsImageTexture.PreviewImageData,
                        _loadedDdsImageTexture.Width, _loadedDdsImageTexture.Height, _logger);
                    break;
                default:
                    _logger.LogError("Selected invalid export format: {ExportFormat}", exportFormat.ToString());
                    return;
            }
        }
    }
}