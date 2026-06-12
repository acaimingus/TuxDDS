using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TuxDDS;

public static class DdsLoader
{
    [DllImport("DirectXTexWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int ExtractDdsTextureInfo(string filePath, out int width, out int height, StringBuilder format);

    [DllImport("DirectXTexWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int ExtractDdsTextureData(string filePath, int bufferSize, out byte[] imageData);

    const string testPath = "/home/adam/test.dds";
    
    public static void LoadDdsTexture()
    {
        var formatName = new StringBuilder(64);
        var errorCodeInfo = ExtractDdsTextureInfo(testPath, out var width, out var height, formatName);
        if (errorCodeInfo != 0)
        {
            Console.WriteLine($"Extracting DDS file info failed with DirectXTex HRESULT {errorCodeInfo}");
            return;
        }
        Console.WriteLine("Image loaded successfully!");
        Console.WriteLine($"Size: {width} x {height} Pixel");
        Console.WriteLine($"Format: {formatName.ToString()}");
        
        // Create a buffer to fit the image
        // TODO: Instead of assuming width * height * 4, create a safer solution
        var imageData = new byte[width * height * 4];
        
        // Extract the image data
        var errorCodeData = ExtractDdsTextureData(testPath, imageData.Length, out imageData);
        if (errorCodeData != 0)
        {
            Console.WriteLine($"Extracting DDS file data failed with DirectXTex HRESULT {errorCodeInfo}");
            return;
        }
        
        // Pass the data to the UI
    }
}