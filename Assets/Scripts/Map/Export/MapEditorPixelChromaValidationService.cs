using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class MapEditorPixelChromaValidationService
{
    public static PixelChromaMapValidationReport Validate(MapData mapData, int spawnX, int spawnY)
    {
        return Validate(mapData, spawnX, spawnY, null);
    }

    public static PixelChromaMapValidationReport Validate(MapData mapData, int spawnX, int spawnY, IReadOnlyList<MapEditorSpawnPointData> spawnPoints)
    {
        PixelChromaMapValidationReport report = new PixelChromaMapValidationReport();

        if (mapData == null)
        {
            report.errors.Add("Map data is missing.");
            report.isValid = false;
            return report;
        }

        mapData.EnsureInitialized();

        if (mapData.width <= 0 || mapData.height <= 0)
        {
            report.errors.Add("Map size must be greater than zero.");
        }

        if ((spawnPoints == null || spawnPoints.Count == 0) && (spawnX < 0 || spawnX >= mapData.width || spawnY < 0 || spawnY >= mapData.height))
        {
            report.errors.Add("Spawn point is outside the map.");
        }

        HashSet<string> usedTilesets = new HashSet<string>();
        HashSet<string> missingTilesets = new HashSet<string>();

        foreach (MapEditorLayerType layerType in MapData.GetSerializableLayers())
        {
            for (int y = 0; y < mapData.height; y++)
            {
                for (int x = 0; x < mapData.width; x++)
                {
                    int tileId = mapData.GetTile(x, y, layerType);

                    if (tileId == -1)
                    {
                        continue;
                    }

                    report.paintedTileCount++;

                    if (layerType == MapEditorLayerType.WallCollision || tileId == MapEditorManager.WallTileId)
                    {
                        report.wallTileCount++;
                    }
                    else if (layerType == MapEditorLayerType.Zone)
                    {
                        report.zoneCount++;
                    }
                    else
                    {
                        report.groundTileCount++;
                    }

                    string imagePath = mapData.GetImagePath(x, y, layerType);

                    if (string.IsNullOrEmpty(imagePath))
                    {
                        report.colorTileCount++;
                        continue;
                    }

                    report.imageTileCount++;
                    usedTilesets.Add(imagePath);

                    if (!File.Exists(imagePath))
                    {
                        missingTilesets.Add(imagePath);
                    }
                }
            }
        }

        report.tilesetCount = usedTilesets.Count;
        report.missingTilesetCount = missingTilesets.Count;
        report.spawnPointCount = spawnPoints == null || spawnPoints.Count == 0 ? 1 : spawnPoints.Count;

        if (report.paintedTileCount == 0)
        {
            report.errors.Add("Map has no painted tiles.");
        }

        if (report.wallTileCount == 0)
        {
            report.warnings.Add("Map has no wall tiles. PixelChroma collision may be missing.");
        }

        if (missingTilesets.Count > 0)
        {
            foreach (string missingTileset in missingTilesets)
            {
                report.errors.Add("Missing tileset file: " + missingTileset);
            }
        }

        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                ValidateSpawnPoint(mapData, spawnPoints[i], i, report);
            }
        }
        else
        {
            ValidateSpawnPoint(mapData, new MapEditorSpawnPointData("SpawnPoint_1", spawnX, spawnY, "Any"), 0, report);
        }

        report.isValid = report.errors.Count == 0;
        return report;
    }

    private static void ValidateSpawnPoint(MapData mapData, MapEditorSpawnPointData spawnPoint, int index, PixelChromaMapValidationReport report)
    {
        if (spawnPoint == null)
        {
            report.errors.Add("Spawn point " + (index + 1) + " is missing.");
            return;
        }

        if (spawnPoint.x < 0 || spawnPoint.x >= mapData.width || spawnPoint.y < 0 || spawnPoint.y >= mapData.height)
        {
            report.errors.Add("Spawn point " + (index + 1) + " is outside the map.");
            return;
        }

        int wallTileId = mapData.GetTile(spawnPoint.x, spawnPoint.y, MapEditorLayerType.WallCollision);

        if (wallTileId == MapEditorManager.WallTileId)
        {
            report.errors.Add("Spawn point " + (index + 1) + " is on a wall tile.");
        }
        else if (mapData.GetTile(spawnPoint.x, spawnPoint.y, MapEditorLayerType.Ground) == -1)
        {
            report.warnings.Add("Spawn point " + (index + 1) + " is on an empty floor tile.");
        }
    }

    public static void Log(PixelChromaMapValidationReport report)
    {
        if (report == null)
        {
            return;
        }

        for (int i = 0; i < report.errors.Count; i++)
        {
            Debug.LogError("PixelChroma map validation error: " + report.errors[i]);
        }

        for (int i = 0; i < report.warnings.Count; i++)
        {
            Debug.LogWarning("PixelChroma map validation warning: " + report.warnings[i]);
        }
    }
}
