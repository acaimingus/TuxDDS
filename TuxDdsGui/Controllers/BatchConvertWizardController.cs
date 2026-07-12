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
        ExportFormats outputFormat, Action<string> statusCallback, NamingStrategies namingStrategy, string? namingString)
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

            // Counter for the batch renaming strategy
            var batchRenameIndex = 1;
            
            foreach (var ddsFile in ddsFiles)
            {
                // Update the status
                statusCallback($"INFO: Converting {ddsFile}...");

                // Load the DDS image
                var ddsTexture = DdsLoader.LoadDdsTexture(ddsFile, statusCallback);
                
                // Export
                if (ddsTexture != null)
                {
                    string? outputFilePath;
                    
                    // Rename the files based on the naming strategy
                    var baseFileName =  Path.GetFileNameWithoutExtension(ddsFile);
                    switch (namingStrategy)
                    {
                        case NamingStrategies.BatchRename:
                            baseFileName = $"{namingString}-{batchRenameIndex}";
                            batchRenameIndex++;
                            break;
                        case NamingStrategies.Append:
                            baseFileName = $"{baseFileName}-{namingString}";
                            break;
                        case NamingStrategies.KeepName:
                        default:
                            // Keep the default baseFileName
                            break;
                    }
                    
                    var extension = outputFormat.ToString().ToLower();
                    
                    // Build the output file path
                    if (keepFolderStructure)
                    {
                        // Determine the relative path
                        var relativePath = Path.GetRelativePath(inputFolder, ddsFile);

                        // Get the relative directories in between
                        var relativeDir = Path.GetDirectoryName(relativePath);

                        // Create the target directory on the filesystem
                        var targetDir = string.IsNullOrEmpty(relativeDir)
                            ? outputFolder
                            : Path.Combine(outputFolder, relativeDir);
                        Directory.CreateDirectory(targetDir);

                        // Generate final path
                        outputFilePath = Path.Combine(targetDir, $"{baseFileName}.{extension}");
                    }
                    else
                    {
                        // Just put it all in the top directory
                        outputFilePath = Path.Combine(outputFolder, $"{baseFileName}.{extension}");
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