namespace TuxDDSLib.Dds;

/// <summary>
/// Description class for all the information needed about the DDS texture
/// </summary>
public class DdsTexture
{
    /// <summary>
    /// The name of the file
    /// </summary>
    public required string FileName { get; init; }
    /// <summary>
    /// The path to the file
    /// </summary>
    public required string Path { get; init; }
    /// <summary>
    /// The width of the image in pixels
    /// </summary>
    public required int Width { get; init; }
    /// <summary>
    /// The height of the image in pixels
    /// </summary>
    public required int Height { get; init; }
    /// <summary>
    /// The name of the image format as specified by DirectXTex
    /// </summary>
    public required string ImageFormat { get; init; }
    /// <summary>
    /// The amount of bits needed per pixel for the image format
    /// </summary>
    public required int BitsPerPixel { get; init; }
    /// <summary>
    /// The amount of bits needed per color channel for the image format
    /// </summary>
    public required int BitsPerColor { get; init; }
    /// <summary>
    /// Array containing the raw DDS image data (if the image was uncompressed, else it's raw R8G8B8A8_UNORM)
    /// </summary>
    public required byte[] RawImageData { get; init; }
    /// <summary>
    /// Array containing the DDS image data converted to R8G8B8A8_UNORM for preview purposes
    /// </summary>
    public required byte[] PreviewImageData { get; init; }
}