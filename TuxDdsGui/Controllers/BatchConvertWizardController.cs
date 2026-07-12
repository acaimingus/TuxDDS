using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using TuxDdsGui.Views;
using TuxDDSLib.Dds;
using TuxDdsLib.Export;

namespace TuxDdsGui.Controllers;

public class BatchConvertWizardController(BatchConvertWizardWindow batchConvertWizardWindow)
{
    private readonly BatchConvertWizardWindow _batchConvertWizardWindow = batchConvertWizardWindow;

    public async Task<string> PickFolder(string windowTitle)
    {
        // Open a folder picker
        var topLevel = TopLevel.GetTopLevel(_batchConvertWizardWindow);
        var folder = await topLevel!.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = $"TuxDDS - {windowTitle}",
            SuggestedStartLocation = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Desktop),
        });

        // Return the folder name if correctly selected, else throw an exception
        return folder.Count > 0 ? folder[0].Path.LocalPath : throw new FileNotFoundException("No folder was selected.");
    }

    public static async Task BatchConvert(string inputFolder, string outputFolder, bool recursiveSearch,
        bool keepFolderStructure,
        ExportFormats outputFormat, Action<string> statusCallback)
    {
        await Task.Run(() =>
        {
            // Search the input directory for DDS files
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = recursiveSearch,
                MatchCasing = MatchCasing.CaseInsensitive
            };
            var ddsFiles = Directory.GetFiles(inputFolder, "*.dds", options);

            foreach (var ddsFile in ddsFiles)
            {
                // Update the status
                statusCallback($"INFO: Converting {ddsFile}...");

                // Load the DDS image
                var ddsTexture = DdsLoader.LoadDdsTexture(ddsFile, statusCallback);

                // Export
                if (ddsTexture != null)
                {
                    string? outputFilePath = null;

                    // Build the output file path
                    if (keepFolderStructure)
                    {
                        // Determine the relative path
                        var relativePath = Path.GetRelativePath(inputFolder, ddsFile);

                        // Get the relative directories inbetween
                        var relativeDir = Path.GetDirectoryName(relativePath);

                        // Create the target directory on the filesystem
                        var targetDir = string.IsNullOrEmpty(relativeDir)
                            ? outputFolder
                            : Path.Combine(outputFolder, relativeDir);
                        Directory.CreateDirectory(targetDir);

                        // Generate final path
                        var originalFileName = Path.GetFileNameWithoutExtension(ddsFile);
                        var extension = outputFormat.ToString().ToLower();
                        outputFilePath = Path.Combine(targetDir, $"{originalFileName}.{extension}");
                    }
                    else
                    {
                        // Just put it all in the top directory
                        var originalFileName = Path.GetFileNameWithoutExtension(ddsFile);
                        var extension = outputFormat.ToString().ToLower();
                        outputFilePath = Path.Combine(outputFolder, $"{originalFileName}.{extension}");
                    }

                    // Do the actual export
                    switch (outputFormat)
                    {
                        case ExportFormats.PNG:
                            Exporter.ExportToPng(outputFilePath, ddsTexture.PreviewImageData,
                                ddsTexture.Width, ddsTexture.Height, statusCallback);
                            break;
                        case ExportFormats.JPG:
                            Exporter.ExportToJpg(outputFilePath, ddsTexture.PreviewImageData,
                                ddsTexture.Width, ddsTexture.Height, statusCallback);
                            break;
                        default:
                            statusCallback($"ERROR: Selected invalid export format: {outputFormat.ToString()}");
                            return;
                    }
                }
            }

            statusCallback(
                $"INFO: Finished the batch conversion of {ddsFiles.Length} files in {inputFolder} successfully.");
        });
    }
}