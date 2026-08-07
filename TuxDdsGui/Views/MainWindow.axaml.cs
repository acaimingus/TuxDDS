using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using TuxDdsGui.Controllers;
using TuxDdsGui.Logging;
using TuxDdsLib.Export;

namespace TuxDdsGui.Views;

/// <summary>
/// Code-behind for the main window view.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Controller for this view class
    /// </summary>
    private readonly MainWindowController _mainWindowController;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<MainWindowController> logger;
    
    /// <summary>
    /// Constructor
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // Create the logger
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new TuxLoggerGuiProvider(UpdateApplicationStatus));
        });
        logger = _loggerFactory.CreateLogger<MainWindowController>();
        
        // Set the controller for this view and give it a logger
        _mainWindowController = new MainWindowController(this, logger);
        
        // Log the first message
        logger.LogInformation("Welcome to TuxDDS!");
    }

    /// <summary>
    /// Callback method for setting the window title, used for displaying the currently open image in the title bar
    /// </summary>
    /// <param name="title">Title to set for the window</param>
    private void SetWindowTitle(string title)
    {
        Title = $"TuxDDS - {title}";
    }

    /// <summary>
    /// Callback method for setting status messages from the controller.
    /// </summary>
    /// <param name="message">Status message to be displayed</param>
    private void UpdateApplicationStatus(string message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => SbApplicationStatus.Text = message);
    }

    /// <summary>
    /// Callback method for displaying the image from the controller.
    /// </summary>
    /// <param name="writeableBitmap">Bitmap to be displayed</param>
    private void DisplayDdsImage(WriteableBitmap writeableBitmap)
    {
        ImgDdsTexture.Source = writeableBitmap;
        ToggleNoImage(false);
    }
    
    /// <summary>
    /// Helper method for managing if an image is present or not
    /// </summary>
    /// <param name="toggle">Is there currently no image?</param>
    private void ToggleNoImage(bool toggle)
    {
        if (toggle)
        {
            LblNoImage.IsVisible = true;
            ImgDdsTexture.IsVisible = false;
        }
        else
        {
            LblNoImage.IsVisible = false;
            ImgDdsTexture.IsVisible = true;
        }
    }

    /// <summary>
    /// Event handler for when the "Open" menu item is selected
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="eventArgs">Event arguments</param>
    private async void OnMiOpenDdsImageClick(object? sender, RoutedEventArgs eventArgs)
    {
        try
        {
            await _mainWindowController.OpenDdsImage(DisplayDdsImage, SetWindowTitle);
        }
        catch (Exception exception)
        {
            logger.LogError("{ExceptionMessage}", exception.Message);
        }
    }

    /// <summary>
    /// Event handler for clicking the Export to PNG option in the top menu
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="eventArgs">Event arguments</param>
    private async void OnMiExportToPngClick(object? sender, RoutedEventArgs eventArgs)
    {
        try
        {
            await _mainWindowController.ExportImage(ExportFormats.PNG);
        }
        catch (Exception exception)
        {
            logger.LogError("{ExceptionMessage}", exception.Message);
        }
    }
    
    /// <summary>
    /// Event handler for clicking the Export to JPG option in the top menu
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="eventArgs">Event arguments</param>
    private async void OnMiExportToJpgClick(object? sender, RoutedEventArgs eventArgs)
    {
        try
        {
            await _mainWindowController.ExportImage(ExportFormats.JPG);
        }
        catch (Exception exception)
        {
            logger.LogError("{ExceptionMessage}", exception.Message);
        }
    }
    
    /// <summary>
    /// Event handler for clicking the batch convert wizard option in the top menu
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="eventArgs">Event arguments</param>
    private async void OnMiBatchConvertClick(object? sender, RoutedEventArgs eventArgs)
    {
        try
        {
            var wizardLogger = _loggerFactory.CreateLogger<BatchConvertWizardWindow>();
            var batchConvertWizardWindow = new BatchConvertWizardWindow(wizardLogger);
            await batchConvertWizardWindow.ShowDialog(this);
        }
        catch (Exception exception)
        {
            logger.LogError("{ExceptionMessage}", exception.Message);
        }
    }

    private void OnMiLogClick(object? sender, RoutedEventArgs e)
    {
        var logWindow = new LogWindow();
        logWindow.Show(this);
    }
}