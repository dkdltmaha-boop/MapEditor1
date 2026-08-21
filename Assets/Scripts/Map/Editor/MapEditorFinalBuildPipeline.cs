#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class MapEditorFinalBuildPipeline
{
    private const string BuildRootFolder = "Builds";
    private const string FinalBuildFolder = "Windows";
    private const string StagingBuildFolder = "Windows_Staging";

    [MenuItem("Tools/MapEditor/Run Regression Tests And Build Windows")]
    public static void RunFromMenu()
    {
        RunBatchMode();
    }

    public static void RunBatchMode()
    {
        RunRegressionTests();
        BuildWindowsPlayer();
    }

    [MenuItem("Tools/MapEditor/Run Safe Windows Build Test")]
    public static void RunBuildTestBatchMode()
    {
        RunRegressionTests();
        BuildWindowsTestPlayer();
    }

    public static void RunPlayerBuildTestOnly()
    {
        BuildWindowsTestPlayer();
    }

    private static void RunRegressionTests()
    {
        MapEditorExportSmokeTest.RunBatchMode();
        MapEditorTilesetImportSmokeTest.Run();
        MapEditorSizeConfigurationSmokeTest.Run();
        Debug.Log("MapEditor final regression suite passed.");
    }

    private static void BuildWindowsPlayer()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string buildRoot = GetChildPath(projectRoot, BuildRootFolder);
        string finalBuildPath = GetChildPath(buildRoot, FinalBuildFolder);
        string stagingBuildPath = GetChildPath(buildRoot, StagingBuildFolder);

        Directory.CreateDirectory(buildRoot);
        DeleteDirectory(stagingBuildPath);
        Directory.CreateDirectory(stagingBuildPath);

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes are configured in Build Settings.");
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(stagingBuildPath, "MapEditor.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            DeleteDirectory(stagingBuildPath);
            throw new InvalidOperationException(
                $"Windows build failed: {report.summary.result}, {report.summary.totalErrors} error(s).");
        }

        CopySteamAppId(projectRoot, stagingBuildPath);
        DeleteDirectory(finalBuildPath);
        Directory.Move(stagingBuildPath, finalBuildPath);
        Debug.Log($"MapEditor final Windows build completed: {finalBuildPath}");
    }

    private static void BuildWindowsTestPlayer()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string buildRoot = GetChildPath(projectRoot, BuildRootFolder);
        string testBuildPath = GetChildPath(buildRoot, "Windows_Test");

        Directory.CreateDirectory(buildRoot);
        DeleteDirectory(testBuildPath);
        Directory.CreateDirectory(testBuildPath);

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
            .Select(scene => scene.path)
            .ToArray();
        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes are configured in Build Settings.");
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(testBuildPath, "MapEditor.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Windows test build failed: {report.summary.result}, {report.summary.totalErrors} error(s).");
        }

        CopySteamAppId(projectRoot, testBuildPath);
        Debug.Log($"MapEditor Windows test build completed: {testBuildPath}");
    }

    private static void CopySteamAppId(string projectRoot, string buildPath)
    {
        string sourcePath = Path.Combine(projectRoot, "steam_appid.txt");
        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning("steam_appid.txt was not copied because it is missing from the project root.");
            return;
        }

        string appId = File.ReadAllText(sourcePath).Trim();
        if (string.IsNullOrWhiteSpace(appId))
        {
            throw new InvalidOperationException("steam_appid.txt is empty.");
        }

        File.WriteAllText(Path.Combine(buildPath, "steam_appid.txt"), appId);
    }

    private static string GetChildPath(string parent, string childName)
    {
        string parentPath = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar);
        string childPath = Path.GetFullPath(Path.Combine(parentPath, childName));
        if (!childPath.StartsWith(parentPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Build output escaped the project directory: " + childPath);
        }

        return childPath;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }
}
#endif
