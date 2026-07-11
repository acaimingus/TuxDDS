using System;
using Avalonia.Controls;
using TuxDdsGui.Controllers;
using TuxDdsLib.Export;

namespace TuxDdsGui.Views;

public partial class BatchConvertWizardWindow : Window
{
    private readonly BatchConvertWizardController _batchConvertWizardController;
    
    public BatchConvertWizardWindow()
    {
        InitializeComponent();
        
        _batchConvertWizardController = new BatchConvertWizardController();

        CbOutputFormats.ItemsSource = Enum.GetValues(typeof(ExportFormats));
        CbOutputFormats.SelectedIndex = 0;
    }
}