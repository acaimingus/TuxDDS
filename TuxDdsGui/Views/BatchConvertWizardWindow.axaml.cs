using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TuxDdsGui.Controllers;
using TuxDdsLib.Export;

namespace TuxDdsGui.Views;

public partial class BatchConvertWizardWindow : Window
{
    private readonly BatchConvertWizardController _batchConvertWizardController;
    private readonly Action<string> _statusCallback;

    public BatchConvertWizardWindow(Action<string> statusCallback)
    {
        InitializeComponent();

        _statusCallback = statusCallback;
        _batchConvertWizardController = new BatchConvertWizardController(this);

        CbOutputFormats.ItemsSource = Enum.GetValues<ExportFormats>();
        CbOutputFormats.SelectedIndex = 0;
        
        TbInputFolder.TextChanged += (_, _) => EvaluatePropertyChanges();
        TbOutputFolder.TextChanged += (_, _) => EvaluatePropertyChanges();
        TbAppendName.TextChanged += (_, _) => EvaluatePropertyChanges();
        TbBatchRename.TextChanged += (_, _) => EvaluatePropertyChanges();
        RbKeepName.IsCheckedChanged += (_, _) => EvaluatePropertyChanges();
        RbAppendName.IsCheckedChanged += (_, _) => EvaluatePropertyChanges();
        RbBatchRename.IsCheckedChanged += (_, _) => EvaluatePropertyChanges();
    }

    private void EvaluatePropertyChanges()
    {
        // Check if the convert button is enabled
        BtnConvert.IsEnabled = IsConvertEnabled();
        // Check if naming strategy can be selected
        RbKeepName.IsEnabled = IsNamingEnabled();
        RbAppendName.IsEnabled =  IsNamingEnabled();
        TbAppendName.IsEnabled = IsNamingEnabled();
        LblAppendNameHint.IsEnabled = IsNamingEnabled();
        RbBatchRename.IsEnabled = IsNamingEnabled();
        TbBatchRename.IsEnabled = IsNamingEnabled();
        LblBatchRenameHint.IsEnabled = IsNamingEnabled();
    }

    private async void BtnSelectInputFolderClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            TbInputFolder.Text =
                await _batchConvertWizardController.PickFolder("Select Folder with DDS files to convert");
        }
        catch (Exception exception)
        {
            _statusCallback($"ERROR: {exception.Message}");
        }
    }

    private async void BtnSelectOutputFolderClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            TbOutputFolder.Text = await _batchConvertWizardController.PickFolder("Select Output Folder");
        }
        catch (Exception exception)
        {
            _statusCallback($"ERROR: {exception.Message}");
        }
    }

    /// <summary>
    /// Quick check whether to enable the convert button; needs input, output and name set
    /// </summary>
    /// <returns>True, if the button should be enabled, else False</returns>
    private bool IsConvertEnabled()
    {
        if (!ValidatePaths())
        {
            return false;
        }

        // If the radio button for appending to the file name is checked, make sure there is an input in the box
        if (RbAppendName.IsChecked == true)
        {
            if (string.IsNullOrWhiteSpace(TbAppendName.Text))
            {
                return false;
            }
        }

        // If the radio button for batch renaming is enabled, make sure there is an input in the box
        if (RbBatchRename.IsChecked == true)
        {
            if (string.IsNullOrWhiteSpace(TbBatchRename.Text))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Quick check if the naming strategy options should be enabled
    /// </summary>
    /// <returns>True, if a valid path has been selected, else False</returns>
    private bool IsNamingEnabled()
    {
        return ValidatePaths();
    }

    /// <summary>
    /// Helper method for quick verification, if the user input valid paths into the input and output fields
    /// </summary>
    /// <returns>True, if not empty and a valid path, else False</returns>
    private bool ValidatePaths()
    {
        // Check if the output and input directories are not whitespace or empty
        if (string.IsNullOrWhiteSpace(TbInputFolder.Text) || string.IsNullOrWhiteSpace(TbOutputFolder.Text))
        {
            return false;
        }

        // Check if the output and input directories are existing paths
        if (!Directory.Exists(TbInputFolder.Text) || !Directory.Exists(TbOutputFolder.Text))
        {
            return false;
        }

        // Looks fine
        return true;
    }
}