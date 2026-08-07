using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace TuxDdsLib.Logging;

public class TuxLogger : ILogger
{
    // Path for the log file of the application
    public static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
        "TuxDds", 
        "TuxDds.log");
    
    private readonly Action<string> _uiLogCallback;
    private readonly string _categoryName;
    private readonly StreamWriter _streamWriter;
    
    public TuxLogger(Action<string> uiLogCallback, string categoryName, StreamWriter streamWriter)
    {
        _uiLogCallback = uiLogCallback;
        _categoryName = categoryName;
        _streamWriter = streamWriter;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // If this log level is not enabled then skip this message
        if (!IsEnabled(logLevel))
        {
            return;
        }
        
        // Write the log message into the log file
        var message = formatter(state, exception);
        var formattedLog = $"[{logLevel.ToString().ToUpper()}] {_categoryName}: {message}";
        
        // Lock the writer while writing to avoid race conditions
        lock (_streamWriter)
        {
            _streamWriter.WriteLine(formattedLog);
        }
        
        // Log the message to the UI as well
        _uiLogCallback($"[{logLevel.ToString().ToUpper()}] {message}");
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        switch (logLevel)
        {
            case LogLevel.Information:
            case LogLevel.Warning:
            case LogLevel.Error:
            case LogLevel.Critical:
                return true;
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.None:
            default:
                return false;
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
}