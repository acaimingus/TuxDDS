namespace TuxDdsLib.Export;

/// <summary>
/// Summary result of a batch export operation.
/// </summary>
/// <param name="TotalFiles">Total number of DDS files found in the input folder.</param>
/// <param name="SucceededCount">Number of files exported successfully.</param>
/// <param name="FailedCount">Number of files that failed to load or export.</param>
public readonly record struct BatchExportResult(int TotalFiles, int SucceededCount, int FailedCount);
