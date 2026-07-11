using Avalonia.Controls;
using TuxDdsGui.Controllers;

namespace TuxDdsGui.Views;

public partial class BatchConvertWizardWindow : Window
{
    private readonly BatchConvertWizardController _batchConvertWizardController;
    
    public BatchConvertWizardWindow()
    {
        InitializeComponent();
        
        _batchConvertWizardController = new BatchConvertWizardController();
    }
}