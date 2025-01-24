using System.Diagnostics;

namespace FGC;

public static class Utils
{
    public static bool IsSymbolicLink(this string file)
    {
        try
        {
            var pathInfo = new FileInfo(file);
            return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch
        {
            return false;
        }
    }

    public static void RecursiveCopy(string sourceDir, string destDir)
    {
        var cursor = Console.GetCursorPosition();
        using var spinner = new Spinner(cursor.Left, cursor.Top);
        spinner.Start();
        foreach (var path in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            if (path.IsSymbolicLink())
            {
                continue;
            }

            var newPath = path.Replace(sourceDir, destDir);
            Directory.CreateDirectory(Path.GetDirectoryName(newPath) ??
                                      throw new InvalidOperationException("Unable to create directory: " + newPath));
            File.Copy(path, newPath, true);

            if (OperatingSystem.IsLinux())
            {
                var fileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                if (newPath.EndsWith(".exe"))
                {
                    fileMode |= UnixFileMode.UserExecute;
                }

                File.SetUnixFileMode(newPath, fileMode);
            }
        }
    }

    public static async Task RunProcess(string path, string workingDirectory, string[] args)
    {
        var config = Config.Instance;
        
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var process = Process.Start(new ProcessStartInfo(path)
                {
                    Arguments = string.Join(' ', args),
                    RedirectStandardOutput = true,
                    WorkingDirectory = workingDirectory
                });

                process!.OutputDataReceived += (_, output) => Console.WriteLine("Freelancer STDOUT: {0}", output.Data);
                process!.ErrorDataReceived += (_, output) => Console.WriteLine("Freelancer ERROR: {0}", output.Data);

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();
            }
            else
            {
                var strArgs = string.Join(' ', args.Prepend(path));
                if (!File.Exists(config.WineBinary))
                {
                    strArgs = config.WineBinary + " " + strArgs;
                    config.WineBinary = "/usr/bin/env";
                }

                var process = Process.Start(new ProcessStartInfo(config.WineBinary)
                {
                    Arguments = strArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workingDirectory,
                    EnvironmentVariables =
                    {
                        ["WINEPREFIX"] = config.WinePrefix,
                        ["WINEDLLOVERRIDES"] = $"{string.Join(',', config.WineDllOverrides)}=n,b"
                    }
                });

                process!.OutputDataReceived += (sender, args) => Console.WriteLine("STDOUT: {0}", args.Data);
                process!.ErrorDataReceived += (sender, args) => Console.WriteLine("ERROR: {0}", args.Data);

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to start {path}!");
            Console.WriteLine(ex.Message);
        }
    }
}