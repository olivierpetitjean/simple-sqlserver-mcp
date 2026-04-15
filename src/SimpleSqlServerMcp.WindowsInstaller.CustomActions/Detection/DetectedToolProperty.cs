namespace SimpleSqlServerMcp.WindowsInstaller.CustomActions;

internal sealed class DetectedToolProperty
{
    public DetectedToolProperty(
        string detectionKey,
        string installProperty,
        string[] displayNameMarkers,
        string[] knownExecutablePaths,
        string[] knownConfigPaths,
        string[] commandNames,
        string[] registryFileMarkers,
        string[] appxPackageNames)
    {
        DetectionKey = detectionKey;
        InstallProperty = installProperty;
        DisplayNameMarkers = displayNameMarkers;
        KnownExecutablePaths = knownExecutablePaths;
        KnownConfigPaths = knownConfigPaths;
        CommandNames = commandNames;
        RegistryFileMarkers = registryFileMarkers;
        AppxPackageNames = appxPackageNames;
    }

    public string DetectionKey { get; }

    public string InstallProperty { get; }

    public string[] DisplayNameMarkers { get; }

    public string[] KnownExecutablePaths { get; }

    public string[] KnownConfigPaths { get; }

    public string[] CommandNames { get; }

    public string[] RegistryFileMarkers { get; }

    public string[] AppxPackageNames { get; }
}
