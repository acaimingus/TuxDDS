namespace TuxDDS.Dds;

/// <summary>
/// Description class for all the information needed about the DDS texture
/// </summary>
public class DdsTexture
{
    /// <summary>
    /// The name of the file
    /// </summary>
    public string FileName { get; init; }
    /// <summary>
    /// The path to the file
    /// </summary>
    public string Path { get; init; }
    /// <summary>
    /// The width of the image in pixels
    /// </summary>
    public int Width { get; init; }
    /// <summary>
    /// The height of the image in pixels
    /// </summary>
    public int Height { get; init; }
    /// <summary>
    /// The name of the image format as specified by DirectXTex
    /// </summary>
    public string ImageFormat { get; init; }
    /// <summary>
    /// The amount of bytes needed per pixel for the image format
    /// </summary>
    public int BytesPerPixel { get; init; }
    /// <summary>
    /// The RGBA array of the DDS image data
    /// </summary>
    public byte[] ImageData { get; init; }
}