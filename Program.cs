using System.CommandLine;
using System.Text.Json;
using FGC;

var launchServerOption = new Option<bool>(
    name: "--server",
    description: "Launch FLServer");

var compileFrcFile = new Option<FileInfo[]>(
    name: "--frc",
    description: "FRC files to compile").ExistingOnly();

var generateConfigOpt = new Option<bool>(
    name: "--config",
    description: "Generate config file");

var resetOpt = new Option<bool>(
    name: "--reset",
    description:
    "Purges any files from your destination directory and copies a fresh install from your source directory"
);

var rootCommand = new RootCommand("Freelancer Gate Construction - Freelancer BMod Development Launcher");

var buildCommand = new Command("build", "Builds and copies mod files");
buildCommand.AddOption(resetOpt);
buildCommand.AddOption(generateConfigOpt);
buildCommand.AddOption(compileFrcFile);
buildCommand.SetHandler(async (reset, generateConfig, frc) =>
{
    if (generateConfig)
    {
        await CommandHandler.GenerateConfig();
    }
    
    if (reset)
    {
        CommandHandler.ResetInstall();
    }

    if (frc.Length is not 0)
    {
        Console.WriteLine("Compiling frc files...");
    }
    
    var config = Config.Instance;
    
    Console.WriteLine("Copying mod files to destination");
    Utils.RecursiveCopy(config.ModFilesPath, config.DestinationFreelancerPath);
    
}, resetOpt, generateConfigOpt, compileFrcFile);

var runCommand = new Command("run", "Run game or server");
runCommand.AddOption(launchServerOption);
runCommand.SetHandler(async (generateConfig, launchServer) =>
{
    if (generateConfig)
    {
        await CommandHandler.GenerateConfig();
    }
    
    await CommandHandler.LaunchFreelancer(launchServer);
}, generateConfigOpt, launchServerOption);

rootCommand.AddCommand(buildCommand);
rootCommand.AddCommand(runCommand);

if (!File.Exists("config.json"))
{
    Console.WriteLine("Could not find config.json");
    await CommandHandler.GenerateConfig();
}
else
{
    await using var jsonFile = File.OpenRead("config.json");
    Config.Instance = JsonSerializer.Deserialize<Config>(jsonFile, Config.Options)
                      ?? throw new InvalidOperationException("Unable to parse config file.");
}

var config = Config.Instance;
List<string> errors = [];

if (!Directory.Exists(config.SourceFreelancerPath))
{
    errors.Add("Source freelancer path was not found " + config.SourceFreelancerPath);
}

if (!File.Exists(config.PythonScriptPath))
{
    errors.Add("Python script path was not found" + config.PythonScriptPath);
}

if (!Directory.Exists(config.DestinationFreelancerPath))
{
    try
    {
        Directory.CreateDirectory(config.DestinationFreelancerPath);
    }
    catch (Exception e)
    {
        errors.Add($"Destination freelancer path was not found and could not be created: {e.Message}\n\n" + config.DestinationFreelancerPath);
    }
}

if (OperatingSystem.IsLinux())
{
    if (!Directory.Exists(config.WinePrefix))
    {
        errors.Add($"Wine Prefix ({config.WinePrefix}) was not found");
    }
}

if (errors.Count is not 0)
{
    Console.WriteLine("Cannot continue, errors found: ");
    foreach (var error in errors)
    {
        Console.WriteLine(error);
    }

    return 1;
}

return await rootCommand.InvokeAsync(args);