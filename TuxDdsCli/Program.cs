using System.Reflection;
using Microsoft.Extensions.Logging;
using TuxDDSLib.Dds;
using TuxDdsLib.Export;
using TuxDdsLib.Logging;

namespace TuxDdsCli;

internal class Program
{
    private static ILogger<Program>? _logger;

    internal static int Main(string[] args)
    {
        // Create the logger
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new TuxLoggerProvider());
        });
        _logger = loggerFactory.CreateLogger<Program>();

        // Check if the user did not specify any arguments
        if (args.Length <= 0)
        {
            // Show invalid command and then show help before exiting
            ShowInvalidCommand("");
            return 1;
        }

        // Check the task to do depending on the specified argument
        switch (args[0].ToLowerInvariant())
        {
            case "info":
                return ShowImageInfo(args[1..]);
            case "convert":
                return ConvertImage(args[1..]);
            case "batch":
                return ConvertBatch(args[1..]);
            case "log":
                return ShowLog();
            case "about":
            case "version":
            case "--version":
            case "-v":
                ShowAboutInfo();
                return 0;
            case "help":
            case "--help":
            case "-h":
                return ShowHelpInfo(args[1..]);
            default:
                // Show invalid command and then show help
                ShowInvalidCommand(args[0]);
                return 1;
        }
    }

    private static int ShowImageInfo(string[] args)
    {
        if (args.Length > 0 && args[0].ToLowerInvariant() is "-h" or "--help")
        {
            return ShowHelpInfo(["info"]);
        }

        if (args.Length <= 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            _logger?.LogError("No file path specified for the 'info' command.");
            Console.WriteLine("No file path specified. Please provide a path to a DDS file.");
            return 1;
        }

        var filePath = args[0];

        if (!filePath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
        {
            _logger?.LogError("The specified file is not a DDS file: '{FilePath}'.", filePath);
            Console.WriteLine($"The specified file '{filePath}' is not a DDS file.");
            return 1;
        }

        var ddsTexture = DdsLoader.LoadDdsTexture(filePath, _logger);
        if (ddsTexture == null)
        {
            Console.WriteLine($"Failed to read DDS image '{filePath}'. Check the log for details.");
            return 1;
        }

        Console.WriteLine($"File Name:      {ddsTexture.FileName}");
        Console.WriteLine($"File Path:      {Path.GetFullPath(ddsTexture.Path)}");
        Console.WriteLine($"Dimensions:     {ddsTexture.Width} x {ddsTexture.Height} px");
        Console.WriteLine($"Format:         {ddsTexture.ImageFormat}");
        Console.WriteLine($"Bits per Pixel: {ddsTexture.BitsPerPixel}");
        Console.WriteLine($"Bits per Color: {ddsTexture.BitsPerColor}");

        _logger?.LogInformation("Displayed image information for '{FilePath}'.", filePath);
        return 0;
    }

    private static int ConvertImage(string[] args)
    {
        const string usage = "Usage: tuxdds convert -i <input.dds> -o <output> [-f png|jpg]";

        if (args.Length <= 0)
        {
            _logger?.LogError("No arguments specified for the 'convert' command.");
            Console.WriteLine($"No arguments specified. {usage}");
            return 1;
        }

        string? inputPath = null;
        string? outputPath = null;
        ExportFormats? explicitFormat = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "-h":
                case "--help":
                    return ShowHelpInfo(["convert"]);

                case "-i":
                case "--input":
                    if (inputPath != null)
                    {
                        _logger?.LogError("Duplicate input option specified for 'convert'.");
                        Console.WriteLine($"Input option specified more than once. {usage}");
                        return 1;
                    }

                    if (i + 1 >= args.Length)
                    {
                        _logger?.LogError("Missing value for '{Option}' in 'convert'.", args[i]);
                        Console.WriteLine($"Missing file path after '{args[i]}'. {usage}");
                        return 1;
                    }

                    inputPath = args[++i];
                    break;

                case "-o":
                case "--output":
                    if (outputPath != null)
                    {
                        _logger?.LogError("Duplicate output option specified for 'convert'.");
                        Console.WriteLine($"Output option specified more than once. {usage}");
                        return 1;
                    }

                    if (i + 1 >= args.Length)
                    {
                        _logger?.LogError("Missing value for '{Option}' in 'convert'.", args[i]);
                        Console.WriteLine($"Missing output path after '{args[i]}'. {usage}");
                        return 1;
                    }

                    outputPath = args[++i];
                    break;

                case "-f":
                case "--format":
                    if (explicitFormat.HasValue)
                    {
                        _logger?.LogError("Duplicate format option specified for 'convert'.");
                        Console.WriteLine($"Format option specified more than once. {usage}");
                        return 1;
                    }

                    if (i + 1 >= args.Length)
                    {
                        _logger?.LogError("Missing value for '{Option}' in 'convert'.", args[i]);
                        Console.WriteLine($"Missing format after '{args[i]}'. Supported formats: png, jpg.");
                        return 1;
                    }

                    var formatArg = args[++i];
                    if (!TryParseExportFormat(formatArg, out var parsedFormat))
                    {
                        _logger?.LogError("Invalid export format specified for 'convert': '{Format}'.", formatArg);
                        Console.WriteLine($"Invalid format '{formatArg}'. Supported formats: png, jpg.");
                        return 1;
                    }

                    explicitFormat = parsedFormat;
                    break;

                default:
                    _logger?.LogError("Unknown argument '{Argument}' specified for 'convert'.", args[i]);
                    Console.WriteLine($"Unknown argument '{args[i]}'. {usage}");
                    return 1;
            }
        }

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            _logger?.LogError("No input file path specified for the 'convert' command.");
            Console.WriteLine($"No input file path specified. {usage}");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            _logger?.LogError("No output path specified for the 'convert' command.");
            Console.WriteLine($"No output path specified. {usage}");
            return 1;
        }

        if (!inputPath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
        {
            _logger?.LogError("The specified file is not a DDS file: '{FilePath}'.", inputPath);
            Console.WriteLine($"The specified file '{inputPath}' is not a DDS file.");
            return 1;
        }

        // Resolve output format and target file path
        ExportFormats exportFormat;
        string finalOutputFilePath;

        var isDirectory = Directory.Exists(outputPath)
                          || outputPath.EndsWith(Path.DirectorySeparatorChar)
                          || outputPath.EndsWith(Path.AltDirectorySeparatorChar);

        if (isDirectory)
        {
            exportFormat = explicitFormat ?? ExportFormats.PNG;
            var ext = exportFormat.ToString().ToLowerInvariant();
            var baseName = Path.GetFileNameWithoutExtension(inputPath);
            finalOutputFilePath = Path.Combine(outputPath, $"{baseName}.{ext}");
        }
        else
        {
            var outputExtension = Path.GetExtension(outputPath);

            if (explicitFormat.HasValue)
            {
                exportFormat = explicitFormat.Value;
                var expectedExtension = $".{exportFormat.ToString().ToLowerInvariant()}";
                var hasMatchingExtension = outputExtension.Equals(expectedExtension, StringComparison.OrdinalIgnoreCase)
                                           || (exportFormat == ExportFormats.JPG
                                               && outputExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase));

                finalOutputFilePath = hasMatchingExtension ? outputPath : $"{outputPath}{expectedExtension}";
            }
            else if (string.IsNullOrEmpty(outputExtension))
            {
                exportFormat = ExportFormats.PNG;
                finalOutputFilePath = $"{outputPath}.png";
            }
            else if (TryParseExportFormat(outputExtension.TrimStart('.'), out var inferredFormat))
            {
                exportFormat = inferredFormat;
                finalOutputFilePath = outputPath;
            }
            else
            {
                _logger?.LogError("Unsupported output file extension '{Extension}' for 'convert'.", outputExtension);
                Console.WriteLine(
                    $"Unsupported output extension '{outputExtension}'. Supported formats: png, jpg (or specify '-f png|jpg').");
                return 1;
            }
        }

        try
        {
            var parentDir = Path.GetDirectoryName(Path.GetFullPath(finalOutputFilePath));
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }
        }
        catch (Exception exception)
        {
            _logger?.LogError("Failed to create output directory for '{OutputPath}': {ExceptionMessage}",
                finalOutputFilePath, exception.Message);
            Console.WriteLine($"Failed to prepare output directory: {exception.Message}");
            return 1;
        }

        var ddsTexture = DdsLoader.LoadDdsTexture(inputPath, _logger);
        if (ddsTexture == null)
        {
            Console.WriteLine($"Failed to read DDS image '{inputPath}'. Check the log for details.");
            return 1;
        }

        var exported = exportFormat switch
        {
            ExportFormats.PNG => Exporter.ExportToPng(finalOutputFilePath, ddsTexture.PreviewImageData,
                ddsTexture.Width, ddsTexture.Height, _logger),
            ExportFormats.JPG => Exporter.ExportToJpg(finalOutputFilePath, ddsTexture.PreviewImageData,
                ddsTexture.Width, ddsTexture.Height, _logger),
            _ => false
        };

        if (!exported)
        {
            Console.WriteLine($"Failed to export image to '{finalOutputFilePath}'. Check the log for details.");
            return 1;
        }

        Console.WriteLine($"Converted '{inputPath}' to '{Path.GetFullPath(finalOutputFilePath)}'.");
        return 0;
    }

    private static int ConvertBatch(string[] args)
    {
        const string usage =
            "Usage: tuxdds batch -i <input-dir> -o <output-dir> [-f png|jpg] [-r] [-k] [-a <suffix> | -n <name>]";

        if (args.Length <= 0)
        {
            _logger?.LogError("No arguments specified for the 'batch' command.");
            Console.WriteLine($"No arguments specified. {usage}");
            return 1;
        }

        string? inputFolder = null;
        string? outputFolder = null;
        ExportFormats? explicitFormat = null;
        var recursive = false;
        var keepFolderStructure = false;
        var namingStrategy = NamingStrategies.KeepName;
        var namingStrategySpecified = false;
        string? namingString = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "-h":
                case "--help":
                    return ShowHelpInfo(["batch"]);

                case "-i":
                case "--input":
                    if (inputFolder != null)
                    {
                        _logger?.LogError("Duplicate input option specified for 'batch'.");
                        Console.WriteLine($"Input option specified more than once. {usage}");
                        return 1;
                    }

                    if (i + 1 >= args.Length)
                    {
                        _logger?.LogError("Missing value for '{Option}' in 'batch'.", args[i]);
                        Console.WriteLine($"Missing input directory after '{args[i]}'. {usage}");
                        return 1;
                    }

                    inputFolder = args[++i];
                    break;

                case "-o":
                case "--output":
                    if (outputFolder != null)
                    {
                        _logger?.LogError("Duplicate output option specified for 'batch'.");
                        Console.WriteLine($"Output option specified more than once. {usage}");
                        return 1;
                    }

                    if (i + 1 >= args.Length)
                    {
                        _logger?.LogError("Missing value for '{Option}' in 'batch'.", args[i]);
                        Console.WriteLine($"Missing output directory after '{args[i]}'. {usage}");
                        return 1;
                    }

                    outputFolder = args[++i];
                    break;

                case "-f":
                case "--format":
                    if (explicitFormat.HasValue)
                    {
                        _logger?.LogError("Duplicate format option specified for 'batch'.");
                        Console.WriteLine($"Format option specified more than once. {usage}");
                        return 1;
                    }

                    if (i + 1 >= args.Length)
                    {
                        _logger?.LogError("Missing value for '{Option}' in 'batch'.", args[i]);
                        Console.WriteLine($"Missing format after '{args[i]}'. Supported formats: png, jpg.");
                        return 1;
                    }

                    var formatArg = args[++i];
                    if (!TryParseExportFormat(formatArg, out var parsedFormat))
                    {
                        _logger?.LogError("Invalid export format specified for 'batch': '{Format}'.", formatArg);
                        Console.WriteLine($"Invalid format '{formatArg}'. Supported formats: png, jpg.");
                        return 1;
                    }

                    explicitFormat = parsedFormat;
                    break;

                case "-r":
                case "--recursive":
                    recursive = true;
                    break;

                case "-k":
                case "--keep-structure":
                case "--keep-folders":
                    keepFolderStructure = true;
                    break;

                case "--keep-name":
                    if (namingStrategySpecified)
                    {
                        _logger?.LogError("Multiple naming strategies specified for 'batch'.");
                        Console.WriteLine("Only one naming strategy ('--keep-name', '-a/--append', '-n/--rename') can be specified.");
                        return 1;
                    }

                    namingStrategy = NamingStrategies.KeepName;
                    namingStrategySpecified = true;
                    break;

                case "-a":
                case "--append":
                    if (namingStrategySpecified)
                    {
                        _logger?.LogError("Multiple naming strategies specified for 'batch'.");
                        Console.WriteLine("Only one naming strategy ('--keep-name', '-a/--append', '-n/--rename') can be specified.");
                        return 1;
                    }

                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
                    {
                        _logger?.LogError("Missing or empty value for '{Option}' in 'batch'.", args[i]);
                        Console.WriteLine($"Missing non-empty suffix after '{args[i]}'. {usage}");
                        return 1;
                    }

                    namingStrategy = NamingStrategies.Append;
                    namingString = args[++i];
                    namingStrategySpecified = true;
                    break;

                case "-n":
                case "--rename":
                case "--batch-rename":
                    if (namingStrategySpecified)
                    {
                        _logger?.LogError("Multiple naming strategies specified for 'batch'.");
                        Console.WriteLine("Only one naming strategy ('--keep-name', '-a/--append', '-n/--rename') can be specified.");
                        return 1;
                    }

                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
                    {
                        _logger?.LogError("Missing or empty value for '{Option}' in 'batch'.", args[i]);
                        Console.WriteLine($"Missing non-empty base name after '{args[i]}'. {usage}");
                        return 1;
                    }

                    namingStrategy = NamingStrategies.BatchRename;
                    namingString = args[++i];
                    namingStrategySpecified = true;
                    break;

                default:
                    _logger?.LogError("Unknown argument '{Argument}' specified for 'batch'.", args[i]);
                    Console.WriteLine($"Unknown argument '{args[i]}'. {usage}");
                    return 1;
            }
        }

        if (string.IsNullOrWhiteSpace(inputFolder))
        {
            _logger?.LogError("No input directory specified for the 'batch' command.");
            Console.WriteLine($"No input directory specified. {usage}");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            _logger?.LogError("No output directory specified for the 'batch' command.");
            Console.WriteLine($"No output directory specified. {usage}");
            return 1;
        }

        if (!Directory.Exists(inputFolder))
        {
            _logger?.LogError("Input directory does not exist: '{InputFolder}'.", inputFolder);
            Console.WriteLine($"Input directory '{inputFolder}' does not exist.");
            return 1;
        }

        if (File.Exists(outputFolder))
        {
            _logger?.LogError("Output path is an existing file, not a directory: '{OutputFolder}'.", outputFolder);
            Console.WriteLine($"Output path '{outputFolder}' is an existing file, not a directory.");
            return 1;
        }

        try
        {
            Directory.CreateDirectory(outputFolder);
        }
        catch (Exception exception)
        {
            _logger?.LogError("Failed to create output directory '{OutputFolder}': {ExceptionMessage}",
                outputFolder, exception.Message);
            Console.WriteLine($"Failed to create output directory '{outputFolder}': {exception.Message}");
            return 1;
        }

        var options = new BatchExportOptions
        {
            InputFolder = inputFolder,
            OutputFolder = outputFolder,
            OutputFormat = explicitFormat ?? ExportFormats.PNG,
            RecursiveSearch = recursive,
            KeepFolderStructure = keepFolderStructure,
            NamingStrategy = namingStrategy,
            NamingString = namingString
        };

        _logger?.LogInformation("Starting batch conversion from '{InputFolder}' to '{OutputFolder}'...",
            inputFolder, outputFolder);

        var result = Exporter.BatchExport(options, _logger);

        if (result.TotalFiles == 0)
        {
            Console.WriteLine($"No DDS files found in '{inputFolder}'.");
            return 0;
        }

        if (result.FailedCount > 0)
        {
            Console.WriteLine(
                $"Batch conversion completed with errors: {result.SucceededCount}/{result.TotalFiles} succeeded, {result.FailedCount} failed. Check the log for details.");
            return 1;
        }

        Console.WriteLine(
            $"Successfully converted {result.SucceededCount} file(s) from '{inputFolder}' to '{Path.GetFullPath(outputFolder)}'.");
        return 0;
    }

    private static bool TryParseExportFormat(string formatString, out ExportFormats format)
    {
        switch (formatString.Trim().ToLowerInvariant())
        {
            case "png":
                format = ExportFormats.PNG;
                return true;
            case "jpg":
            case "jpeg":
                format = ExportFormats.JPG;
                return true;
            default:
                format = default;
                return false;
        }
    }

    private static int ShowLog()
    {
        if (!File.Exists(TuxLogger.LogFilePath))
        {
            _logger?.LogError("Log file not found at '{LogFilePath}'.", TuxLogger.LogFilePath);
            Console.WriteLine($"Log file not found at '{TuxLogger.LogFilePath}'.");
            return 1;
        }

        _logger?.LogInformation("Displayed log file contents from '{LogFilePath}'.", TuxLogger.LogFilePath);
        Console.WriteLine(File.ReadAllText(TuxLogger.LogFilePath));
        return 0;
    }

    private static void ShowAboutInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString(3) ?? "???";
        var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";

        Console.WriteLine($"TuxDDS CLI v{version}");
        Console.WriteLine(copyright);
        Console.WriteLine("Licensed under the MIT License.");

        _logger?.LogInformation("Displayed about information for TuxDDS CLI v{Version}.", version);
    }

    private static int ShowHelpInfo(string[]? args = null)
    {
        if (args is { Length: > 0 } && !string.IsNullOrWhiteSpace(args[0]))
        {
            var topic = args[0].ToLowerInvariant();
            switch (topic)
            {
                case "info":
                    Console.WriteLine("Usage: tuxdds info <file.dds>");
                    Console.WriteLine();
                    Console.WriteLine("Displays metadata (dimensions, format, bits per pixel/color) for a DDS image.");
                    _logger?.LogInformation("Displayed help information for command 'info'.");
                    return 0;

                case "convert":
                    Console.WriteLine("Usage: tuxdds convert -i <input.dds> -o <output> [-f png|jpg]");
                    Console.WriteLine();
                    Console.WriteLine("Converts a single DDS image to PNG or JPG.");
                    Console.WriteLine();
                    Console.WriteLine("Options:");
                    Console.WriteLine("  -i, --input <file.dds>   Path to the input DDS file (required)");
                    Console.WriteLine("  -o, --output <path>      Output file path or target directory (required)");
                    Console.WriteLine("  -f, --format <png|jpg>   Output format (inferred from extension if omitted, default: png)");
                    Console.WriteLine("  -h, --help               Show help for this command");
                    _logger?.LogInformation("Displayed help information for command 'convert'.");
                    return 0;

                case "batch":
                    Console.WriteLine(
                        "Usage: tuxdds batch -i <input-dir> -o <output-dir> [-f png|jpg] [-r] [-k] [-a <suffix> | -n <name> | --keep-name]");
                    Console.WriteLine();
                    Console.WriteLine("Converts all DDS images inside a folder.");
                    Console.WriteLine();
                    Console.WriteLine("Options:");
                    Console.WriteLine("  -i, --input <dir>                   Input folder containing DDS files (required)");
                    Console.WriteLine("  -o, --output <dir>                  Target output folder (required)");
                    Console.WriteLine("  -f, --format <png|jpg>              Output format (default: png)");
                    Console.WriteLine("  -r, --recursive                     Search subdirectories recursively");
                    Console.WriteLine("  -k, --keep-structure, --keep-folders Preserve relative folder structure in output");
                    Console.WriteLine("      --keep-name                     Keep original file names (default)");
                    Console.WriteLine("  -a, --append <suffix>               Append '-<suffix>' to output file names");
                    Console.WriteLine("  -n, --rename, --batch-rename <name> Rename files sequentially ('<name>-1', '<name>-2', ...)");
                    Console.WriteLine("  -h, --help                          Show help for this command");
                    _logger?.LogInformation("Displayed help information for command 'batch'.");
                    return 0;

                case "log":
                    Console.WriteLine("Usage: tuxdds log");
                    Console.WriteLine();
                    Console.WriteLine("Prints the contents of the TuxDDS application log file.");
                    _logger?.LogInformation("Displayed help information for command 'log'.");
                    return 0;

                case "about":
                case "version":
                    Console.WriteLine("Usage: tuxdds about | version | -v | --version");
                    Console.WriteLine();
                    Console.WriteLine("Displays version, copyright, and license information.");
                    _logger?.LogInformation("Displayed help information for command '{Topic}'.", topic);
                    return 0;

                case "help":
                    Console.WriteLine("Usage: tuxdds help [command]");
                    Console.WriteLine();
                    Console.WriteLine("Displays general help or detailed help for a specific command.");
                    _logger?.LogInformation("Displayed help information for command 'help'.");
                    return 0;

                default:
                    _logger?.LogError("Unknown help topic requested: '{Topic}'.", args[0]);
                    Console.WriteLine($"Unknown command '{args[0]}'.");
                    Console.WriteLine();
                    break;
            }
        }

        Console.WriteLine("Usage: tuxdds <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  info <file.dds>        Display metadata about a DDS image");
        Console.WriteLine("  convert [options]      Convert a single DDS image to PNG or JPG");
        Console.WriteLine("  batch [options]        Batch convert DDS images in a directory");
        Console.WriteLine("  log                    Display the application log");
        Console.WriteLine("  about, version, -v     Show version and license information");
        Console.WriteLine("  help [command], -h     Show general help or help for a command");
        Console.WriteLine();
        Console.WriteLine("Run 'tuxdds help <command>' for detailed options (e.g. 'tuxdds help convert').");

        if (args is { Length: > 0 } && !string.IsNullOrWhiteSpace(args[0]))
        {
            return 1;
        }

        _logger?.LogInformation("Displayed general help information.");
        return 0;
    }

    private static void ShowInvalidCommand(string commandArgument)
    {
        if (string.IsNullOrWhiteSpace(commandArgument))
        {
            _logger?.LogError("No command specified.");
            Console.WriteLine("No command specified. Please enter a valid command.");
        }
        else
        {
            _logger?.LogError("Invalid command argument specified: '{CommandArgument}'.", commandArgument);
            Console.WriteLine(
                $"The argument '{commandArgument}' is not a valid argument. Please enter a valid command.");
        }

        ShowHelpInfo();
    }
}