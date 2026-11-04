using Microsoft.Extensions.Logging;

namespace TuxDdsLib.Logging;

public class TuxLoggerProvider : ILoggerProvider
{
    private const int MaxLogLines = 1000;
    private const int TrimToLogLines = 500;

    private readonly Action<string>? _logCallback;
    private readonly StreamWriter _streamWriter;

    public TuxLoggerProvider(Action<string>? logCallback = null)
    {
        _logCallback = logCallback;

        // Check if the directory exists before writing to it
        var logDir = Path.GetDirectoryName(TuxLogger.LogFilePath);
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir!);
        }

        // If the log file exceeds the maximum line count, keep only the newest lines
        if (File.Exists(TuxLogger.LogFilePath))
        {
            var lines = File.ReadAllLines(TuxLogger.LogFilePath);
            if (lines.Length > MaxLogLines)
            {
                var trimmedLines = lines.TakeLast(TrimToLogLines);
                File.WriteAllLines(TuxLogger.LogFilePath, ["=== TuxDds.log ===", "", ..trimmedLines]);
            }
        }

        // Open the streamWriter in append mode
        var fileStream = new FileStream(
            TuxLogger.LogFilePath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.ReadWrite);
        _streamWriter = new StreamWriter(fileStream)
        {
            AutoFlush = true
        };

        // Write the header for the log file if it is empty
        if (_streamWriter.BaseStream.Length == 0)
        {
            _streamWriter.WriteLine("=== TuxDds.log ===");
            _streamWriter.WriteLine("");
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        // Pass the same StreamWriter to a new logger when one is created
        return new TuxLogger(_logCallback, categoryName, _streamWriter);
    }

    public void Dispose()
    {
        // Dispose of the StreamWriter after use
        _streamWriter.Dispose();
    }
}
