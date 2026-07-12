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

        CbOutputFormats.SelectionChanged += (_, _) => EvaluatePropertyChanges();
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
        RbAppendName.IsEnabled = IsNamingEnabled();
        TbAppendName.IsEnabled = IsAppendRenameEnabled();
        LblAppendNameHint.IsEnabled = IsAppendRenameEnabled();
        RbBatchRename.IsEnabled = IsNamingEnabled();
        TbBatchRename.IsEnabled = IsBatchRenameEnabled();
        LblBatchRenameHint.IsEnabled = IsBatchRenameEnabled();
        CbOutputFormats.IsEnabled = IsNamingEnabled();

        // Set the hints for the naming strategies
        if (IsNamingEnabled())
        {
            SetAppendHint();
            SetBatchRenameHint();
        }
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

    private async void BtnConvertClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Get the values of the checkboxes
            var recursive = ChkRecursive.IsChecked == true;
            var keepFolders = ChkKeepFolders.IsChecked == true;
            
            // Check if the selected output format is a valid enum
            if (CbOutputFormats.SelectedItem is ExportFormats selectedFormat)
            {
                // Check if the paths are not empty
                if (TbInputFolder.Text != null && TbOutputFolder.Text != null)
                {
                    // Get the selected naming strategy
                    if (RbKeepName.IsChecked == true)
                    {
                        // Start the batch conversion
                        _ = BatchConvertWizardController.BatchConvert(TbInputFolder.Text, TbOutputFolder.Text,
                            recursive, keepFolders, selectedFormat, _statusCallback, NamingStrategies.KeepName, null);
                    }
                    if (RbBatchRename.IsChecked == true)
                    {
                        // Start the batch conversion
                        _ = BatchConvertWizardController.BatchConvert(TbInputFolder.Text, TbOutputFolder.Text,
                            recursive, keepFolders, selectedFormat, _statusCallback, NamingStrategies.BatchRename, TbBatchRename.Text);
                    }
                    if (RbAppendName.IsChecked == true)
                    {
                        // Start the batch conversion
                        _ = BatchConvertWizardController.BatchConvert(TbInputFolder.Text, TbOutputFolder.Text,
                            recursive, keepFolders, selectedFormat, _statusCallback, NamingStrategies.Append, TbAppendName.Text);
                    }
                    
                    // Close the wizard
                    Close();
                }
                else
                {
                    _statusCallback("ERROR: The input/output path was empty.");
                }
            }
            else
            {
                _statusCallback("ERROR: The selected output format was invalid?");
            }
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
    /// Quick check if the naming strategies in general should be enabled
    /// </summary>
    /// <returns>True, if valid paths are given, else False</returns>
    private bool IsNamingEnabled()
    {
        return ValidatePaths();
    }

    /// <summary>
    /// Quick check if the needed fields for the append strategy are set/selected
    /// </summary>
    /// <returns>True, if append renaming should be enabled, else False</returns>
    private bool IsAppendRenameEnabled()
    {
        return ValidatePaths() && RbAppendName.IsChecked == true;
    }

    /// <summary>
    /// Quick check if the needed fields for the batch rename strategy are set/selected
    /// </summary>
    /// <returns>True, if batch renaming should be enabled, else False</returns>
    private bool IsBatchRenameEnabled()
    {
        return ValidatePaths() && RbBatchRename.IsChecked == true;
    }

    /// <summary>
    /// Method for setting the example file path when using the append naming strategy
    /// </summary>
    private void SetAppendHint()
    {
        LblAppendNameHint.Content =
            $"{TbOutputFolder.Text}{{FILENAME}}-{TbAppendName.Text}.{CbOutputFormats.Text!.ToLower()}";
    }

    /// <summary>
    /// Method for setting the example file path when using the batch rename strategy
    /// </summary>
    private void SetBatchRenameHint()
    {
        LblBatchRenameHint.Content = $"{TbOutputFolder.Text}{TbBatchRename.Text}-1.{CbOutputFormats.Text!.ToLower()}";
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