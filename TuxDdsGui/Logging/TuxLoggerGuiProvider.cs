using System;
using System.IO;
using Microsoft.Extensions.Logging;
using TuxDdsLib.Logging;

namespace TuxDdsGui.Logging;

public class TuxLoggerGuiProvider : ILoggerProvider
{
    private readonly Action<string> _logCallback;
    private readonly StreamWriter _streamWriter;

    public TuxLoggerGuiProvider(Action<string> logCallback)
    {
        _logCallback = logCallback;

        // Check if the directory exists before writing to it
        var logDir = Path.GetDirectoryName(TuxLogger.LogFilePath);
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir!);
        }

        // Open the streamWriter once and overwrite existing log on startup
        _streamWriter = new StreamWriter(TuxLogger.LogFilePath, append: false)
        {
            AutoFlush = true
        };
        
        // Write the header for the log file
        _streamWriter.WriteLine($"=== TuxDds.log ({DateTime.Now}) ===");
        _streamWriter.WriteLine("");
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