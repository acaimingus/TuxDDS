using System;
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

    private bool IsConvertEnabled()
    {
        // Check if the output and input directories are set
        if (string.IsNullOrWhiteSpace(TbInputFolder.Text) || string.IsNullOrWhiteSpace(TbOutputFolder.Text))
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

    private bool IsNamingEnabled()
    {
        // Check if the output and input directories are set
        if (string.IsNullOrWhiteSpace(TbInputFolder.Text) || string.IsNullOrWhiteSpace(TbOutputFolder.Text))
        {
            return false;
        }
        
        return true;
    }
}