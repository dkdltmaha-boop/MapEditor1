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
            report.errors.Add("맵 데이터가 없습니다.");
            report.isValid = false;
            return report;
        }

        mapData.EnsureInitialized();

        if (mapData.width <= 0 || mapData.height <= 0)
        {
            report.errors.Add("맵 크기는 0보다 커야 합니다.");
        }

        if ((spawnPoints == null || spawnPoints.Count == 0) && (spawnX < 0 || spawnX >= mapData.width || spawnY < 0 || spawnY >= mapData.height))
        {
            report.errors.Add("시작 위치가 맵 바깥에 있습니다.");
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
            report.errors.Add("맵에 그려진 타일이 없습니다.");
        }

        if (report.wallTileCount == 0)
        {
            report.warnings.Add("벽 타일이 없습니다. PixelChroma에서 충돌이 적용되지 않을 수 있습니다.");
        }

        if (missingTilesets.Count > 0)
        {
            foreach (string missingTileset in missingTilesets)
            {
                report.errors.Add("타일셋 파일을 찾을 수 없습니다: " + missingTileset);
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
            report.errors.Add("시작 위치 " + (index + 1) + "이(가) 없습니다.");
            return;
        }

        if (spawnPoint.x < 0 || spawnPoint.x >= mapData.width || spawnPoint.y < 0 || spawnPoint.y >= mapData.height)
        {
            report.errors.Add("시작 위치 " + (index + 1) + "이(가) 맵 바깥에 있습니다.");
            return;
        }

        int wallTileId = mapData.GetTile(spawnPoint.x, spawnPoint.y, MapEditorLayerType.WallCollision);

        if (wallTileId == MapEditorManager.WallTileId)
        {
            report.errors.Add("시작 위치 " + (index + 1) + "이(가) 벽 타일 위에 있습니다.");
        }
        else if (mapData.GetTile(spawnPoint.x, spawnPoint.y, MapEditorLayerType.Ground) == -1)
        {
            report.warnings.Add("시작 위치 " + (index + 1) + "이(가) 빈 바닥 타일 위에 있습니다.");
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
            Debug.LogError("PixelChroma 맵 검사 오류: " + report.errors[i]);
        }

        for (int i = 0; i < report.warnings.Count; i++)
        {
            Debug.LogWarning("PixelChroma 맵 검사 경고: " + report.warnings[i]);
        }
    }
}
