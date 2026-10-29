using Microsoft.Extensions.Logging;
using StbImageWriteSharp;
using TuxDDSLib.Dds;

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

    /// <summary>
    /// Method for batch exporting all DDS images inside a directory according to the given <see cref="BatchExportOptions"/>.
    /// </summary>
    /// <param name="options">Configuration options for the batch export</param>
    /// <param name="logger">Logger for relaying status and error messages</param>
    public static void BatchExport(BatchExportOptions options, ILogger? logger = null)
    {
        // Search the input directory for DDS files
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = options.RecursiveSearch,
            MatchCasing = MatchCasing.CaseInsensitive
        };
        var ddsFiles = Directory.GetFiles(options.InputFolder, "*.dds", enumerationOptions);

        // Counter for the batch renaming strategy
        var batchRenameIndex = 1;

        foreach (var ddsFile in ddsFiles)
        {
            // Update the status
            logger?.LogInformation("Converting {DdsFile}...", ddsFile);

            // Load the DDS image
            var ddsTexture = DdsLoader.LoadDdsTexture(ddsFile, logger);

            // Export
            if (ddsTexture != null)
            {
                string outputFilePath;

                // Rename the files based on the naming strategy
                var baseFileName = Path.GetFileNameWithoutExtension(ddsFile);
                switch (options.NamingStrategy)
                {
                    case NamingStrategies.BatchRename:
                        baseFileName = $"{options.NamingString}-{batchRenameIndex}";
                        batchRenameIndex++;
                        break;
                    case NamingStrategies.Append:
                        baseFileName = $"{baseFileName}-{options.NamingString}";
                        break;
                    case NamingStrategies.KeepName:
                    default:
                        // Keep the default baseFileName
                        break;
                }

                var extension = options.OutputFormat.ToString().ToLower();

                // Build the output file path
                if (options.KeepFolderStructure)
                {
                    // Determine the relative path
                    var relativePath = Path.GetRelativePath(options.InputFolder, ddsFile);

                    // Get the relative directories in between
                    var relativeDir = Path.GetDirectoryName(relativePath);

                    // Create the target directory on the filesystem
                    var targetDir = string.IsNullOrEmpty(relativeDir)
                        ? options.OutputFolder
                        : Path.Combine(options.OutputFolder, relativeDir);
                    Directory.CreateDirectory(targetDir);

                    // Generate final path
                    outputFilePath = Path.Combine(targetDir, $"{baseFileName}.{extension}");
                }
                else
                {
                    // Just put it all in the top directory
                    outputFilePath = Path.Combine(options.OutputFolder, $"{baseFileName}.{extension}");
                }

                // Do the actual export
                switch (options.OutputFormat)
                {
                    case ExportFormats.PNG:
                        ExportToPng(outputFilePath, ddsTexture.PreviewImageData,
                            ddsTexture.Width, ddsTexture.Height, logger);
                        break;
                    case ExportFormats.JPG:
                        ExportToJpg(outputFilePath, ddsTexture.PreviewImageData,
                            ddsTexture.Width, ddsTexture.Height, logger);
                        break;
                    default:
                        logger?.LogError("Selected invalid export format: {OutputFormat}", options.OutputFormat);
                        return;
                }
            }
        }

        logger?.LogInformation(
            "Finished the batch conversion of {FileCount} files in {InputFolder} successfully. Check the log for details.",
            ddsFiles.Length, options.InputFolder);
    }
}