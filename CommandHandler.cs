using System.Diagnostics;
using System.Text.Json;

namespace FGC;

public static class CommandHandler
{
    public static void ResetInstall()
    {
        var config = Config.Instance;
        
        if (Directory.Exists(config.DestinationFreelancerPath))
        {
            Directory.Delete(config.DestinationFreelancerPath, true);
        }
        
        Console.WriteLine("Copying source files to destination");
        Utils.RecursiveCopy(config.SourceFreelancerPath, config.DestinationFreelancerPath);
    }
    
    public static async Task GenerateConfig()
    {
        var defaultSpewPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Freelancer",
            "flspew.txt"
        );

        string? sourcePath = null;
        string? destPath = null;
        string? modFilePath = null;
        string? pythonScriptPath = null;

        Console.WriteLine("Generating config...");
        Console.WriteLine("Please answer the questions.");
        while (string.IsNullOrWhiteSpace(sourcePath))
        {
            Console.WriteLine("Where is your base Freelancer install? ");
            Console.WriteLine();
            sourcePath = Console.ReadLine();

            if (sourcePath is null || !Directory.Exists(sourcePath))
            {
                Console.WriteLine("Error: Please enter a valid path.");
            }
        }
        
        while (string.IsNullOrWhiteSpace(modFilePath))
        {
            Console.WriteLine("Where are your mod assets? ");
            Console.WriteLine();
            modFilePath = Console.ReadLine();

            if (modFilePath is null || !Directory.Exists(modFilePath))
            {
                Console.WriteLine("Error: Please enter a valid path.");
            }
        }
        
        while (string.IsNullOrWhiteSpace(destPath))
        {
            Console.WriteLine("Where would you like to copy your mod files to? ");
            Console.WriteLine();
            destPath = Console.ReadLine();

            if (destPath is null || destPath.Length == 0)
            {
                Console.WriteLine("Error: Please enter a valid path, the directory does not need to exist.");
            }
        }
        
        Console.WriteLine($"Where is your FLSpew file ({defaultSpewPath})?");
        Console.WriteLine();
        var flSpewPath = Console.ReadLine();

        if (flSpewPath is null || flSpewPath.Length == 0)
        {
            flSpewPath = defaultSpewPath;
        }
        
        while (string.IsNullOrWhiteSpace(pythonScriptPath))
        {
            Console.WriteLine("Where is your python generation script?");
            Console.WriteLine();
            pythonScriptPath = Console.ReadLine();

            if (pythonScriptPath is null || !File.Exists(pythonScriptPath))
            {
                Console.WriteLine("Error: Please enter a valid file path.");
            }
        }
        
        var winePrefix = string.Empty;
        var wineBinary = string.Empty;

        if (OperatingSystem.IsLinux())
        {
            while (string.IsNullOrWhiteSpace(winePrefix))
            {
                Console.WriteLine("Where is your wine prefix?");
                Console.WriteLine();
                winePrefix = Console.ReadLine();

                if (winePrefix is null || !Directory.Exists(Path.Combine(winePrefix, "drive_c")))
                {
                    Console.WriteLine("Error: Please enter a valid wine prefix path.");
                }
            }
            
            Console.WriteLine($"Which wine binary are you wanting to use (wine)?");
            Console.WriteLine();
            wineBinary = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(wineBinary))
            {
                wineBinary = "wine";
            }
        }
        
        await using var outputFile = File.Create("config.json");
        Config.Instance = new Config()
        {
            SourceFreelancerPath = sourcePath,
            DestinationFreelancerPath = destPath,
            ModFilesPath = modFilePath,
            FlSpewPath = flSpewPath,
            PythonScriptPath = pythonScriptPath,
            WinePrefix = winePrefix,
            WineBinary = wineBinary,
            ServerArgs = [],
            GameArgs = []
        };
        await JsonSerializer.SerializeAsync(outputFile, Config.Instance, Config.Options);
        
        Console.WriteLine("Config generated!");
    }

    public static async Task LaunchFreelancer(bool server)
    {
        var config = Config.Instance;
        var freelancerPath = Path.Combine(config.DestinationFreelancerPath, "EXE");
        var freelancerExePath = Path.Combine(freelancerPath, "Freelancer.exe");
        var freelancerServerExePath = Path.Combine(freelancerPath, "FLServer.exe");

        if (!File.Exists(freelancerExePath) || !File.Exists(freelancerServerExePath))
        {
            throw new FileNotFoundException($"Could not find {freelancerExePath} or {freelancerServerExePath}");
        }

        if (server)
        {
            await Utils.RunProcess(freelancerServerExePath, freelancerPath, config.ServerArgs);
        }
        else
        {
            await Utils.RunProcess(freelancerExePath, freelancerPath, config.GameArgs);
        }
    }
}