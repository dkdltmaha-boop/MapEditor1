using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class MapEditorMapSaveService
{
    private const string DefaultSaveFileName = "mapeditor_project.json";

    private readonly int maxMapSize;
    private string currentMapFilePath = string.Empty;
    private MapEditorTilesetDefinition[] importedTilesets = System.Array.Empty<MapEditorTilesetDefinition>();
    private MapEditorLayerSetting[] layerSettings = System.Array.Empty<MapEditorLayerSetting>();
    private RectInt? previewRegion;
    private RectInt[] previewRegions = System.Array.Empty<RectInt>();
    private MapEditorMovingRegionData[] movingRegions = System.Array.Empty<MapEditorMovingRegionData>();

    public MapEditorMapSaveService(int maxMapSize)
    {
        this.maxMapSize = maxMapSize;
    }

    public void SetImportedTilesets(MapEditorTilesetDefinition[] definitions)
    {
        importedTilesets = definitions ?? System.Array.Empty<MapEditorTilesetDefinition>();
    }

    public void SetPreviewRegion(RectInt? region)
    {
        previewRegion = region;
    }

    public void SetPreviewRegions(IReadOnlyList<RectInt> regions)
    {
        previewRegions = regions == null ? System.Array.Empty<RectInt>() : new RectInt[regions.Count];
        for (int i = 0; i < previewRegions.Length; i++) previewRegions[i] = regions[i];
        previewRegion = previewRegions.Length > 0 ? previewRegions[0] : (RectInt?)null;
    }

    public void SetLayerSettings(MapEditorLayerSetting[] settings)
    {
        if (settings == null)
        {
            layerSettings = System.Array.Empty<MapEditorLayerSetting>();
            return;
        }

        layerSettings = new MapEditorLayerSetting[settings.Length];

        for (int i = 0; i < settings.Length; i++)
        {
            layerSettings[i] = settings[i]?.Clone();
        }
    }

    public void SetMovingRegions(MapEditorMovingRegionData[] regions)
    {
        movingRegions = CloneMovingRegions(regions);
    }

    public bool Save(MapData mapData, string currentPngPalettePath)
    {
        return Save(mapData, currentPngPalettePath, 0, 0);
    }

    public bool Save(MapData mapData, string currentPngPalettePath, int spawnX, int spawnY)
    {
        return Save(mapData, currentPngPalettePath, spawnX, spawnY, (MapEditorSpawnPointData[])null);
    }

    public bool Save(MapData mapData, string currentPngPalettePath, int spawnX, int spawnY, MapEditorSpawnPointData[] spawnPoints)
    {
        if (string.IsNullOrEmpty(currentMapFilePath))
        {
            string path = MapEditorFileDialog.SaveFile(
                MapEditorLocalization.Choose("편집용 맵 저장", "Save Editable Map"),
                DefaultSaveFileName,
                "json");

            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            currentMapFilePath = path;
            MapEditorFileDialog.RememberDirectory(path);
        }

        return SaveToPath(mapData, currentPngPalettePath, spawnX, spawnY, spawnPoints, currentMapFilePath);
    }

    public bool Save(MapData mapData, string currentPngPalettePath, string fileName)
    {
        return Save(mapData, currentPngPalettePath, 0, 0, fileName);
    }

    public bool Save(MapData mapData, string currentPngPalettePath, int spawnX, int spawnY, string fileName)
    {
        return Save(mapData, currentPngPalettePath, spawnX, spawnY, null, fileName);
    }

    public bool Save(MapData mapData, string currentPngPalettePath, int spawnX, int spawnY, MapEditorSpawnPointData[] spawnPoints, string fileName)
    {
        return SaveToPath(mapData, currentPngPalettePath, spawnX, spawnY, spawnPoints, GetSavePath(fileName));
    }

    public bool Load(out MapSaveData saveData, out string path)
    {
        path = MapEditorFileDialog.OpenFile(
            MapEditorLocalization.Choose("편집용 맵 불러오기", "Load Editable Map"),
            "json");

        if (string.IsNullOrEmpty(path))
        {
            saveData = null;
            return false;
        }

        MapEditorFileDialog.RememberDirectory(path);
        return TryLoadFromPath(path, out saveData);
    }

    public bool Load(string fileName, out MapSaveData saveData, out string path)
    {
        path = GetSavePath(fileName);
        return TryLoadFromPath(path, out saveData);
    }

    private bool SaveToPath(MapData mapData, string currentPngPalettePath, int spawnX, int spawnY, MapEditorSpawnPointData[] spawnPoints, string path)
    {
        if (mapData == null || string.IsNullOrEmpty(path))
        {
            return false;
        }

        MapSaveData saveData = mapData.ToSaveData();
        saveData.currentPngPalettePath = currentPngPalettePath;
        saveData.importedTilesets = importedTilesets;
        saveData.layerSettings = layerSettings;
        saveData.spawnX = Mathf.Clamp(spawnX, 0, mapData.width - 1);
        saveData.spawnY = Mathf.Clamp(spawnY, 0, mapData.height - 1);
        saveData.spawnPoints = spawnPoints ?? System.Array.Empty<MapEditorSpawnPointData>();
        saveData.movingRegions = CloneMovingRegions(movingRegions);
        if (previewRegion.HasValue)
        {
            RectInt region = previewRegion.Value;
            saveData.previewX = region.x;
            saveData.previewY = region.y;
            saveData.previewWidth = region.width;
            saveData.previewHeight = region.height;
        }
        saveData.previewRegions = new MapEditorPreviewRegionData[previewRegions.Length];
        for (int i = 0; i < previewRegions.Length; i++)
        {
            saveData.previewRegions[i] = new MapEditorPreviewRegionData(previewRegions[i]);
        }
        EmbedUsedPngAssets(saveData);
        string json = JsonUtility.ToJson(saveData, true);
        string directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, json);
        currentMapFilePath = path;
        Debug.Log("맵을 저장했습니다: " + path);
        return true;
    }

    private static MapEditorMovingRegionData[] CloneMovingRegions(MapEditorMovingRegionData[] regions)
    {
        MapEditorMovingRegionData[] result = new MapEditorMovingRegionData[regions == null ? 0 : regions.Length];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = regions[i]?.Clone();
        }

        return result;
    }

    private bool TryLoadFromPath(string path, out MapSaveData saveData)
    {
        saveData = null;

        if (!File.Exists(path))
        {
            Debug.LogWarning("맵 파일이 없습니다: " + path);
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            if (json.Contains("\"format\"") && json.Contains("PixelChromaMap"))
            {
                Debug.LogWarning("PixelChroma용으로 내보낸 맵입니다. 편집 불러오기 대신 게임 맵 가져오기를 사용하세요: " + path);
                return false;
            }

            saveData = JsonUtility.FromJson<MapSaveData>(json);
            RestoreEmbeddedPngAssets(saveData);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("맵 파일을 읽을 수 없습니다: " + path + "\n" + exception.Message);
            return false;
        }

        if (!MapSaveDataValidator.IsValid(saveData, maxMapSize))
        {
            Debug.LogWarning("맵 파일을 읽을 수 없습니다: " + path);
            return false;
        }

        currentMapFilePath = path;
        return true;
    }

    private static void EmbedUsedPngAssets(MapSaveData saveData)
    {
        if (saveData == null || saveData.imagePaths == null)
        {
            return;
        }

        Dictionary<string, EmbeddedPngAsset> embeddedAssets = new Dictionary<string, EmbeddedPngAsset>();

        for (int i = 0; i < saveData.imagePaths.Length; i++)
        {
            TryEmbedPngAsset(saveData.imagePaths[i], embeddedAssets);
        }

        if (saveData.importedTilesets != null)
        {
            for (int i = 0; i < saveData.importedTilesets.Length; i++)
            {
                MapEditorTilesetDefinition definition = saveData.importedTilesets[i];
                if (definition != null)
                {
                    TryEmbedPngAsset(definition.atlasPath, embeddedAssets);
                }
            }
        }

        if (saveData.layerTiles != null)
        {
            for (int layerIndex = 0; layerIndex < saveData.layerTiles.Length; layerIndex++)
            {
                MapLayerTileData layer = saveData.layerTiles[layerIndex];

                if (layer == null || layer.imagePaths == null)
                {
                    continue;
                }

                for (int i = 0; i < layer.imagePaths.Length; i++)
                {
                    TryEmbedPngAsset(layer.imagePaths[i], embeddedAssets);
                }
            }
        }

        saveData.embeddedPngAssets = new List<EmbeddedPngAsset>(embeddedAssets.Values).ToArray();
    }

    private static void TryEmbedPngAsset(string path, Dictionary<string, EmbeddedPngAsset> embeddedAssets)
    {
        if (string.IsNullOrEmpty(path) || embeddedAssets.ContainsKey(path) || !File.Exists(path))
        {
            return;
        }

        try
        {
            embeddedAssets[path] = new EmbeddedPngAsset
            {
                originalPath = path,
                fileName = Path.GetFileName(path),
                base64Png = System.Convert.ToBase64String(File.ReadAllBytes(path))
            };
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("편집 저장 파일에 PNG를 포함하지 못했습니다: " + path + "\n" + exception.Message);
        }
    }

    private static void RestoreEmbeddedPngAssets(MapSaveData saveData)
    {
        if (saveData == null || saveData.embeddedPngAssets == null || saveData.embeddedPngAssets.Length == 0)
        {
            return;
        }

        string cacheDirectory = Path.Combine(Application.persistentDataPath, "MapEditorEmbeddedPng");
        Directory.CreateDirectory(cacheDirectory);
        Dictionary<string, string> restoredPaths = new Dictionary<string, string>();
        string firstRestoredPath = string.Empty;

        for (int i = 0; i < saveData.embeddedPngAssets.Length; i++)
        {
            EmbeddedPngAsset asset = saveData.embeddedPngAssets[i];

            if (asset == null || string.IsNullOrEmpty(asset.originalPath) || string.IsNullOrEmpty(asset.base64Png))
            {
                continue;
            }

            try
            {
                string fileName = string.IsNullOrEmpty(asset.fileName) ? "embedded_" + i + ".png" : asset.fileName;
                string restoredPath = Path.Combine(cacheDirectory, SanitizeFileName(fileName));
                File.WriteAllBytes(restoredPath, System.Convert.FromBase64String(asset.base64Png));
                restoredPaths[asset.originalPath] = restoredPath;

                if (string.IsNullOrEmpty(firstRestoredPath))
                {
                    firstRestoredPath = restoredPath;
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("포함된 PNG를 복원하지 못했습니다: " + asset.originalPath + "\n" + exception.Message);
            }
        }

        if (restoredPaths.Count == 0)
        {
            return;
        }

        if (saveData.imagePaths != null)
        {
            for (int i = 0; i < saveData.imagePaths.Length; i++)
            {
                string imagePath = saveData.imagePaths[i];

                if (!string.IsNullOrEmpty(imagePath) && restoredPaths.TryGetValue(imagePath, out string restoredPath))
                {
                    saveData.imagePaths[i] = restoredPath;
                    continue;
                }

                if (saveData.imageIndices != null
                    && i < saveData.imageIndices.Length
                    && saveData.imageIndices[i] >= 0
                    && !string.IsNullOrEmpty(firstRestoredPath))
                {
                    saveData.imagePaths[i] = firstRestoredPath;
                }
            }
        }

        if (saveData.layerTiles != null)
        {
            for (int layerIndex = 0; layerIndex < saveData.layerTiles.Length; layerIndex++)
            {
                MapLayerTileData layer = saveData.layerTiles[layerIndex];

                if (layer == null || layer.imagePaths == null || layer.imageIndices == null)
                {
                    continue;
                }

                for (int i = 0; i < layer.imagePaths.Length; i++)
                {
                    string imagePath = layer.imagePaths[i];

                    if (!string.IsNullOrEmpty(imagePath) && restoredPaths.TryGetValue(imagePath, out string restoredPath))
                    {
                        layer.imagePaths[i] = restoredPath;
                        continue;
                    }

                    if (i < layer.imageIndices.Length && layer.imageIndices[i] >= 0 && !string.IsNullOrEmpty(firstRestoredPath))
                    {
                        layer.imagePaths[i] = firstRestoredPath;
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(saveData.currentPngPalettePath) && restoredPaths.TryGetValue(saveData.currentPngPalettePath, out string restoredPalettePath))
        {
            saveData.currentPngPalettePath = restoredPalettePath;
        }
        else if (string.IsNullOrEmpty(saveData.currentPngPalettePath) || !File.Exists(saveData.currentPngPalettePath))
        {
            saveData.currentPngPalettePath = firstRestoredPath;
        }

        if (saveData.importedTilesets != null)
        {
            for (int i = 0; i < saveData.importedTilesets.Length; i++)
            {
                MapEditorTilesetDefinition definition = saveData.importedTilesets[i];
                if (definition != null
                    && !string.IsNullOrEmpty(definition.atlasPath)
                    && restoredPaths.TryGetValue(definition.atlasPath, out string restoredTilesetPath))
                {
                    definition.atlasPath = restoredTilesetPath;
                }
            }
        }

        BakeImageTilesIntoPixelData(saveData);
        Debug.Log("편집 불러오기용 PNG를 복원했습니다: " + restoredPaths.Count + "개");
    }

    private static void BakeImageTilesIntoPixelData(MapSaveData saveData)
    {
        if (saveData == null || saveData.imagePaths == null || saveData.imageIndices == null || saveData.tiles == null)
        {
            return;
        }

        MapEditorPngTilesetService tilesets = new MapEditorPngTilesetService();
        if (saveData.pixelData == null || saveData.pixelData.Length < saveData.tiles.Length)
        {
            saveData.pixelData = new MapTilePixelData[saveData.tiles.Length];
        }

        int count = Mathf.Min(saveData.tiles.Length, Mathf.Min(saveData.imagePaths.Length, saveData.imageIndices.Length));
        int bakedCount = 0;

        for (int i = 0; i < count; i++)
        {
            int tileId = saveData.tiles[i];

            if ((tileId != MapEditorManager.CustomImageTileId && tileId != MapEditorManager.WallTileId) || saveData.imageIndices[i] < 0)
            {
                continue;
            }

            if (IsAnimatedTile(saveData.importedTilesets, saveData.imagePaths[i], saveData.imageIndices[i]))
            {
                continue;
            }

            Sprite sprite = tilesets.GetTileSprite(
                saveData.imagePaths[i],
                saveData.imageIndices[i],
                saveData.imageRotations != null && i < saveData.imageRotations.Length ? saveData.imageRotations[i] : 0,
                saveData.imageFlipXs != null && i < saveData.imageFlipXs.Length && saveData.imageFlipXs[i],
                saveData.imageFlipYs != null && i < saveData.imageFlipYs.Length && saveData.imageFlipYs[i]
            );

            if (sprite == null)
            {
                continue;
            }

            saveData.pixelData[i] = MapTilePixelData.CreateFromSprite(sprite, MapEditorManager.MaxExportCellPixels);
            saveData.colors[i] = saveData.pixelData[i].GetAverageColor();
            saveData.imagePaths[i] = string.Empty;
            saveData.imageIndices[i] = -1;
            saveData.imageRotations[i] = 0;
            saveData.imageFlipXs[i] = false;
            saveData.imageFlipYs[i] = false;

            if (tileId == MapEditorManager.CustomImageTileId)
            {
                saveData.tiles[i] = MapEditorManager.CustomColorTileId;
            }

            bakedCount++;
        }

        if (bakedCount > 0)
        {
            Debug.Log("포함된 PNG 맵 타일을 편집 가능한 픽셀 데이터로 변환했습니다: " + bakedCount + "개");
        }
    }

    private static bool IsAnimatedTile(MapEditorTilesetDefinition[] definitions, string imagePath, int imageIndex)
    {
        if (definitions == null || string.IsNullOrEmpty(imagePath))
        {
            return false;
        }

        int tileId = MapEditorPngTilesetService.GetBaseImageIndex(imageIndex);
        for (int i = 0; i < definitions.Length; i++)
        {
            MapEditorTilesetDefinition definition = definitions[i];
            if (definition == null
                || !string.Equals(definition.atlasPath, imagePath, System.StringComparison.OrdinalIgnoreCase)
                || definition.animations == null)
            {
                continue;
            }

            for (int animationIndex = 0; animationIndex < definition.animations.Length; animationIndex++)
            {
                if (definition.animations[animationIndex] != null && definition.animations[animationIndex].ContainsTile(tileId))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalid, '_');
        }

        return fileName;
    }

    private string GetSavePath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }
}
