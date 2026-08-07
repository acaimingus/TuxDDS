using System.IO;
using Avalonia.Controls;
using TuxDdsLib.Logging;

namespace TuxDdsGui.Views;

public partial class LogWindow : Window
{
    public LogWindow()
    {
        InitializeComponent();
        
        // Load the log contents from the log file
        if (File.Exists(TuxLogger.LogFilePath))
        {
            TbLogContent.Text = File.ReadAllText(TuxLogger.LogFilePath);
        }
    }
}