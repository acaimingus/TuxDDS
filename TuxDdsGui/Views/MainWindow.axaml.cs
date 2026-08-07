using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
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
    private readonly ILogger<MainWindowController> _logger;

    private readonly ScaleTransform _imgScaleTransform = new(1.0, 1.0);
    private readonly TranslateTransform _imgTranslateTransform = new(0, 0);
    private double _zoomFactor = 1.0;
    private Vector _panPosition = new(0, 0);
    private Point _lastPointerPosition;
    private bool _isPanning;
    
    /// <summary>
    /// Constructor
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        var transformGroup = new TransformGroup();
        transformGroup.Children.Add(_imgScaleTransform);
        transformGroup.Children.Add(_imgTranslateTransform);
        ImgBorder.RenderTransform = transformGroup;

        // Create the logger
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new TuxLoggerGuiProvider(UpdateApplicationStatus));
        });
        _logger = _loggerFactory.CreateLogger<MainWindowController>();
        
        // Set the controller for this view and give it a logger
        _mainWindowController = new MainWindowController(this, _logger);
        
        // Log the first message
        _logger.LogInformation("Welcome to TuxDDS!");
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
        ResetZoomAndScroll();
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
            ImgBorder.IsVisible = false;
            TbControlHint.IsVisible = false;
            TbZoomFactor.IsVisible = false;
        }
        else
        {
            LblNoImage.IsVisible = false;
            ImgBorder.IsVisible = true;
            TbControlHint.IsVisible = true;
            TbZoomFactor.IsVisible = true;
        }
    }

    private void UpdateZoomText(double zoomValue)
    {
        var truncated = Math.Round(zoomValue, 1, MidpointRounding.ToZero);
        TbZoomFactor.Text = $"Zoom: x{truncated}";
    }

    /// <summary>
    /// Event handler for zooming via mouse wheel.
    /// </summary>
    private void OnPreviewPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!ImgBorder.IsVisible) return;

        var zoomDelta = e.Delta.Y > 0 ? 1.15 : (1.0 / 1.15);
        var oldZoom = _zoomFactor;
        var newZoom = Math.Clamp(_zoomFactor * zoomDelta, 0.1, 50.0);

        if (Math.Abs(newZoom - oldZoom) < 0.001) return;

        var mousePos = e.GetPosition(StkWindowContent);
        var scaleRatio = newZoom / oldZoom;

        _panPosition = new Vector(
            mousePos.X - (mousePos.X - _panPosition.X) * scaleRatio,
            mousePos.Y - (mousePos.Y - _panPosition.Y) * scaleRatio
        );

        UpdateZoomText(newZoom);
        
        _zoomFactor = newZoom;
        _imgScaleTransform.ScaleX = _zoomFactor;
        _imgScaleTransform.ScaleY = _zoomFactor;
        _imgTranslateTransform.X = _panPosition.X;
        _imgTranslateTransform.Y = _panPosition.Y;

        e.Handled = true;
    }

    /// <summary>
    /// Event handler for pressing pointer (start drag-pan or middle-click to reset zoom).
    /// </summary>
    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!ImgBorder.IsVisible) return;

        var properties = e.GetCurrentPoint(SvPreview).Properties;

        if (properties.IsMiddleButtonPressed)
        {
            ResetZoomAndScroll();
            e.Handled = true;
            return;
        }

        if (properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _lastPointerPosition = e.GetPosition(SvPreview);
            e.Pointer.Capture(SvPreview);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Event handler for moving pointer during drag-pan.
    /// </summary>
    private void OnPreviewPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning) return;

        var currentPosition = e.GetPosition(SvPreview);
        var delta = currentPosition - _lastPointerPosition;
        _lastPointerPosition = currentPosition;

        _panPosition = new Vector(
            _panPosition.X + delta.X,
            _panPosition.Y + delta.Y
        );

        _imgTranslateTransform.X = _panPosition.X;
        _imgTranslateTransform.Y = _panPosition.Y;
        e.Handled = true;
    }

    /// <summary>
    /// Event handler for releasing pointer (end drag-pan).
    /// </summary>
    private void OnPreviewPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Resets the zoom level to 100% and pan position to 0,0.
    /// </summary>
    private void ResetZoomAndScroll()
    {
        _zoomFactor = 1.0;
        UpdateZoomText(_zoomFactor);
        _panPosition = new Vector(0, 0);
        _imgScaleTransform.ScaleX = 1.0;
        _imgScaleTransform.ScaleY = 1.0;
        _imgTranslateTransform.X = 0;
        _imgTranslateTransform.Y = 0;
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
            _logger.LogError("{ExceptionMessage}", exception.Message);
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
            _logger.LogError("{ExceptionMessage}", exception.Message);
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
            _logger.LogError("{ExceptionMessage}", exception.Message);
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
            _logger.LogError("{ExceptionMessage}", exception.Message);
        }
    }

    private void OnMiLogClick(object? sender, RoutedEventArgs e)
    {
        var logWindow = new LogWindow();
        logWindow.Show(this);
    }
}