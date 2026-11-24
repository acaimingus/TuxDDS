using System.Buffers.Binary;
using Microsoft.Extensions.Logging;
using TuxDdsLib.Export;
using TuxDdsTests.Helpers;

namespace TuxDdsTests.Lib;

public class ExporterTests
{
    [Fact]
    public void ExportToPng_ValidRgbaData_WritesValidPngWithDimensions()
    {
        using var temp = new TempDirectory();
        var outPath = temp.GetFilePath("out.png");
        var pixels = DdsTestDataGenerator.CreateDefaultRgbaPixels(4, 3);
        var logger = new TestLogger();

        var ok = Exporter.ExportToPng(outPath, pixels, 4, 3, logger);

        Assert.True(ok);
        Assert.True(File.Exists(outPath));

        var bytes = File.ReadAllBytes(outPath);
        // PNG signature: 89 50 4E 47 0D 0A 1A 0A
        Assert.True(bytes.Length > 24);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, bytes[..8]);
        // IHDR width and height at bytes 16..20 and 20..24 (big-endian)
        Assert.Equal(4u, BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4)));
        Assert.Equal(3u, BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4)));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains(".PNG"));
    }

    [Fact]
    public void ExportToPng_DirectoryAsOutputPath_ReturnsFalseAndLogsError()
    {
        using var temp = new TempDirectory();
        var pixels = DdsTestDataGenerator.CreateDefaultRgbaPixels(2, 2);
        var logger = new TestLogger();

        var ok = Exporter.ExportToPng(temp.Path, pixels, 2, 2, logger);

        Assert.False(ok);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public void ExportToJpg_ValidRgbaData_WritesValidJpegFile()
    {
        using var temp = new TempDirectory();
        var outPath = temp.GetFilePath("out.jpg");
        var pixels = DdsTestDataGenerator.CreateDefaultRgbaPixels(4, 4);
        var logger = new TestLogger();

        var ok = Exporter.ExportToJpg(outPath, pixels, 4, 4, logger);

        Assert.True(ok);
        Assert.True(File.Exists(outPath));

        var bytes = File.ReadAllBytes(outPath);
        Assert.True(bytes.Length > 4);
        // JPEG SOI (FF D8) and EOI (FF D9)
        Assert.Equal(0xFF, bytes[0]);
        Assert.Equal(0xD8, bytes[1]);
        Assert.Equal(0xFF, bytes[^2]);
        Assert.Equal(0xD9, bytes[^1]);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains(".JPG"));
    }

    [Fact]
    public void ExportToJpg_DirectoryAsOutputPath_ReturnsFalseAndLogsError()
    {
        using var temp = new TempDirectory();
        var pixels = DdsTestDataGenerator.CreateDefaultRgbaPixels(2, 2);
        var logger = new TestLogger();

        var ok = Exporter.ExportToJpg(temp.Path, pixels, 2, 2, logger);

        Assert.False(ok);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public void BatchExport_EmptyDirectory_ReturnsZeroCounts()
    {
        using var temp = new TempDirectory();
        var inputDir = temp.GetFilePath("in");
        var outputDir = temp.GetFilePath("out");
        Directory.CreateDirectory(inputDir);

        var result = Exporter.BatchExport(new BatchExportOptions
        {
            InputFolder = inputDir,
            OutputFolder = outputDir
        });

        Assert.Equal(new BatchExportResult(0, 0, 0), result);
    }

    [Theory]
    [InlineData(ExportFormats.PNG, "png")]
    [InlineData(ExportFormats.JPG, "jpg")]
    public void BatchExport_KeepName_ExportsCaseInsensitiveDdsFilesAndIgnoresOtherFiles(
        ExportFormats format,
        string expectedExtension)
    {
        using var temp = new TempDirectory();
        var inputDir = temp.GetFilePath("in");
        var outputDir = temp.GetFilePath("out");
        Directory.CreateDirectory(inputDir);

        DdsTestDataGenerator.CreateRgba8Dds(Path.Combine(inputDir, "tex_lower.dds"));
        DdsTestDataGenerator.CreateRgba8Dds(Path.Combine(inputDir, "TEX_UPPER.DDS"));
        File.WriteAllText(Path.Combine(inputDir, "readme.txt"), "ignore me");

        var result = Exporter.BatchExport(new BatchExportOptions
        {
            InputFolder = inputDir,
            OutputFolder = outputDir,
            OutputFormat = format,
            NamingStrategy = NamingStrategies.KeepName
        });

        Assert.Equal(new BatchExportResult(2, 2, 0), result);
        Assert.True(File.Exists(Path.Combine(outputDir, $"tex_lower.{expectedExtension}")));
        Assert.True(File.Exists(Path.Combine(outputDir, $"TEX_UPPER.{expectedExtension}")));
    }

    [Fact]
    public void BatchExport_AppendStrategy_AppendsSuffixToBaseNames()
    {
        using var temp = new TempDirectory();
        var inputDir = temp.GetFilePath("in");
        var outputDir = temp.GetFilePath("out");
        DdsTestDataGenerator.CreateRgba8Dds(Path.Combine(inputDir, "albedo.dds"));

        var result = Exporter.BatchExport(new BatchExportOptions
        {
            InputFolder = inputDir,
            OutputFolder = outputDir,
            OutputFormat = ExportFormats.PNG,
            NamingStrategy = NamingStrategies.Append,
            NamingString = "exported"
        });

        Assert.Equal(new BatchExportResult(1, 1, 0), result);
        Assert.True(File.Exists(Path.Combine(outputDir, "albedo-exported.png")));
    }

    [Fact]
    public void BatchExport_BatchRenameStrategy_NumbersExportedFilesSequentially()
    {
        using var temp = new TempDirectory();
        var inputDir = temp.GetFilePath("in");
        var outputDir = temp.GetFilePath("out");
        DdsTestDataGenerator.CreateRgba8Dds(Path.Combine(inputDir, "a.dds"));
        DdsTestDataGenerator.CreateRgba8Dds(Path.Combine(inputDir, "b.dds"));

        var result = Exporter.BatchExport(new BatchExportOptions
        {
            InputFolder = inputDir,
            OutputFolder = outputDir,
            OutputFormat = ExportFormats.PNG,
            NamingStrategy = NamingStrategies.BatchRename,
            NamingString = "sprite"
        });

        Assert.Equal(new BatchExportResult(2, 2, 0), result);
        Assert.True(File.Exists(Path.Combine(outputDir, "sprite-1.png")));
        Assert.True(File.Exists(Path.Combine(outputDir, "sprite-2.png")));
    }

    [Fact]
    public void BatchExport_RecursiveAndKeepFolderStructure_PreservesRelativeDirectories()
    {
        using var temp = new TempDirectory();
        var inputDir = temp.GetFilePath("in");
        var outNonRecursive = temp.GetFilePath("out_non_recursive");
        var outFlatRecursive = temp.GetFilePath("out_flat_recursive");
        var outStructured = temp.GetFilePath("out_structured");

        DdsTestDataGenerator.CreateRgba8Dds(Path.Combine(inputDir, "root.dds"));
        DdsTestDataGenerator.CreateRgba8Dds(Path.Combine(inputDir, "sub", "nested", "leaf.dds"));

        // Non-recursive only finds root.dds
        var r1 = Exporter.BatchExport(new BatchExportOptions
        {
            InputFolder = inputDir,
            OutputFolder = outNonRecursive,
            RecursiveSearch = false,
            KeepFolderStructure = false
        });
        Assert.Equal(new BatchExportResult(1, 1, 0), r1);
        Assert.True(File.Exists(Path.Combine(outNonRecursive, "root.png")));
        Assert.False(File.Exists(Path.Combine(outNonRecursive, "leaf.png")));

        // Recursive without KeepFolderStructure flattens into OutputFolder
        var r2 = Exporter.BatchExport(new BatchExportOptions
        {
            InputFolder = inputDir,
            OutputFolder = outFlatRecursive,
            RecursiveSearch = true,
            KeepFolderStructure = false
        });
        Assert.Equal(new BatchExportResult(2, 2, 0), r2);
        Assert.True(File.Exists(Path.Combine(outFlatRecursive, "root.png")));
        Assert.True(File.Exists(Path.Combine(outFlatRecursive, "leaf.png")));

        // Recursive with KeepFolderStructure preserves sub/nested/leaf.png
        var r3 = Exporter.BatchExport(new BatchExportOptions
        {
            InputFolder = inputDir,
            OutputFolder = outStructured,
            RecursiveSearch = true,
            KeepFolderStructure = true
        });
        Assert.Equal(new BatchExportResult(2, 2, 0), r3);
        Assert.True(File.Exists(Path.Combine(outStructured, "root.png")));
        Assert.True(File.Exists(Path.Combine(outStructured, "sub", "nested", "leaf.png")));
    }

    [Fact]
    public void BatchExport_PartialFailureWithCorruptDds_ReportsFailedCountAndLogsWarning()
    {
        using var temp = new TempDirectory();
        var inputDir = temp.GetFilePath("in");
        var outputDir = temp.GetFilePath("out");
        DdsTestDataGenerator.CreateRgba8Dds(Path.Combine(inputDir, "good.dds"));
        File.WriteAllText(Path.Combine(inputDir, "broken.dds"), "not a dds");
        var logger = new TestLogger();

        var result = Exporter.BatchExport(new BatchExportOptions
        {
            InputFolder = inputDir,
            OutputFolder = outputDir
        }, logger);

        Assert.Equal(new BatchExportResult(2, 1, 1), result);
        Assert.True(File.Exists(Path.Combine(outputDir, "good.png")));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("with errors"));
    }

    [Fact]
    public void BatchExport_InvalidExportFormat_ReturnsFailedCountAndLogsError()
    {
        using var temp = new TempDirectory();
        var inputDir = temp.GetFilePath("in");
        var outputDir = temp.GetFilePath("out");
        DdsTestDataGenerator.CreateRgba8Dds(Path.Combine(inputDir, "good.dds"));
        var logger = new TestLogger();

        var result = Exporter.BatchExport(new BatchExportOptions
        {
            InputFolder = inputDir,
            OutputFolder = outputDir,
            OutputFormat = (ExportFormats)999
        }, logger);

        Assert.Equal(1, result.TotalFiles);
        Assert.Equal(0, result.SucceededCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("invalid export format"));
    }
}
