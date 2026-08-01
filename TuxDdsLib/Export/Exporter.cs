using Microsoft.Extensions.Logging;
using StbImageWriteSharp;

namespace TuxDdsLib.Export;

/// <summary>
/// Class for handling the export of the loaded DDS image to various export formats.
/// </summary>
public static class Exporter
{
    /// <summary>
    /// Method for exporting a PNG image from the DDS image data.
    /// </summary>
    /// <param name="outputFilePath">File path under which to save the PNG image</param>
    /// <param name="imageData">The given image data, always the preview data from the UI to make sure the pixel formats
    /// are correct (R8G8B8A8)</param>
    /// <param name="imageWidth">Width of the given image</param>
    /// <param name="imageHeight">Height of the given image</param>
    /// <param name="logger">Logger for relaying error messages</param>
    public static void ExportToPng(string outputFilePath, byte[] imageData, int imageWidth, int imageHeight,
        ILogger? logger = null)
    {
        try
        {
            // Open an ImageWriter and write the PNG
            using var stream = File.Open(outputFilePath, FileMode.Create);
            var writer = new ImageWriter();

            writer.WritePng(imageData, imageWidth, imageHeight, ColorComponents.RedGreenBlueAlpha, stream);
            logger?.LogInformation("Export to {OutputFilePath} as .PNG was successful!", outputFilePath);
        }
        catch (Exception exception)
        {
            // Update the status message and return
            logger?.LogError("Exporting the image failed: {ExceptionMessage}", exception.Message);
        }
    }

    /// <summary>
    /// Method for exporting a JPG image from the DDS image data, always exports the image at 100% quality.
    /// </summary>
    /// <param name="outputFilePath">File path under which to save the PNG image</param>
    /// <param name="imageData">The given image data, always the preview data from the UI to make sure the pixel formats
    /// are correct (R8G8B8A8)</param>
    /// <param name="imageWidth">Width of the given image</param>
    /// <param name="imageHeight">Height of the given image</param>
    /// <param name="logger">Logger for relaying error messages</param>
    public static void ExportToJpg(string outputFilePath, byte[] imageData, int imageWidth, int imageHeight,
        ILogger? logger = null)
    {
        // This method could be reduced by the DRY-principle, but I am not going to so there won't be any dependencies
        // introduced between exporting PNG and JPG images
        
        try
        {
            // Open an ImageWriter and write the JPG
            using var stream = File.Open(outputFilePath, FileMode.Create);
            var writer = new ImageWriter();

            // Use 100% quality always
            writer.WriteJpg(imageData, imageWidth, imageHeight, ColorComponents.RedGreenBlueAlpha, stream, 100);
            logger?.LogInformation("Export to {OutputFilePath} as .JPG was successful!", outputFilePath);
        }
        catch (Exception exception)
        {
            // Update the status message and return
            logger?.LogError("Exporting the image failed: {ExceptionMessage}", exception.Message);
        }
    }
}