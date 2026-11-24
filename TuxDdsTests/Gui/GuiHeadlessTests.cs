using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using TuxDdsGui.Controllers;
using TuxDdsGui.Views;
using TuxDdsLib.Export;
using TuxDdsLib.Logging;
using TuxDdsTests.Helpers;

namespace TuxDdsTests.Gui;

[Collection("GlobalState")]
public class GuiHeadlessTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly string _originalLogPath;

    public GuiHeadlessTests()
    {
        _originalLogPath = TuxLogger.LogFilePath;
        TuxLogger.LogFilePath = _temp.GetFilePath("gui_logs", "TuxDds.log");
    }

    public void Dispose()
    {
        TuxLogger.LogFilePath = _originalLogPath;
        _temp.Dispose();
    }

    private static WriteableBitmap CreateTestBitmap(int width = 16, int height = 16)
    {
        var bmp = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);

        var pixels = DdsTestDataGenerator.CreateDefaultRgbaPixels(width, height);
        using var fb = bmp.Lock();
        Marshal.Copy(pixels, 0, fb.Address, pixels.Length);
        return bmp;
    }

    [AvaloniaFact]
    public void MainWindow_InitialState_ShowsNoImagePlaceholderAndHidesPreview()
    {
        var window = new MainWindow();
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            Assert.Equal("TuxDDS", window.Title);
            Assert.True(window.LblNoImage.IsVisible);
            Assert.False(window.ImgBorder.IsVisible);
            Assert.False(window.TbControlHint.IsVisible);
            Assert.False(window.TbZoomFactor.IsVisible);
            Assert.Contains("Welcome to TuxDDS!", window.SbApplicationStatus.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MainWindow_DisplayDdsImageAndSetTitle_UpdatesUiAndSupportsZoomPanReset()
    {
        var window = new MainWindow();
        window.Show();

        try
        {
            using var bitmap = CreateTestBitmap(32, 32);
            window.DisplayDdsImage(bitmap);
            window.SetWindowTitle("preview.dds");
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("TuxDDS - preview.dds", window.Title);
            Assert.False(window.LblNoImage.IsVisible);
            Assert.True(window.ImgBorder.IsVisible);
            Assert.True(window.TbControlHint.IsVisible);
            Assert.True(window.TbZoomFactor.IsVisible);

            var transformGroup = Assert.IsType<TransformGroup>(window.ImgBorder.RenderTransform);
            var scale = Assert.IsType<ScaleTransform>(transformGroup.Children[0]);
            var translate = Assert.IsType<TranslateTransform>(transformGroup.Children[1]);

            Assert.Equal(1.0, scale.ScaleX, 3);
            Assert.Equal(0.0, translate.X, 3);

            // Zoom in with mouse wheel at center of window
            window.MouseWheel(new Point(320, 240), new Vector(0, 1));
            Dispatcher.UIThread.RunJobs();
            Assert.True(scale.ScaleX > 1.0);

            // Drag-pan with left mouse button
            window.MouseDown(new Point(200, 200), MouseButton.Left);
            window.MouseMove(new Point(250, 230), RawInputModifiers.LeftMouseButton);
            window.MouseUp(new Point(250, 230), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            var pannedX = translate.X;
            var pannedY = translate.Y;
            Assert.NotEqual(0.0, pannedX);
            Assert.NotEqual(0.0, pannedY);

            // Middle-click resets zoom and pan
            window.MouseDown(new Point(250, 230), MouseButton.Middle);
            window.MouseUp(new Point(250, 230), MouseButton.Middle);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1.0, scale.ScaleX, 3);
            Assert.Equal(1.0, scale.ScaleY, 3);
            Assert.Equal(0.0, translate.X, 3);
            Assert.Equal(0.0, translate.Y, 3);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LogWindow_DisplaysExistingLogFileContent_AndHandlesMissingFile()
    {
        // Missing log file should not throw and leaves text empty/null
        if (File.Exists(TuxLogger.LogFilePath))
        {
            File.Delete(TuxLogger.LogFilePath);
        }

        var emptyLogWindow = new LogWindow();
        emptyLogWindow.Show();
        Assert.True(string.IsNullOrEmpty(emptyLogWindow.TbLogContent.Text));
        emptyLogWindow.Close();

        // Existing log file is loaded into TbLogContent
        Directory.CreateDirectory(Path.GetDirectoryName(TuxLogger.LogFilePath)!);
        File.WriteAllText(TuxLogger.LogFilePath, "=== TuxDds.log ===\n[INFORMATION] Sample log entry");

        var populatedWindow = new LogWindow();
        populatedWindow.Show();
        Assert.Contains("Sample log entry", populatedWindow.TbLogContent.Text);
        populatedWindow.Close();
    }

    [AvaloniaFact]
    public void BatchConvertWizardWindow_ValidatesFoldersAndNamingStrategies()
    {
        var window = new BatchConvertWizardWindow(NullLogger.Instance);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            // Initial state: controls disabled until valid folders are entered
            Assert.False(window.BtnConvert.IsEnabled);
            Assert.True(window.RbKeepName.IsChecked);
            Assert.False(window.RbKeepName.IsEnabled);
            Assert.False(window.CbOutputFormats.IsEnabled);

            // Non-existent folders keep Convert disabled
            window.TbInputFolder.Text = _temp.GetFilePath("does_not_exist_in");
            window.TbOutputFolder.Text = _temp.GetFilePath("does_not_exist_out");
            Dispatcher.UIThread.RunJobs();
            Assert.False(window.BtnConvert.IsEnabled);

            // Existing folders enable naming strategies and Convert button
            var inDir = _temp.GetFilePath("wizard_in");
            var outDir = _temp.GetFilePath("wizard_out") + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(inDir);
            Directory.CreateDirectory(outDir);

            window.TbInputFolder.Text = inDir;
            window.TbOutputFolder.Text = outDir;
            Dispatcher.UIThread.RunJobs();

            Assert.True(window.BtnConvert.IsEnabled);
            Assert.True(window.RbKeepName.IsEnabled);
            Assert.True(window.RbAppendName.IsEnabled);
            Assert.True(window.RbBatchRename.IsEnabled);
            Assert.True(window.CbOutputFormats.IsEnabled);

            // Selecting Append requires non-empty TbAppendName
            window.RbAppendName.IsChecked = true;
            window.RbKeepName.IsChecked = false;
            Dispatcher.UIThread.RunJobs();

            Assert.True(window.TbAppendName.IsEnabled);
            Assert.False(window.BtnConvert.IsEnabled);

            window.TbAppendName.Text = "v2";
            Dispatcher.UIThread.RunJobs();
            Assert.True(window.BtnConvert.IsEnabled);
            Assert.Contains("{FILENAME}-v2.png", window.LblAppendNameHint.Content?.ToString());

            // Selecting BatchRename requires non-empty TbBatchRename
            window.RbBatchRename.IsChecked = true;
            window.RbAppendName.IsChecked = false;
            Dispatcher.UIThread.RunJobs();

            Assert.True(window.TbBatchRename.IsEnabled);
            Assert.False(window.BtnConvert.IsEnabled);

            window.TbBatchRename.Text = "frame";
            Dispatcher.UIThread.RunJobs();
            Assert.True(window.BtnConvert.IsEnabled);
            Assert.Contains("frame-1.png", window.LblBatchRenameHint.Content?.ToString());
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public async Task BatchConvertWizardController_BatchConvert_ExportsFilesInBackground()
    {
        var inDir = _temp.GetFilePath("ctrl_in");
        var outDir = _temp.GetFilePath("ctrl_out");
        DdsTestDataGenerator.CreateRgba8Dds(Path.Combine(inDir, "tex.dds"));

        await BatchConvertWizardController.BatchConvert(new BatchExportOptions
        {
            InputFolder = inDir,
            OutputFolder = outDir,
            OutputFormat = ExportFormats.PNG
        }, NullLogger.Instance);

        Assert.True(File.Exists(Path.Combine(outDir, "tex.png")));
    }
}
