using System;
using System.IO;
using Microsoft.Extensions.Logging;
using TuxDdsLib.Logging;

namespace TuxDdsGui.Logging;

public class TuxLoggerGuiProvider : ILoggerProvider
{
    private readonly Action<string> _logCallback;
    private readonly StreamWriter _streamWriter;
    
    // Path for the log file of the application
    private readonly string _logFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
        "TuxDds", 
        "TuxDds.log");

    public TuxLoggerGuiProvider(Action<string> logCallback)
    {
        _logCallback = logCallback;

        // Check if the directory exists before writing to it
        var logDir = Path.GetDirectoryName(_logFilePath);
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir!);
        }

        // Open the streamWriter once
        _streamWriter = new StreamWriter(_logFilePath, append: true)
        {
            AutoFlush = true
        };
    }

    public ILogger CreateLogger(string categoryName)
    {
        // Pass the same StreamWriter a new logger when one is created
        return new TuxLogger(_logCallback, categoryName, _streamWriter);
    }

    public void Dispose()
    {
        // Dispose of the StreamWriter after use
        _streamWriter.Dispose();
    }
}