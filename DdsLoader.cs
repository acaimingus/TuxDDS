using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TuxDDS;

public static class DdsLoader
{
    [DllImport("DirectXTexWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int ExtractDdsTextureInfo(string filePath, out int width, out int height,
        StringBuilder format);

    [DllImport("DirectXTexWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int ExtractDdsTextureData(string filePath, int bufferSize, byte[] imageData);

    public static DdsTexture LoadDdsTexture(string filePath, Action<string> statusCallback)
    {
        // Check if the provided file path even exists
        if (!File.Exists(filePath))
        {
            statusCallback?.Invoke($"The specified file was not found: {filePath}");
            return null;
        }

        var formatName = new StringBuilder(64);
        var errorCodeInfo = ExtractDdsTextureInfo(filePath, out var width, out var height, formatName);
        if (errorCodeInfo != 0)
        {
            statusCallback?.Invoke($"Extracting DDS file data failed with error code {errorCodeInfo}");
            return null;
        }

        statusCallback?.Invoke(
            $"Image loaded successfully! Size: {width} x {height} px / Format: {formatName}");

        // Create a buffer to fit the image
        // TODO: Instead of assuming width * height * 4, create a safer solution
        var imageData = new byte[width * height * 4];

        // Extract the image data
        var errorCodeData = ExtractDdsTextureData(filePath, imageData.Length, imageData);
        if (errorCodeData != 0)
        {
            statusCallback?.Invoke($"Extracting DDS data failed with error code {errorCodeData}");
            return null;
        }
        
        // Return the data object
        return new DdsTexture { 
            FileName = filePath,
            Path = filePath,
            Width = width,
            Height = height,
            ImageFormat = formatName.ToString(),
            ImageData = imageData,
        };
    }
}