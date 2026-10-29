using System.Reflection;

namespace TuxDdsCli;

class Program
{
    static int Main(string[] args)
    {
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
            case "about" or "--version" or "-v":
                ShowAboutInfo();
                return 0;
            case "help" or "--help" or "-h":
                ShowHelpInfo();
                return 0;
            default:
                // Show invalid command and then show help
                ShowInvalidCommand(args[0]);
                return 1;
        }
    }

    static int ShowImageInfo(string[] args)
    {
        return 0;
    }

    static int ConvertImage(string[] args)
    {
        return 0;
    }

    static int ConvertBatch(string[] args)
    {
        return 0;
    }

    static int ShowLog()
    {
        return 0;
    }

    static void ShowAboutInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString(3) ?? "???";
        var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";
        
        Console.WriteLine($"TuxDDS CLI v{version}");
        Console.WriteLine(copyright);
        Console.WriteLine("Licensed under the MIT License.");
    }

    static void ShowHelpInfo()
    {
        // Show available commands and give brief explanations
        // If there is a second argument specifying which command needs to be helped with then show help for that one
    }

    static void ShowInvalidCommand(string commandArgument)
    {
        if (string.IsNullOrWhiteSpace(commandArgument))
        {
            Console.WriteLine("No command specified. Please enter a valid command.");
        }
        else
        {
            Console.WriteLine(
                $"The argument '{commandArgument}' is not a valid argument. Please enter a valid command.");
        }

        ShowHelpInfo();
    }
}