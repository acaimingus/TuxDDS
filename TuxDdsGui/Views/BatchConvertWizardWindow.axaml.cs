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
}