using System.Text.Json;

namespace FGC;

public class Config
{
    public static Config Instance { get; set; } = null!;

    public static JsonSerializerOptions Options { get; } = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    
    public required string SourceFreelancerPath { get; set; }
    public required string ModFilesPath { get; set; }
    public required string DestinationFreelancerPath { get; set; }
    public required string FlSpewPath { get; set; }
    public required string PythonScriptPath { get; set; }
    public string WinePrefix { get; set; } = string.Empty;
    public string WineBinary { get; set; } = "wine";
    public string[] WineDllOverrides { get; set; } = [];
    public required string[] GameArgs { get; set; } = [];
    public required string[] ServerArgs { get; set; } = [];
}