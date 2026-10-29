namespace TuxDdsLib.Export;

/// <summary>
/// Configuration options for a batch export operation.
/// </summary>
public class BatchExportOptions
{
    /// <summary>
    /// The input directory containing DDS files to convert.
    /// </summary>
    public required string InputFolder { get; init; }

    /// <summary>
    /// The target directory where converted files will be saved.
    /// </summary>
    public required string OutputFolder { get; init; }

    /// <summary>
    /// The target image format for exported files.
    /// </summary>
    public ExportFormats OutputFormat { get; init; } = ExportFormats.PNG;

    /// <summary>
    /// Whether to search subdirectories of <see cref="InputFolder"/> recursively.
    /// </summary>
    public bool RecursiveSearch { get; init; }

    /// <summary>
    /// Whether to preserve the relative subdirectory structure inside <see cref="OutputFolder"/>.
    /// </summary>
    public bool KeepFolderStructure { get; init; }

    /// <summary>
    /// Naming strategy to apply to exported files.
    /// </summary>
    public NamingStrategies NamingStrategy { get; init; } = NamingStrategies.KeepName;

    /// <summary>
    /// Additional string used by <see cref="NamingStrategies.Append"/> or <see cref="NamingStrategies.BatchRename"/>.
    /// </summary>
    public string? NamingString { get; init; }
}
