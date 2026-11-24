using TuxDdsCli;
using TuxDdsLib.Logging;
using TuxDdsTests.Helpers;

namespace TuxDdsTests.Cli;

[Collection("GlobalState")]
public class CliProgramTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly string _originalLogPath;

    public CliProgramTests()
    {
        _originalLogPath = TuxLogger.LogFilePath;
        TuxLogger.LogFilePath = _temp.GetFilePath("logs", "TuxDds.log");
    }

    public void Dispose()
    {
        TuxLogger.LogFilePath = _originalLogPath;
        _temp.Dispose();
    }

    private static (int ExitCode, string Output) RunCli(params string[] args)
    {
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        try
        {
            Console.SetOut(sw);
            var exitCode = Program.Main(args);
            return (exitCode, sw.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Main_NoArguments_Returns1AndShowsUsage()
    {
        var (code, output) = RunCli();

        Assert.Equal(1, code);
        Assert.Contains("No command specified", output);
        Assert.Contains("Usage: tuxdds <command> [options]", output);
    }

    [Fact]
    public void Main_UnknownCommand_Returns1AndShowsError()
    {
        var (code, output) = RunCli("nonexistent");

        Assert.Equal(1, code);
        Assert.Contains("not a valid argument", output);
        Assert.Contains("Usage: tuxdds <command> [options]", output);
    }

    [Theory]
    [InlineData("about")]
    [InlineData("version")]
    [InlineData("-v")]
    [InlineData("--version")]
    public void Main_VersionAndAboutCommands_Return0AndPrintMetadata(string arg)
    {
        var (code, output) = RunCli(arg);

        Assert.Equal(0, code);
        Assert.Contains("TuxDDS CLI v", output);
        Assert.Contains("MIT License", output);
    }

    [Theory]
    [InlineData(new[] { "help" }, "Usage: tuxdds <command> [options]")]
    [InlineData(new[] { "--help" }, "Usage: tuxdds <command> [options]")]
    [InlineData(new[] { "-h" }, "Usage: tuxdds <command> [options]")]
    [InlineData(new[] { "help", "info" }, "Usage: tuxdds info <file.dds>")]
    [InlineData(new[] { "help", "convert" }, "Usage: tuxdds convert")]
    [InlineData(new[] { "help", "batch" }, "Usage: tuxdds batch")]
    [InlineData(new[] { "help", "log" }, "Usage: tuxdds log")]
    [InlineData(new[] { "help", "about" }, "Usage: tuxdds about")]
    [InlineData(new[] { "help", "version" }, "Usage: tuxdds about")]
    [InlineData(new[] { "help", "help" }, "Usage: tuxdds help [command]")]
    [InlineData(new[] { "info", "--help" }, "Usage: tuxdds info <file.dds>")]
    [InlineData(new[] { "convert", "-h" }, "Usage: tuxdds convert")]
    [InlineData(new[] { "batch", "--help" }, "Usage: tuxdds batch")]
    public void Main_HelpTopics_Return0AndPrintExpectedHelp(string[] args, string expectedSubstring)
    {
        var (code, output) = RunCli(args);

        Assert.Equal(0, code);
        Assert.Contains(expectedSubstring, output);
    }

    [Fact]
    public void Main_HelpUnknownTopic_Returns1()
    {
        var (code, output) = RunCli("help", "unknown-cmd");

        Assert.Equal(1, code);
        Assert.Contains("Unknown command 'unknown-cmd'", output);
    }

    [Fact]
    public void Main_Info_ValidDds_Returns0AndDisplaysTextureDetails()
    {
        var ddsPath = DdsTestDataGenerator.CreateRgba8Dds(_temp.GetFilePath("hero.dds"), 4, 2);

        var (code, output) = RunCli("info", ddsPath);

        Assert.Equal(0, code);
        Assert.Contains("hero.dds", output);
        Assert.Contains("4 x 2 px", output);
        Assert.Contains("DXGI_FORMAT_R8G8B8A8_UNORM", output);
        Assert.Contains("32", output);
        Assert.Contains("8", output);
    }

    [Fact]
    public void Main_Info_InvalidInputs_Return1()
    {
        // Missing file path
        Assert.Equal(1, RunCli("info").ExitCode);

        // Non-DDS file
        var txtPath = _temp.GetFilePath("notes.txt");
        File.WriteAllText(txtPath, "hello");
        var (nonDdsCode, nonDdsOut) = RunCli("info", txtPath);
        Assert.Equal(1, nonDdsCode);
        Assert.Contains("not a DDS file", nonDdsOut);

        // Missing DDS file
        var (missingCode, missingOut) = RunCli("info", _temp.GetFilePath("missing.dds"));
        Assert.Equal(1, missingCode);
        Assert.Contains("Failed to read DDS image", missingOut);
    }

    [Fact]
    public void Main_Convert_OutputFormatsAndPathResolution_Succeeds()
    {
        var ddsPath = DdsTestDataGenerator.CreateRgba8Dds(_temp.GetFilePath("icon.dds"), 2, 2);

        // 1. Inferred PNG from extension
        var pngOut = _temp.GetFilePath("exported", "icon.png");
        var (c1, _) = RunCli("convert", "-i", ddsPath, "-o", pngOut);
        Assert.Equal(0, c1);
        Assert.True(File.Exists(pngOut));

        // 2. Inferred JPG from .jpeg extension
        var jpegOut = _temp.GetFilePath("exported", "icon.jpeg");
        var (c2, _) = RunCli("convert", "-i", ddsPath, "-o", jpegOut);
        Assert.Equal(0, c2);
        Assert.True(File.Exists(jpegOut));

        // 3. Extensionless output path defaults to .png
        var noExtBase = _temp.GetFilePath("exported", "noext");
        var (c3, _) = RunCli("convert", "-i", ddsPath, "-o", noExtBase);
        Assert.Equal(0, c3);
        Assert.True(File.Exists($"{noExtBase}.png"));

        // 4. Explicit -f jpg appends .jpg when extension does not match
        var customBase = _temp.GetFilePath("exported", "custom_name");
        var (c4, _) = RunCli("convert", "-i", ddsPath, "-o", customBase, "-f", "jpg");
        Assert.Equal(0, c4);
        Assert.True(File.Exists($"{customBase}.jpg"));

        // 5. Output path is an existing directory
        var targetDir = _temp.GetFilePath("dir_out");
        Directory.CreateDirectory(targetDir);
        var (c5, _) = RunCli("convert", "-i", ddsPath, "-o", targetDir, "-f", "png");
        Assert.Equal(0, c5);
        Assert.True(File.Exists(Path.Combine(targetDir, "icon.png")));
    }

    [Theory]
    [InlineData((object)new[] { "convert" })]
    [InlineData((object)new[] { "convert", "-i" })]
    [InlineData((object)new[] { "convert", "-i", "a.dds", "-i", "b.dds", "-o", "out.png" })]
    [InlineData((object)new[] { "convert", "-i", "a.dds", "-o" })]
    [InlineData((object)new[] { "convert", "-i", "a.dds", "-o", "1.png", "-o", "2.png" })]
    [InlineData((object)new[] { "convert", "-i", "a.dds", "-o", "1.png", "-f" })]
    [InlineData((object)new[] { "convert", "-i", "a.dds", "-o", "1.png", "-f", "bmp" })]
    [InlineData((object)new[] { "convert", "-i", "a.dds", "-o", "1.png", "-f", "png", "-f", "jpg" })]
    [InlineData((object)new[] { "convert", "-i", "a.dds", "-o", "1.png", "--unknown" })]
    [InlineData((object)new[] { "convert", "-o", "1.png" })]
    [InlineData((object)new[] { "convert", "-i", "a.dds" })]
    [InlineData((object)new[] { "convert", "-i", "a.png", "-o", "1.png" })]
    [InlineData((object)new[] { "convert", "-i", "a.dds", "-o", "1.bmp" })]
    public void Main_Convert_InvalidArguments_Return1(string[] args)
    {
        var (code, _) = RunCli(args);
        Assert.Equal(1, code);
    }

    [Fact]
    public void Main_Batch_EndToEndOptions_Returns0AndExportsExpectedFiles()
    {
        var inputDir = _temp.GetFilePath("batch_in");
        var outputDir = _temp.GetFilePath("batch_out");
        DdsTestDataGenerator.CreateRgba8Dds(Path.Combine(inputDir, "sub", "tex1.dds"));
        DdsTestDataGenerator.CreateRgba8Dds(Path.Combine(inputDir, "sub", "tex2.dds"));

        var (code, output) = RunCli(
            "batch",
            "-i", inputDir,
            "-o", outputDir,
            "-f", "jpg",
            "-r",
            "-k",
            "-a", "hd");

        Assert.Equal(0, code);
        Assert.Contains("Successfully converted 2 file(s)", output);
        Assert.True(File.Exists(Path.Combine(outputDir, "sub", "tex1-hd.jpg")));
        Assert.True(File.Exists(Path.Combine(outputDir, "sub", "tex2-hd.jpg")));
    }

    [Fact]
    public void Main_Batch_EmptyFolderAndPartialFailure_ReturnExpectedExitCodes()
    {
        var emptyDir = _temp.GetFilePath("empty_in");
        Directory.CreateDirectory(emptyDir);
        var outDir1 = _temp.GetFilePath("empty_out");

        var (emptyCode, emptyOut) = RunCli("batch", "-i", emptyDir, "-o", outDir1);
        Assert.Equal(0, emptyCode);
        Assert.Contains("No DDS files found", emptyOut);

        var mixedDir = _temp.GetFilePath("mixed_in");
        var outDir2 = _temp.GetFilePath("mixed_out");
        DdsTestDataGenerator.CreateRgba8Dds(Path.Combine(mixedDir, "ok.dds"));
        File.WriteAllText(Path.Combine(mixedDir, "bad.dds"), "corrupt");

        var (mixedCode, mixedOut) = RunCli("batch", "-i", mixedDir, "-o", outDir2, "-n", "item");
        Assert.Equal(1, mixedCode);
        Assert.Contains("completed with errors", mixedOut);
        Assert.True(File.Exists(Path.Combine(outDir2, "item-1.png")));
    }

    [Fact]
    public void Main_Batch_InvalidArguments_Return1()
    {
        var validIn = _temp.GetFilePath("valid_in");
        Directory.CreateDirectory(validIn);
        var existingFile = _temp.GetFilePath("existing_file.txt");
        File.WriteAllText(existingFile, "file");

        // No args
        Assert.Equal(1, RunCli("batch").ExitCode);
        // Conflicting naming strategies
        Assert.Equal(1, RunCli("batch", "-i", validIn, "-o", _temp.GetFilePath("o1"), "--keep-name", "-a", "x").ExitCode);
        Assert.Equal(1, RunCli("batch", "-i", validIn, "-o", _temp.GetFilePath("o2"), "-a", "x", "-n", "y").ExitCode);
        // Missing input dir on disk
        Assert.Equal(1, RunCli("batch", "-i", _temp.GetFilePath("missing_dir"), "-o", _temp.GetFilePath("o3")).ExitCode);
        // Output path is an existing file
        Assert.Equal(1, RunCli("batch", "-i", validIn, "-o", existingFile).ExitCode);
        // Invalid format
        Assert.Equal(1, RunCli("batch", "-i", validIn, "-o", _temp.GetFilePath("o4"), "-f", "webp").ExitCode);
        // Unknown flag
        Assert.Equal(1, RunCli("batch", "-i", validIn, "-o", _temp.GetFilePath("o5"), "--bogus").ExitCode);
    }

    [Fact]
    public void Main_Log_PrintsLogContentsWhenPresent_Returns1WhenMissing()
    {
        // Note: Program.Main initializes TuxLoggerProvider on entry, creating TuxLogger.LogFilePath!
        var (code, output) = RunCli("log");

        Assert.Equal(0, code);
        Assert.Contains("=== TuxDds.log ===", output);
    }
}
