using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TuxDdsTests.Helpers;

internal sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

internal class TestLogger : ILogger
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyCollection<LogEntry> Entries => _entries.ToArray();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Enqueue(new LogEntry(logLevel, formatter(state, exception), exception));
    }
}

internal sealed class TestLogger<T> : TestLogger, ILogger<T>;

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"tuxdds_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string GetFilePath(params string[] relativeSegments)
    {
        var all = new string[relativeSegments.Length + 1];
        all[0] = Path;
        Array.Copy(relativeSegments, 0, all, 1, relativeSegments.Length);
        return System.IO.Path.Combine(all);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in temporary test folders
        }
    }
}

[CollectionDefinition("GlobalState", DisableParallelization = true)]
public sealed class GlobalStateCollection;
