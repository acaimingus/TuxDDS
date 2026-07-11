using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using TuxDdsGui.Views;

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
}