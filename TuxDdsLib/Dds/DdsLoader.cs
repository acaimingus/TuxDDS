using System.Runtime.InteropServices;
using System.Text;

namespace TuxDDSLib.Dds;

public static class DdsLoader
{
    [DllImport("DirectXTexWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int ExtractDdsTextureInfo(string filePath, out int width, out int height,
        StringBuilder format, out int bitsPerPixel, out int bitsPerColor);

    [DllImport("DirectXTexWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int ExtractDdsTextureData(string filePath, int rawBufferSize, int previewBufferSize, byte[] rawImageData, byte[] previewImageData);

    public static DdsTexture? LoadDdsTexture(string filePath, Action<string> statusCallback)
    {
        // Check if the provided file path even exists
        if (!File.Exists(filePath))
        {
            statusCallback.Invoke($"The specified file was not found: {filePath}");
            return null;
        }

        var formatName = new StringBuilder(64);
        var errorCodeInfo =
            ExtractDdsTextureInfo(filePath, out var width, out var height, formatName, out var bitsPerPixel, out var bitsPerColor);
        if (errorCodeInfo != 0)
        {
            statusCallback.Invoke($"Extracting DDS file data failed with HRESULT {errorCodeInfo}");
            return null;
        }

        statusCallback.Invoke(
            $"Image loaded successfully! Size: {width} x {height} px / Format: {formatName}, {bitsPerPixel} Bits per Pixel, {bitsPerColor} Bits per Color");

        // Create a buffer to fit the image
        var bytesPerPixel = (int)Math.Ceiling((double)bitsPerPixel / 8);
        var rawImageData = new byte[width * height * bytesPerPixel];
        // This buffer is always R8G8B8A8, so it needs 4 bytes per pixel
        var previewImageData = new byte[width * height * 4];

        // Extract the image data
        var errorCodeData = ExtractDdsTextureData(filePath, rawImageData.Length, previewImageData.Length, rawImageData, previewImageData);
        if (errorCodeData != 0)
        {
            statusCallback.Invoke($"Extracting DDS data failed with HRESULT {errorCodeData}");
            return null;
        }
        
        // Return the data object
        return new DdsTexture
        {
            FileName = Path.GetFileName(filePath),
            Path = filePath,
            Width = width,
            Height = height,
            ImageFormat = formatName.ToString(),
            BitsPerPixel = bitsPerPixel,
            BitsPerColor = bitsPerColor,
            RawImageData = rawImageData,
            PreviewImageData = previewImageData
        };
    }
}