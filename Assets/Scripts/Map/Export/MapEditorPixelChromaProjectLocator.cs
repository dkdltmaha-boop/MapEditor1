using System;
using System.IO;

public static class MapEditorPixelChromaProjectLocator
{
    private const string ProjectFolderName = "PixelChroma";
    private const string RepositoryFolderName = "network-team-project-july-7-3";

    public static string FindProjectPath()
    {
        string configuredPath = Environment.GetEnvironmentVariable("PIXELCHROMA_PROJECT_PATH");

        if (IsProject(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string knownProjectPath = Path.Combine(userProfile, RepositoryFolderName, ProjectFolderName);

        if (IsProject(knownProjectPath))
        {
            return Path.GetFullPath(knownProjectPath);
        }

        DirectoryInfo current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current != null)
        {
            string directChild = Path.Combine(current.FullName, ProjectFolderName);

            if (IsProject(directChild))
            {
                return Path.GetFullPath(directChild);
            }

            current = current.Parent;
        }

        return string.Empty;
    }

    public static string GetWorkshopRoot(string projectPath)
    {
        return string.IsNullOrWhiteSpace(projectPath)
            ? string.Empty
            : Path.Combine(projectPath, "Assets", "WorkshopMaps");
    }

    private static bool IsProject(string projectPath)
    {
        return !string.IsNullOrWhiteSpace(projectPath)
            && Directory.Exists(Path.Combine(projectPath, "Assets"))
            && File.Exists(Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt"));
    }
}
