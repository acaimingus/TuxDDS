using Microsoft.Extensions.Logging;
using TuxDdsLib.Logging;
using TuxDdsTests.Helpers;

namespace TuxDdsTests.Lib;

public class LoggingTests
{
    [Theory]
    [InlineData(LogLevel.Trace, false)]
    [InlineData(LogLevel.Debug, false)]
    [InlineData(LogLevel.None, false)]
    [InlineData(LogLevel.Information, true)]
    [InlineData(LogLevel.Warning, true)]
    [InlineData(LogLevel.Error, true)]
    [InlineData(LogLevel.Critical, true)]
    public void TuxLogger_IsEnabled_MatchesExpectedLogLevels(LogLevel level, bool expected)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms);
        var logger = new TuxLogger(null, "TestCategory", writer);

        Assert.Equal(expected, logger.IsEnabled(level));
        Assert.Null(logger.BeginScope("scope"));
    }

    [Fact]
    public void TuxLogger_Log_WritesEnabledMessageToStreamAndUiCallback_SkipsDisabledLevels()
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms) { AutoFlush = true };
        var uiMessages = new List<string>();
        var logger = new TuxLogger(uiMessages.Add, "UnitTestCategory", writer);

        logger.LogDebug("Should be ignored");
        logger.LogInformation("Hello TuxDDS");

        ms.Position = 0;
        var written = new StreamReader(ms).ReadToEnd();

        Assert.DoesNotContain("Should be ignored", written);
        Assert.Contains("[INFORMATION]", written);
        Assert.Contains("UnitTestCategory: Hello TuxDDS", written);

        Assert.Single(uiMessages);
        Assert.Equal("[INFORMATION] Hello TuxDDS", uiMessages[0]);
    }

    [Fact]
    public void TuxLoggerProvider_CreatesDirectoryAndWritesHeaderAndLogsMessages()
    {
        using var temp = new TempDirectory();
        var logFilePath = temp.GetFilePath("nested", "TuxDds.log");
        var callbacks = new List<string>();

        using (var provider = new TuxLoggerProvider(callbacks.Add, logFilePath))
        {
            var logger = provider.CreateLogger("ProviderTest");
            logger.LogWarning("Disk nearly full");
        }

        Assert.True(File.Exists(logFilePath));
        var lines = File.ReadAllLines(logFilePath);
        Assert.True(lines.Length >= 3);
        Assert.Equal("=== TuxDds.log ===", lines[0]);
        Assert.Equal("", lines[1]);
        Assert.Contains(lines, l => l.Contains("[WARNING]") && l.Contains("ProviderTest: Disk nearly full"));
        Assert.Contains("[WARNING] Disk nearly full", callbacks);
    }

    [Fact]
    public void TuxLoggerProvider_WhenLogExceeds1000Lines_TrimsToHeaderPlusNewest500Lines()
    {
        using var temp = new TempDirectory();
        var logFilePath = temp.GetFilePath("TuxDds.log");

        var initialLines = Enumerable.Range(1, 1050)
            .Select(i => $"Line-{i:D4}")
            .ToArray();
        File.WriteAllLines(logFilePath, initialLines);

        using (var provider = new TuxLoggerProvider(logFilePath: logFilePath))
        {
            var logger = provider.CreateLogger("TrimTest");
            logger.LogInformation("After trim");
        }

        var resultLines = File.ReadAllLines(logFilePath);
        // Header ("=== TuxDds.log ===", "") + 500 kept lines (Line-0551..Line-1050) + 1 newly logged line = 503 lines
        Assert.Equal(503, resultLines.Length);
        Assert.Equal("=== TuxDds.log ===", resultLines[0]);
        Assert.Equal("Line-0551", resultLines[2]);
        Assert.Equal("Line-1050", resultLines[501]);
        Assert.Contains("After trim", resultLines[502]);
    }
}
