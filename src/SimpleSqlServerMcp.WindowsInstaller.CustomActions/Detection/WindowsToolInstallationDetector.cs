using Microsoft.Win32;
using System.Diagnostics;

namespace SimpleSqlServerMcp.WindowsInstaller.CustomActions;

internal sealed class WindowsToolInstallationDetector
{
    private static readonly string[] UninstallRoots =
    [
        @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
        @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    ];

    public bool IsDetected(DetectedToolProperty tool)
    {
        return IsDetectedFromUninstallRegistry(tool)
            || IsDetectedFromAppxPackage(tool)
            || IsDetectedFromKnownPaths(tool)
            || IsDetectedFromCommandPath(tool);
    }

    private static bool IsDetectedFromAppxPackage(DetectedToolProperty tool)
    {
        foreach (var packageName in tool.AppxPackageNames)
        {
            if (IsAppxPackageInstalled(packageName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDetectedFromUninstallRegistry(DetectedToolProperty tool)
    {
        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            foreach (var root in UninstallRoots)
            {
                using var uninstallRoot = hive.OpenSubKey(root);
                if (uninstallRoot is null)
                {
                    continue;
                }

                foreach (var subKeyName in uninstallRoot.GetSubKeyNames())
                {
                    using var subKey = uninstallRoot.OpenSubKey(subKeyName);
                    if (subKey is null)
                    {
                        continue;
                    }

                    var displayName = subKey.GetValue("DisplayName") as string;
                    if (!MatchesAnyMarker(displayName, tool.DisplayNameMarkers))
                    {
                        continue;
                    }

                    var installLocation = subKey.GetValue("InstallLocation") as string;
                    if (!string.IsNullOrWhiteSpace(installLocation) && Directory.Exists(TrimQuotes(installLocation!)))
                    {
                        return true;
                    }

                    var displayIcon = subKey.GetValue("DisplayIcon") as string;
                    if (!string.IsNullOrWhiteSpace(displayIcon))
                    {
                        var displayIconPath = TrimExecutableArgument(displayIcon!);
                        if (File.Exists(displayIconPath))
                        {
                            return true;
                        }
                    }

                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsDetectedFromKnownPaths(DetectedToolProperty tool)
        => tool.KnownExecutablePaths.Any(File.Exists) || tool.KnownConfigPaths.Any(path => File.Exists(path) || Directory.Exists(path));

    private static bool IsDetectedFromCommandPath(DetectedToolProperty tool)
    {
        foreach (var commandName in tool.CommandNames)
        {
            var resolved = ResolveCommandFromPath(commandName);
            if (!string.IsNullOrWhiteSpace(resolved) && File.Exists(resolved))
            {
                return true;
            }
        }

        return false;
    }

    private static string? ResolveCommandFromPath(string commandName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        var pathExtensions = SplitAndTrim(Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT");

        foreach (var directory in SplitAndTrim(pathValue))
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            var directPath = Path.Combine(directory, commandName);
            if (File.Exists(directPath))
            {
                return directPath;
            }

            foreach (var extension in pathExtensions)
            {
                var candidate = Path.Combine(directory, commandName + extension.ToLowerInvariant());
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static bool MatchesAnyMarker(string? input, IEnumerable<string> markers)
        => !string.IsNullOrWhiteSpace(input)
           && markers.Any(marker => input!.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0);

    private static IEnumerable<string> SplitAndTrim(string value)
        => value
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .Where(part => part.Length > 0);

    private static string TrimQuotes(string value)
        => value.Trim().Trim('"');

    private static string TrimExecutableArgument(string value)
    {
        var trimmed = TrimQuotes(value);
        var commaIndex = trimmed.IndexOf(',');
        if (commaIndex >= 0)
        {
            return trimmed.Substring(0, commaIndex);
        }

        return trimmed;
    }

    private static bool IsAppxPackageInstalled(string packageName)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"(Get-AppxPackage -Name '{packageName}' | Select-Object -First 1 -ExpandProperty PackageFamilyName)\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);

            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
        }
        catch
        {
            return false;
        }
    }
}
