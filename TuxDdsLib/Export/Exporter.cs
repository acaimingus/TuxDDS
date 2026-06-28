namespace TuxDdsLib.Export;

using StbImageWriteSharp;

public static class Exporter
{
    public static void ExportToPng(string filePath, byte[] imageData, int imageWidth, int imageHeight, Action<string> statusCallback)
    {
        try
        {
            // Open an ImageWriter and write the PNG
            using var stream = File.Open(filePath, FileMode.Create);
            var writer = new ImageWriter();
            
            writer.WritePng(imageData, imageWidth, imageHeight, ColorComponents.RedGreenBlueAlpha, stream);
            statusCallback($"Export to {filePath} as .PNG was successful!");
        }
        catch (Exception exception)
        {
            // Update the status message and return
            statusCallback($"Exporting the image failed: {exception.Message}");
        }
    }
    
    public static void ExportToJpg(string filePath, byte[] imageData, int imageWidth, int imageHeight, Action<string> statusCallback)
    {
        try
        {
            // Open an ImageWriter and write the JPG
            using var stream = File.Open(filePath, FileMode.Create);
            var writer = new ImageWriter();
            
            writer.WriteJpg(imageData, imageWidth, imageHeight, ColorComponents.RedGreenBlueAlpha, stream, 100);
            statusCallback($"Export to {filePath} as .JPG was successful!");
        }
        catch (Exception exception)
        {
            // Update the status message and return
            statusCallback($"Exporting the image failed: {exception.Message}");
        }
    }
}