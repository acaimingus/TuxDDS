using System.Reflection;
using Microsoft.Extensions.Logging;
using TuxDdsLib.Logging;

namespace TuxDdsCli;

internal class Program
{
    private static ILogger<Program>? _logger;

    static int Main(string[] args)
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
                ShowHelpInfo();
                return 0;
            default:
                // Show invalid command and then show help
                ShowInvalidCommand(args[0]);
                return 1;
        }
    }

    private static int ShowImageInfo(string[] args)
    {
        return 0;
    }

    private static int ConvertImage(string[] args)
    {
        return 0;
    }

    private static int ConvertBatch(string[] args)
    {
        return 0;
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

    static void ShowHelpInfo()
    {
        // Show available commands and give brief explanations
        // If there is a second argument specifying which command needs to be helped with then show help for that one
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