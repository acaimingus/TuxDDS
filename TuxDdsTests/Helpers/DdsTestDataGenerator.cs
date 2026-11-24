using System.Buffers.Binary;

namespace TuxDdsTests.Helpers;

/// <summary>
/// Generates minimal, spec-compliant DDS texture files for deterministic unit testing.
/// </summary>
internal static class DdsTestDataGenerator
{
    private const uint DdsMagic = 0x20534444; // "DDS "
    private const uint DdsHeaderSize = 124;
    private const uint DdsPixelFormatSize = 32;

    private const uint DdsdCaps = 0x1;
    private const uint DdsdHeight = 0x2;
    private const uint DdsdWidth = 0x4;
    private const uint DdsdPitch = 0x8;
    private const uint DdsdPixelFormat = 0x1000;
    private const uint DdsdLinearSize = 0x80000;

    private const uint DdpfAlphaPixels = 0x1;
    private const uint DdpfFourCc = 0x4;
    private const uint DdpfRgb = 0x40;

    private const uint DdsCapsTexture = 0x1000;

    public static string CreateRgba8Dds(string filePath, int width = 2, int height = 2, byte[]? pixels = null)
    {
        var payload = pixels ?? CreateDefaultRgbaPixels(width, height);
        if (payload.Length != width * height * 4)
        {
            throw new ArgumentException("Invalid RGBA8 pixel buffer length.", nameof(pixels));
        }

        WriteUncompressed32BitDds(
            filePath,
            width,
            height,
            rMask: 0x000000FF,
            gMask: 0x0000FF00,
            bMask: 0x00FF0000,
            aMask: 0xFF000000,
            payload);

        return filePath;
    }

    public static string CreateBgra8Dds(string filePath, int width = 2, int height = 2, byte[]? pixels = null)
    {
        var payload = pixels ?? CreateDefaultRgbaPixels(width, height);
        if (payload.Length != width * height * 4)
        {
            throw new ArgumentException("Invalid BGRA8 pixel buffer length.", nameof(pixels));
        }

        WriteUncompressed32BitDds(
            filePath,
            width,
            height,
            rMask: 0x00FF0000,
            gMask: 0x0000FF00,
            bMask: 0x000000FF,
            aMask: 0xFF000000,
            payload);

        return filePath;
    }

    public static string CreateBc1Dds(string filePath)
    {
        // 4x4 BC1 (DXT1) solid red block
        const int width = 4;
        const int height = 4;
        const uint fourCcDxt1 = 0x31545844; // "DXT1"

        var buffer = new byte[4 + DdsHeaderSize + 8];
        var span = buffer.AsSpan();

        BinaryPrimitives.WriteUInt32LittleEndian(span[0..4], DdsMagic);
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..8], DdsHeaderSize);
        BinaryPrimitives.WriteUInt32LittleEndian(
            span[8..12],
            DdsdCaps | DdsdHeight | DdsdWidth | DdsdPixelFormat | DdsdLinearSize);
        BinaryPrimitives.WriteUInt32LittleEndian(span[12..16], height);
        BinaryPrimitives.WriteUInt32LittleEndian(span[16..20], width);
        BinaryPrimitives.WriteUInt32LittleEndian(span[20..24], 8); // 8 bytes per 4x4 BC1 block
        BinaryPrimitives.WriteUInt32LittleEndian(span[28..32], 1); // mipMapCount = 1

        // DDS_PIXELFORMAT starts at byte offset 76 (4 magic + 72 into header)
        BinaryPrimitives.WriteUInt32LittleEndian(span[76..80], DdsPixelFormatSize);
        BinaryPrimitives.WriteUInt32LittleEndian(span[80..84], DdpfFourCc);
        BinaryPrimitives.WriteUInt32LittleEndian(span[84..88], fourCcDxt1);

        // dwCaps at byte offset 108
        BinaryPrimitives.WriteUInt32LittleEndian(span[108..112], DdsCapsTexture);

        // BC1 8-byte payload at offset 128: RGB565 pure red (0xF800), c0=0xF800, c1=0xF800, indices=0
        BinaryPrimitives.WriteUInt16LittleEndian(span[128..130], 0xF800);
        BinaryPrimitives.WriteUInt16LittleEndian(span[130..132], 0xF800);
        BinaryPrimitives.WriteUInt32LittleEndian(span[132..136], 0x00000000);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
        File.WriteAllBytes(filePath, buffer);
        return filePath;
    }

    private static void WriteUncompressed32BitDds(
        string filePath,
        int width,
        int height,
        uint rMask,
        uint gMask,
        uint bMask,
        uint aMask,
        byte[] payload)
    {
        var buffer = new byte[4 + DdsHeaderSize + payload.Length];
        var span = buffer.AsSpan();

        BinaryPrimitives.WriteUInt32LittleEndian(span[0..4], DdsMagic);
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..8], DdsHeaderSize);
        BinaryPrimitives.WriteUInt32LittleEndian(
            span[8..12],
            DdsdCaps | DdsdHeight | DdsdWidth | DdsdPixelFormat | DdsdPitch);
        BinaryPrimitives.WriteUInt32LittleEndian(span[12..16], (uint)height);
        BinaryPrimitives.WriteUInt32LittleEndian(span[16..20], (uint)width);
        BinaryPrimitives.WriteUInt32LittleEndian(span[20..24], (uint)(width * 4)); // row pitch
        BinaryPrimitives.WriteUInt32LittleEndian(span[28..32], 1); // mipMapCount = 1

        // DDS_PIXELFORMAT at offset 76
        BinaryPrimitives.WriteUInt32LittleEndian(span[76..80], DdsPixelFormatSize);
        BinaryPrimitives.WriteUInt32LittleEndian(span[80..84], DdpfRgb | DdpfAlphaPixels);
        BinaryPrimitives.WriteUInt32LittleEndian(span[88..92], 32); // RGBBitCount
        BinaryPrimitives.WriteUInt32LittleEndian(span[92..96], rMask);
        BinaryPrimitives.WriteUInt32LittleEndian(span[96..100], gMask);
        BinaryPrimitives.WriteUInt32LittleEndian(span[100..104], bMask);
        BinaryPrimitives.WriteUInt32LittleEndian(span[104..108], aMask);

        // dwCaps at offset 108
        BinaryPrimitives.WriteUInt32LittleEndian(span[108..112], DdsCapsTexture);

        payload.CopyTo(span[128..]);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
        File.WriteAllBytes(filePath, buffer);
    }

    public static byte[] CreateDefaultRgbaPixels(int width, int height)
    {
        var data = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++)
        {
            data[i * 4 + 0] = (byte)((i * 53) % 256);
            data[i * 4 + 1] = (byte)((i * 97) % 256);
            data[i * 4 + 2] = (byte)((i * 193) % 256);
            data[i * 4 + 3] = 255;
        }

        return data;
    }
}
