using Microsoft.Extensions.Logging;
using TuxDDSLib.Dds;
using TuxDdsTests.Helpers;

namespace TuxDdsTests.Lib;

public class DdsLoaderTests
{
    [Fact]
    public void LoadDdsTexture_ValidRgba8File_ReturnsExpectedMetadataAndPixels()
    {
        using var temp = new TempDirectory();
        var expectedPixels = new byte[]
        {
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            255, 255, 0, 128,
            12, 34, 56, 78,
            90, 120, 150, 200
        };
        var ddsPath = DdsTestDataGenerator.CreateRgba8Dds(temp.GetFilePath("sample.dds"), 3, 2, expectedPixels);
        var logger = new TestLogger();

        var texture = DdsLoader.LoadDdsTexture(ddsPath, logger);

        Assert.NotNull(texture);
        Assert.Equal("sample.dds", texture.FileName);
        Assert.Equal(ddsPath, texture.Path);
        Assert.Equal(3, texture.Width);
        Assert.Equal(2, texture.Height);
        Assert.Equal("DXGI_FORMAT_R8G8B8A8_UNORM", texture.ImageFormat);
        Assert.Equal(32, texture.BitsPerPixel);
        Assert.Equal(8, texture.BitsPerColor);
        Assert.Equal(expectedPixels, texture.RawImageData);
        Assert.Equal(expectedPixels, texture.PreviewImageData);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("Image loaded successfully"));
    }

    [Fact]
    public void LoadDdsTexture_ValidBgra8File_ConvertsPreviewDataToRgba8()
    {
        using var temp = new TempDirectory();
        // 1x1 BGRA pixel: B=15, G=45, R=210, A=255
        var bgraPixels = new byte[] { 15, 45, 210, 255 };
        var ddsPath = DdsTestDataGenerator.CreateBgra8Dds(temp.GetFilePath("bgra.dds"), 1, 1, bgraPixels);

        var texture = DdsLoader.LoadDdsTexture(ddsPath);

        Assert.NotNull(texture);
        Assert.Equal("DXGI_FORMAT_B8G8R8A8_UNORM", texture.ImageFormat);
        Assert.Equal(bgraPixels, texture.RawImageData);
        Assert.Equal(new byte[] { 210, 45, 15, 255 }, texture.PreviewImageData);
    }

    [Fact]
    public void LoadDdsTexture_CompressedBc1File_DecompressesToRgba8()
    {
        using var temp = new TempDirectory();
        var ddsPath = DdsTestDataGenerator.CreateBc1Dds(temp.GetFilePath("compressed.dds"));

        var texture = DdsLoader.LoadDdsTexture(ddsPath);

        Assert.NotNull(texture);
        Assert.Equal(4, texture.Width);
        Assert.Equal(4, texture.Height);
        Assert.Equal("DXGI_FORMAT_BC1_UNORM", texture.ImageFormat);
        Assert.Equal(32, texture.BitsPerPixel);
        Assert.Equal(8, texture.BitsPerColor);
        Assert.Equal(4 * 4 * 4, texture.PreviewImageData.Length);
        // First pixel should be pure red (R=255, G=0, B=0, A=255)
        Assert.Equal(255, texture.PreviewImageData[0]);
        Assert.Equal(0, texture.PreviewImageData[1]);
        Assert.Equal(0, texture.PreviewImageData[2]);
        Assert.Equal(255, texture.PreviewImageData[3]);
    }

    [Fact]
    public void LoadDdsTexture_MissingFile_ReturnsNullAndLogsError()
    {
        using var temp = new TempDirectory();
        var missingPath = temp.GetFilePath("missing.dds");
        var logger = new TestLogger();

        var texture = DdsLoader.LoadDdsTexture(missingPath, logger);

        Assert.Null(texture);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("not found"));
    }

    [Fact]
    public void LoadDdsTexture_CorruptFile_ReturnsNullAndLogsError()
    {
        using var temp = new TempDirectory();
        var corruptPath = temp.GetFilePath("corrupt.dds");
        File.WriteAllBytes(corruptPath, [0x01, 0x02, 0x03, 0x04, 0x05]);
        var logger = new TestLogger();

        var texture = DdsLoader.LoadDdsTexture(corruptPath, logger);

        Assert.Null(texture);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("HRESULT"));
    }
}
