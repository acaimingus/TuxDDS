using System.Runtime.InteropServices;
using System.Text;

namespace TuxDDSLib.Dds;

/// <summary>
/// Class for doing the work behind loading a DDS image texture
/// </summary>
public static class DdsLoader
{
    /// <summary>
    /// Wrapper method for getting all the needed DDS image metadata, needs to be called first so you can create the
    /// needed buffers in C#.
    /// </summary>
    /// <param name="filePath">Path to the DDS image</param>
    /// <param name="width">Width of the image, output</param>
    /// <param name="height">Height of the image, output</param>
    /// <param name="format">Image format as a String, needs to be a StringBuilder because of the way C++ handles
    /// Strings through pointers</param>
    /// <param name="bitsPerPixel">Integer specifying how many bits are needed per pixel, shows the biggest count if
    /// unequal, output</param>
    /// <param name="bitsPerColor">Integer specifying how many bits are needed per color, shows the biggest count if
    /// unequal, output</param>
    /// <returns>0 on success, else HRESULT code</returns>
    [DllImport("DirectXTexWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int ExtractDdsTextureInfo(string filePath, out int width, out int height,
        StringBuilder format, out int bitsPerPixel, out int bitsPerColor);

    /// <summary>
    /// Wrapper methode for getting the actual DDS image pixels, call ExtractDdsTextureInfo first to create the needed.
    /// buffers!
    /// </summary>
    /// <param name="filePath">Path to the DDS image</param>
    /// <param name="rawBufferSize">The buffer size needed for the raw image</param>
    /// <param name="previewBufferSize">The buffer size needed for the preview image</param>
    /// <param name="rawImageData">Actual raw image data</param>
    /// <param name="previewImageData">Actual preview image data</param>
    /// <returns></returns>
    [DllImport("DirectXTexWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int ExtractDdsTextureData(string filePath, int rawBufferSize, int previewBufferSize, byte[] rawImageData, byte[] previewImageData);

    /// <summary>
    /// Method for loading a DDS image texture using DirectXTex through the wrapper.
    /// </summary>
    /// <param name="filePath">Path to the DDS image to load</param>
    /// <param name="statusCallback">Status callback to display messages in the UI</param>
    /// <returns>0 on success, else HRESULT code</returns>
    public static DdsTexture? LoadDdsTexture(string filePath, Action<string> statusCallback)
    {
        // Check if the provided file path even exists
        if (!File.Exists(filePath))
        {
            statusCallback.Invoke($"ERROR: The specified file was not found: {filePath}");
            return null;
        }

        // Get the DDS file data
        var formatName = new StringBuilder(64);
        var errorCodeInfo =
            ExtractDdsTextureInfo(filePath, out var width, out var height, formatName, out var bitsPerPixel, out var bitsPerColor);
        if (errorCodeInfo != 0)
        {
            statusCallback.Invoke($"ERROR: Extracting DDS file data failed with HRESULT {errorCodeInfo}");
            return null;
        }

        // Report success to the user
        statusCallback.Invoke(
            $"INFO: Image loaded successfully! Size: {width} x {height} px / Format: {formatName}, {bitsPerPixel} Bits per Pixel, {bitsPerColor} Bits per Color");

        // Create a buffer to fit the image
        var bytesPerPixel = (int)Math.Ceiling((double)bitsPerPixel / 8);
        var rawImageData = new byte[width * height * bytesPerPixel];
        // This buffer is always R8G8B8A8, so it needs 4 bytes per pixel
        var previewImageData = new byte[width * height * 4];

        // Extract the image data
        var errorCodeData = ExtractDdsTextureData(filePath, rawImageData.Length, previewImageData.Length, rawImageData, previewImageData);
        if (errorCodeData != 0)
        {
            statusCallback.Invoke($"ERROR: Extracting DDS data failed with HRESULT {errorCodeData}");
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