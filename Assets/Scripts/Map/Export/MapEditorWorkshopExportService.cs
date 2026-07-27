using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public sealed class MapEditorWorkshopExportService
{
    private const string ManifestFileName = "manifest.json";
    private const string MapFileName = "map.json";
    private const string PreviewFileName = "preview.png";
    private const string ReadmeFileName = "README.txt";
    private const string ReportFileName = "package_report.json";
    private const string SteamUploadFileName = "steam_upload.json";
    private const string TilesetFolderName = "tilesets";

    private readonly MapEditorPixelChromaExportService mapExportService = new MapEditorPixelChromaExportService();
    private readonly MapEditorMapExportService previewExportService;
    private RectInt? previewRegion;

    public MapEditorWorkshopExportService(Func<string, int, Sprite> getPngTileSprite)
    {
        previewExportService = new MapEditorMapExportService(getPngTileSprite);
    }

    public void SetPreviewRegion(RectInt? region)
    {
        previewRegion = region;
    }

    public bool ExportToPixelChromaProject(
        MapData mapData,
        string mapId,
        string title,
        string author,
        string description,
        string requiredGameVersion,
        string steamVisibility,
        string steamTags,
        int spawnX,
        int spawnY,
        MapEditorSpawnPointData[] spawnPoints,
        int cellSize,
        bool emptyCellsTransparent)
    {
        string projectPath = MapEditorPixelChromaProjectLocator.FindProjectPath();

        if (string.IsNullOrEmpty(projectPath))
        {
            Debug.LogWarning("PixelChroma 프로젝트를 찾지 못했습니다. Assets/WorkshopMaps 폴더를 직접 선택하세요.");
            return ExportWithDialog(
                mapData,
                mapId,
                title,
                author,
                description,
                requiredGameVersion,
                steamVisibility,
                steamTags,
                spawnX,
                spawnY,
                spawnPoints,
                cellSize,
                emptyCellsTransparent
            );
        }

        string normalizedMapId = SanitizeId(string.IsNullOrWhiteSpace(mapId) ? "map" : mapId);
        string packageFolder = Path.Combine(MapEditorPixelChromaProjectLocator.GetWorkshopRoot(projectPath), normalizedMapId);
        bool exported = Export(
            mapData,
            packageFolder,
            mapId,
            title,
            author,
            description,
            requiredGameVersion,
            steamVisibility,
            steamTags,
            spawnX,
            spawnY,
            spawnPoints,
            cellSize,
            emptyCellsTransparent
        );

        if (exported)
        {
            Debug.Log("창작마당 맵을 PixelChroma에 설치했습니다: " + packageFolder);
        }

        return exported;
    }

    public bool ExportWithDialog(
        MapData mapData,
        string mapId,
        string title,
        string author,
        string description,
        string requiredGameVersion,
        string steamVisibility,
        string steamTags,
        int spawnX,
        int spawnY,
        MapEditorSpawnPointData[] spawnPoints,
        int cellSize,
        bool emptyCellsTransparent)
    {
        string defaultFolder = SanitizeId(string.IsNullOrWhiteSpace(mapId) ? "workshop_map" : mapId);
        string folderPath = MapEditorFileDialog.SelectFolder("PixelChroma 창작마당 패키지 내보내기", defaultFolder);

        if (string.IsNullOrEmpty(folderPath))
        {
            return false;
        }

        MapEditorFileDialog.RememberDirectory(folderPath);
        return Export(
            mapData,
            folderPath,
            mapId,
            title,
            author,
            description,
            requiredGameVersion,
            steamVisibility,
            steamTags,
            spawnX,
            spawnY,
            spawnPoints,
            cellSize,
            emptyCellsTransparent
        );
    }

    public bool Export(
        MapData mapData,
        string folderPath,
        string mapId,
        string title,
        string author,
        string description,
        string requiredGameVersion,
        string steamVisibility,
        string steamTags,
        int spawnX,
        int spawnY,
        MapEditorSpawnPointData[] spawnPoints,
        int cellSize,
        bool emptyCellsTransparent)
    {
        if (mapData == null || string.IsNullOrEmpty(folderPath))
        {
            return false;
        }

        Directory.CreateDirectory(folderPath);

        string normalizedMapId = SanitizeId(string.IsNullOrWhiteSpace(mapId) ? "map" : mapId);
        PackagePaths paths = new PackagePaths(folderPath);
        PixelChromaWorkshopPackageReport report = BuildPackageReport(mapData, normalizedMapId, spawnX, spawnY, spawnPoints);

        if (report.errors.Count > 0)
        {
            WritePackageReport(paths.Report, report);
            Debug.LogError("PixelChroma 창작마당 패키지를 내보내지 못했습니다. package_report.json의 검사 오류를 확인하세요: " + paths.Report);
            return false;
        }

        if (!mapExportService.Export(mapData, paths.Map, normalizedMapId, cellSize, TilesetFolderName, spawnX, spawnY, spawnPoints))
        {
            WritePackageReport(paths.Report, report);
            return false;
        }

        Directory.CreateDirectory(paths.TilesetFolder);
        report.tilesetCount = 0;
        previewExportService.ExportPng(mapData, paths.Preview, cellSize, emptyCellsTransparent, previewRegion);
        WriteManifest(paths.Manifest, normalizedMapId, title, author, description, requiredGameVersion);
        WriteSteamUploadConfig(paths.SteamUpload, normalizedMapId, title, description, steamVisibility, steamTags);
        WriteReadme(paths.Readme, normalizedMapId);
        ValidateRequiredPackageFiles(paths, report);
        AddFileInventory(folderPath, report);
        WritePackageReport(paths.Report, report);

        Debug.Log("PixelChroma 창작마당 패키지를 내보냈습니다: " + folderPath);
        return true;
    }

    private static PixelChromaWorkshopPackageReport BuildPackageReport(MapData mapData, string mapId, int spawnX, int spawnY, IReadOnlyList<MapEditorSpawnPointData> spawnPoints)
    {
        mapData.EnsureInitialized();

        int clampedSpawnX = Mathf.Clamp(spawnX, 0, mapData.width - 1);
        int clampedSpawnY = Mathf.Clamp(spawnY, 0, mapData.height - 1);

        PixelChromaWorkshopPackageReport report = new PixelChromaWorkshopPackageReport
        {
            mapId = mapId,
            loaderStrategy = "RuntimeTilemapLoader",
            mapFormat = "PixelChromaMap",
            mapFormatVersion = 3,
            width = mapData.width,
            height = mapData.height,
            minX = 0,
            minY = 0,
            maxX = Mathf.Max(0, mapData.width - 1),
            maxY = Mathf.Max(0, mapData.height - 1),
            spawnX = clampedSpawnX,
            spawnY = clampedSpawnY
        };

        PixelChromaMapValidationReport validation = MapEditorPixelChromaValidationService.Validate(mapData, spawnX, spawnY, spawnPoints);
        report.paintedTileCount = validation.paintedTileCount;
        report.wallTileCount = validation.wallTileCount;
        report.colorTileCount = validation.colorTileCount;
        report.imageTileCount = validation.imageTileCount;
        report.tilesetCount = validation.tilesetCount;
        report.spawnPointCount = validation.spawnPointCount;
        report.zoneCount = validation.zoneCount;
        report.errors.AddRange(validation.errors);
        report.warnings.AddRange(validation.warnings);
        report.isValid = validation.isValid && report.paintedTileCount > 0;
        return report;
    }

    private static void WriteManifest(
        string path,
        string mapId,
        string title,
        string author,
        string description,
        string requiredGameVersion)
    {
        PixelChromaWorkshopManifest manifest = new PixelChromaWorkshopManifest
        {
            mapId = mapId,
            title = string.IsNullOrWhiteSpace(title) ? mapId : title,
            author = string.IsNullOrWhiteSpace(author) ? "Unknown" : author,
            description = description ?? string.Empty,
            mapFile = MapFileName,
            previewImage = PreviewFileName,
            tilesetFolder = TilesetFolderName,
            loaderStrategy = "RuntimeTilemapLoader",
            mapFormat = "PixelChromaMap",
            mapFormatVersion = 3,
            entryScene = "RuntimeWorkshopMap",
            requiredGameVersion = string.IsNullOrWhiteSpace(requiredGameVersion) ? "1.0.0" : requiredGameVersion
        };

        File.WriteAllText(path, JsonUtility.ToJson(manifest, true));
    }

    private static void WritePackageReport(string path, PixelChromaWorkshopPackageReport report)
    {
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
    }

    private static void ValidateRequiredPackageFiles(PackagePaths paths, PixelChromaWorkshopPackageReport report)
    {
        report.manifestExists = ValidateRequiredFile(paths.Manifest, ManifestFileName, report);
        report.mapFileExists = ValidateRequiredFile(paths.Map, MapFileName, report);
        report.previewExists = ValidateRequiredFile(paths.Preview, PreviewFileName, report);
        report.steamUploadConfigExists = ValidateRequiredFile(paths.SteamUpload, SteamUploadFileName, report);
        report.readmeExists = ValidateRequiredFile(paths.Readme, ReadmeFileName, report);
        report.tilesetFolderExists = Directory.Exists(paths.TilesetFolder);

        if (!report.tilesetFolderExists)
        {
            report.errors.Add("Required folder is missing: " + TilesetFolderName);
        }

        ValidateFileContains(paths.Manifest, ManifestFileName, "\"format\": \"PixelChromaWorkshopMap\"", report);
        ValidateFileContains(paths.Manifest, ManifestFileName, "\"mapFile\": \"" + MapFileName + "\"", report);
        ValidateFileContains(paths.Manifest, ManifestFileName, "\"tilesetFolder\": \"" + TilesetFolderName + "\"", report);
        ValidateFileContains(paths.Map, MapFileName, "\"format\": \"PixelChromaMap\"", report);
        ValidateFileContains(paths.Map, MapFileName, "\"loaderStrategy\": \"RuntimeTilemapLoader\"", report);

        report.isValid = report.errors.Count == 0 && report.warnings.Count == 0 && report.paintedTileCount > 0;
    }

    private static bool ValidateRequiredFile(string path, string packagePath, PixelChromaWorkshopPackageReport report)
    {
        if (!File.Exists(path))
        {
            report.errors.Add("Required file is missing: " + packagePath);
            return false;
        }

        FileInfo fileInfo = new FileInfo(path);

        if (fileInfo.Length == 0)
        {
            report.errors.Add("Required file is empty: " + packagePath);
            return false;
        }

        return true;
    }

    private static void ValidateFileContains(string path, string packagePath, string expectedText, PixelChromaWorkshopPackageReport report)
    {
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            string text = File.ReadAllText(path);

            if (!text.Contains(expectedText))
            {
                report.errors.Add("Required file does not contain expected data: " + packagePath + " missing " + expectedText);
            }
        }
        catch (Exception exception)
        {
            report.errors.Add("Could not validate required file: " + packagePath + " (" + exception.Message + ")");
        }
    }

    private static void AddFileInventory(string folderPath, PixelChromaWorkshopPackageReport report)
    {
        report.files.Clear();

        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            report.warnings.Add("Package folder does not exist for file inventory.");
            report.isValid = false;
            return;
        }

        string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

        for (int i = 0; i < files.Length; i++)
        {
            string filePath = files[i];
            string fileName = Path.GetFileName(filePath);

            if (string.Equals(fileName, ReportFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                FileInfo fileInfo = new FileInfo(filePath);

                report.files.Add(new PixelChromaWorkshopPackageFileReport
                {
                    path = GetRelativePath(folderPath, filePath),
                    bytes = fileInfo.Length,
                    sha256 = ComputeSha256(filePath)
                });
            }
            catch (Exception exception)
            {
                string warning = "Could not inventory package file: " + filePath;
                report.warnings.Add(warning);
                Debug.LogWarning(warning + "\n" + exception.Message);
            }
        }

        report.isValid = report.errors.Count == 0 && report.warnings.Count == 0 && report.paintedTileCount > 0;
    }

    private static void WriteSteamUploadConfig(
        string path,
        string mapId,
        string title,
        string description,
        string visibility,
        string tags)
    {
        PixelChromaSteamWorkshopUploadConfig config = new PixelChromaSteamWorkshopUploadConfig
        {
            mapId = mapId,
            title = string.IsNullOrWhiteSpace(title) ? mapId : title,
            description = description ?? string.Empty,
            visibility = NormalizeVisibility(visibility),
            tags = ParseTags(tags)
        };

        File.WriteAllText(path, JsonUtility.ToJson(config, true));
    }

    private static void WriteReadme(string path, string mapId)
    {
        string text =
            "PixelChroma Workshop Map Package\n" +
            "\n" +
            "Map ID: " + mapId + "\n" +
            "\n" +
            "Files:\n" +
            "- manifest.json: Workshop metadata used by PixelChroma and upload tools.\n" +
            "- map.json: Runtime map data exported from MapEditor.\n" +
            "- preview.png: Preview image for menus and Steam Workshop.\n" +
            "- tilesets/: PNG tilesets used by this map.\n" +
            "- package_report.json: Export validation summary.\n" +
            "- steam_upload.json: Suggested metadata for a Steam Workshop uploader.\n" +
            "\n" +
            "Runtime loading contract:\n" +
            "1. Read manifest.json first.\n" +
            "2. Open manifest.mapFile, usually map.json.\n" +
            "3. Create a root GameObject named WorkshopMap and a child Grid.\n" +
            "4. Use map.runtime.layerBindings to create Tilemaps and Colliders.\n" +
            "5. Load PNG files from manifest.tilesetFolder when map.tilesets references them.\n" +
            "6. Create spawn objects from map.spawnPoints.\n" +
            "7. Create optional zone metadata objects from map.zones.\n" +
            "\n" +
            "Do not rename files inside this folder unless the manifest and map data are updated together.\n";

        File.WriteAllText(path, text);
    }

    private static string NormalizeVisibility(string visibility)
    {
        if (string.IsNullOrWhiteSpace(visibility))
        {
            return "Public";
        }

        string value = visibility.Trim();

        if (string.Equals(value, "Private", StringComparison.OrdinalIgnoreCase))
        {
            return "Private";
        }

        if (string.Equals(value, "FriendsOnly", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "Friends Only", StringComparison.OrdinalIgnoreCase))
        {
            return "FriendsOnly";
        }

        if (string.Equals(value, "Unlisted", StringComparison.OrdinalIgnoreCase))
        {
            return "Unlisted";
        }

        return "Public";
    }

    private static string[] ParseTags(string tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return new[] { "Map" };
        }

        string[] rawTags = tags.Split(',');
        List<string> parsedTags = new List<string>();

        for (int i = 0; i < rawTags.Length; i++)
        {
            string tag = rawTags[i].Trim();

            if (!string.IsNullOrEmpty(tag) && !parsedTags.Contains(tag))
            {
                parsedTags.Add(tag);
            }
        }

        return parsedTags.Count == 0 ? new[] { "Map" } : parsedTags.ToArray();
    }

    private static string ComputeSha256(string filePath)
    {
        using (FileStream stream = File.OpenRead(filePath))
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(stream);
            StringBuilder builder = new StringBuilder(hash.Length * 2);

            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }

    private static string GetRelativePath(string rootPath, string filePath)
    {
        Uri rootUri = new Uri(AppendDirectorySeparatorChar(Path.GetFullPath(rootPath)));
        Uri fileUri = new Uri(Path.GetFullPath(filePath));
        return Uri.UnescapeDataString(rootUri.MakeRelativeUri(fileUri).ToString()).Replace('\\', '/');
    }

    private static string AppendDirectorySeparatorChar(string path)
    {
        if (string.IsNullOrEmpty(path) || path.EndsWith(Path.DirectorySeparatorChar.ToString()) || path.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
        {
            return path;
        }

        return path + Path.DirectorySeparatorChar;
    }

    private static string SanitizeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "map";
        }

        StringBuilder builder = new StringBuilder(value.Length);

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];

            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
            else if (c == '_' || c == '-' || c == ' ')
            {
                builder.Append('_');
            }
        }

        string sanitized = builder.ToString().Trim('_');
        return string.IsNullOrEmpty(sanitized) ? "map" : sanitized;
    }

    private sealed class PackagePaths
    {
        public readonly string Manifest;
        public readonly string Map;
        public readonly string Preview;
        public readonly string Readme;
        public readonly string Report;
        public readonly string SteamUpload;
        public readonly string TilesetFolder;

        public PackagePaths(string root)
        {
            Manifest = Path.Combine(root, ManifestFileName);
            Map = Path.Combine(root, MapFileName);
            Preview = Path.Combine(root, PreviewFileName);
            Readme = Path.Combine(root, ReadmeFileName);
            Report = Path.Combine(root, ReportFileName);
            SteamUpload = Path.Combine(root, SteamUploadFileName);
            TilesetFolder = Path.Combine(root, TilesetFolderName);
        }
    }
}
