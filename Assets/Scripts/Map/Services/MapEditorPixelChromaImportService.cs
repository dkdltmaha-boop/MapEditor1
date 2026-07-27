using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class MapEditorPixelChromaImportService
{
    public bool ImportWithDialog(out MapSaveData saveData, out string path)
    {
        saveData = null;
        path = string.Empty;

        path = MapEditorFileDialog.OpenFile("PixelChroma 맵 가져오기", "json");

        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        MapEditorFileDialog.RememberDirectory(path);
        return TryImport(path, out saveData);
    }

    public bool TryImport(string path, out MapSaveData saveData)
    {
        saveData = null;

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Debug.LogWarning("PixelChroma 맵 파일이 없습니다: " + path);
            return false;
        }

        PixelChromaMapExportData exportData;

        try
        {
            exportData = JsonUtility.FromJson<PixelChromaMapExportData>(File.ReadAllText(path));
        }
        catch (Exception exception)
        {
            Debug.LogWarning("PixelChroma 맵 파일을 읽을 수 없습니다: " + path + "\n" + exception.Message);
            return false;
        }

        if (!IsValid(exportData))
        {
            Debug.LogWarning("선택한 파일은 올바른 PixelChroma 맵 파일이 아닙니다: " + path);
            return false;
        }

        saveData = ConvertToSaveData(exportData, Path.GetDirectoryName(path));
        Debug.Log("PixelChroma 맵을 가져왔습니다: " + path);
        return true;
    }

    private static bool IsValid(PixelChromaMapExportData exportData)
    {
        return exportData != null
            && exportData.format == "PixelChromaMap"
            && exportData.width > 0
            && exportData.height > 0
            && exportData.layers != null;
    }

    private static MapSaveData ConvertToSaveData(PixelChromaMapExportData exportData, string sourceDirectory)
    {
        MapSaveData saveData = new MapSaveData(exportData.width, exportData.height)
        {
            formatVersion = 1,
            spawnX = 0,
            spawnY = 0,
            currentPngPalettePath = string.Empty
        };

        Dictionary<string, string> tilesetPaths = CreateTilesetPathLookup(exportData, sourceDirectory);

        if (exportData.spawnPoints != null && exportData.spawnPoints.Count > 0)
        {
            saveData.spawnX = Mathf.Clamp(exportData.spawnPoints[0].x, 0, exportData.width - 1);
            saveData.spawnY = Mathf.Clamp(exportData.spawnPoints[0].y, 0, exportData.height - 1);
            saveData.spawnPoints = new MapEditorSpawnPointData[exportData.spawnPoints.Count];

            for (int i = 0; i < exportData.spawnPoints.Count; i++)
            {
                PixelChromaSpawnPointExportData spawnPoint = exportData.spawnPoints[i];
                saveData.spawnPoints[i] = new MapEditorSpawnPointData(
                    string.IsNullOrEmpty(spawnPoint.id) ? "SpawnPoint_" + (i + 1) : spawnPoint.id,
                    Mathf.Clamp(spawnPoint.x, 0, exportData.width - 1),
                    Mathf.Clamp(spawnPoint.y, 0, exportData.height - 1),
                    spawnPoint.role
                );
            }
        }

        InitializeEmptyMap(saveData);

        for (int layerIndex = 0; layerIndex < exportData.layers.Count; layerIndex++)
        {
            PixelChromaMapLayerExportData layer = exportData.layers[layerIndex];

            if (layer == null || layer.tiles == null)
            {
                continue;
            }

            for (int tileIndex = 0; tileIndex < layer.tiles.Count; tileIndex++)
            {
                ApplyTile(saveData, layer, layer.tiles[tileIndex], tilesetPaths);
            }
        }

        return saveData;
    }

    private static Dictionary<string, string> CreateTilesetPathLookup(PixelChromaMapExportData exportData, string sourceDirectory)
    {
        Dictionary<string, string> paths = new Dictionary<string, string>();

        if (exportData.tilesets == null)
        {
            return paths;
        }

        for (int i = 0; i < exportData.tilesets.Count; i++)
        {
            PixelChromaTilesetExportData tileset = exportData.tilesets[i];

            if (tileset == null || string.IsNullOrEmpty(tileset.tilesetId))
            {
                continue;
            }

            string file = tileset.file;
            string fullPath = string.IsNullOrEmpty(sourceDirectory) || string.IsNullOrEmpty(file)
                ? string.Empty
                : Path.GetFullPath(Path.Combine(sourceDirectory, file.Replace('/', Path.DirectorySeparatorChar)));
            paths[tileset.tilesetId] = fullPath;
        }

        return paths;
    }

    private static void InitializeEmptyMap(MapSaveData saveData)
    {
        int count = saveData.width * saveData.height;
        MapEditorLayerType[] layers = MapData.GetSerializableLayers();
        saveData.layerTiles = new MapLayerTileData[layers.Length];

        for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
        {
            saveData.layerTiles[layerIndex] = new MapLayerTileData(saveData.width, saveData.height)
            {
                layer = (int)layers[layerIndex]
            };
        }

        for (int i = 0; i < count; i++)
        {
            saveData.tiles[i] = -1;
            saveData.colors[i] = Color.white;
            saveData.imagePaths[i] = string.Empty;
            saveData.imageIndices[i] = -1;
            saveData.imageRotations[i] = 0;
            saveData.imageFlipXs[i] = false;
            saveData.imageFlipYs[i] = false;
            saveData.pixelData[i] = null;
        }
    }

    private static void ApplyTile(MapSaveData saveData, PixelChromaMapLayerExportData layer, PixelChromaTileExportData tile, Dictionary<string, string> tilesetPaths)
    {
        if (tile == null || tile.x < 0 || tile.y < 0 || tile.x >= saveData.width || tile.y >= saveData.height)
        {
            return;
        }

        int index = tile.y * saveData.width + tile.x;
        bool collision = tile.collision || string.Equals(tile.kind, "wall", StringComparison.OrdinalIgnoreCase);
        bool hasImage = !string.IsNullOrEmpty(tile.tilesetId) && tile.tileId >= 0;
        MapEditorLayerType layerType = GetLayerType(layer, collision);

        saveData.tiles[index] = collision ? MapEditorManager.WallTileId : MapEditorManager.CustomColorTileId;
        saveData.colors[index] = ParseColor(tile.colorHex, Color.white);
        saveData.imageRotations[index] = tile.rotation;
        saveData.imageFlipXs[index] = tile.flipX;
        saveData.imageFlipYs[index] = tile.flipY;
        saveData.layers[index] = (int)layerType;

        if (hasImage)
        {
            saveData.tiles[index] = collision ? MapEditorManager.WallTileId : MapEditorManager.CustomImageTileId;
            saveData.imagePaths[index] = tilesetPaths.TryGetValue(tile.tilesetId, out string path) ? path : string.Empty;
            saveData.imageIndices[index] = tile.tileId;
            saveData.colors[index] = Color.white;
            ApplyTileToLayer(saveData, layerType, index);
            return;
        }

        saveData.imagePaths[index] = string.Empty;
        saveData.imageIndices[index] = -1;

        if (tile.pixelResolution > 0 && tile.pixelHexes != null && tile.pixelHexes.Length >= tile.pixelResolution * tile.pixelResolution)
        {
            saveData.pixelData[index] = CreatePixelData(tile);
        }

        ApplyTileToLayer(saveData, layerType, index);
    }

    private static void ApplyTileToLayer(MapSaveData saveData, MapEditorLayerType layerType, int flatIndex)
    {
        if (saveData.layerTiles == null)
        {
            return;
        }

        for (int i = 0; i < saveData.layerTiles.Length; i++)
        {
            MapLayerTileData layer = saveData.layerTiles[i];

            if (layer == null || layer.layer != (int)layerType || flatIndex < 0 || flatIndex >= layer.tiles.Length)
            {
                continue;
            }

            layer.tiles[flatIndex] = saveData.tiles[flatIndex];
            layer.colors[flatIndex] = saveData.colors[flatIndex];
            layer.imagePaths[flatIndex] = saveData.imagePaths[flatIndex];
            layer.imageIndices[flatIndex] = saveData.imageIndices[flatIndex];
            layer.imageRotations[flatIndex] = saveData.imageRotations[flatIndex];
            layer.imageFlipXs[flatIndex] = saveData.imageFlipXs[flatIndex];
            layer.imageFlipYs[flatIndex] = saveData.imageFlipYs[flatIndex];
            layer.pixelData[flatIndex] = saveData.pixelData[flatIndex] == null ? null : saveData.pixelData[flatIndex].Clone();
            return;
        }
    }

    private static MapEditorLayerType GetLayerType(PixelChromaMapLayerExportData layer, bool collision)
    {
        if (collision)
        {
            return MapEditorLayerType.WallCollision;
        }

        if (layer == null || string.IsNullOrEmpty(layer.kind))
        {
            return MapEditorLayerType.Ground;
        }

        if (string.Equals(layer.kind, "object", StringComparison.OrdinalIgnoreCase))
        {
            return MapEditorLayerType.Object;
        }

        if (string.Equals(layer.kind, "wall_visual", StringComparison.OrdinalIgnoreCase))
        {
            return MapEditorLayerType.WallVisual;
        }

        return MapEditorLayerType.Ground;
    }

    private static MapTilePixelData CreatePixelData(PixelChromaTileExportData tile)
    {
        int resolution = MapEditorManager.NormalizeExportCellPixels(tile.pixelResolution);
        MapTilePixelData pixelData = new MapTilePixelData
        {
            resolution = resolution,
            colors = new Color[resolution * resolution]
        };

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int index = y * resolution + x;
                pixelData.colors[index] = ParseColor(tile.pixelHexes[index], Color.white);
            }
        }

        return pixelData;
    }

    private static Color ParseColor(string hex, Color fallback)
    {
        if (string.IsNullOrEmpty(hex))
        {
            return fallback;
        }

        return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : fallback;
    }
}
