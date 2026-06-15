using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

namespace TuxDDS;

public partial class MainWindow : Window
{
    /// <summary>
    /// Controller for this view class
    /// </summary>
    private readonly MainWindowController _mainWindowController;
    
    /// <summary>
    /// Constructor
    /// </summary>
    public MainWindow()
    {
        _mainWindowController = new MainWindowController(this);
        
        InitializeComponent();
    }

    /// <summary>
    /// Callback method for setting status messages from the controller.
    /// </summary>
    /// <param name="message">Status message to be displayed</param>
    private void UpdateApplicationStatus(string message)
    {
        SbApplicationStatus.Text = message;
    }

    /// <summary>
    /// Callback method for displaying the image from the controller.
    /// </summary>
    /// <param name="writeableBitmap">Bitmap to be displayed</param>
    private void DisplayDdsImage(WriteableBitmap writeableBitmap)
    {
        ImgDdsTexture.Source =  writeableBitmap;
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
            await _mainWindowController.OpenDdsImage(UpdateApplicationStatus, DisplayDdsImage);
        }
        catch (Exception exception)
        {
            UpdateApplicationStatus($"FATAL EXCEPTION: {exception.Message}");
        }
    }
}