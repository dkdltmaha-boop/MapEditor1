using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class MapEditorPixelChromaValidationService
{
    public static PixelChromaMapValidationReport Validate(MapData mapData, int spawnX, int spawnY)
    {
        return Validate(mapData, spawnX, spawnY, null);
    }

    public static PixelChromaMapValidationReport Validate(
        MapData mapData,
        int spawnX,
        int spawnY,
        IReadOnlyList<MapEditorSpawnPointData> spawnPoints)
    {
        PixelChromaMapValidationReport report = new PixelChromaMapValidationReport();

        if (mapData == null)
        {
            Fail(report, "맵 데이터가 없습니다.");
            return Finish(report);
        }

        mapData.EnsureInitialized();

        if (mapData.width <= 0
            || mapData.height <= 0
            || mapData.width > MapEditorManager.MaxMapSize
            || mapData.height > MapEditorManager.MaxMapSize)
        {
            Fail(report, "맵 크기가 허용 범위를 벗어났습니다.");
        }
        else
        {
            Pass(report, "맵 크기: " + mapData.width + "×" + mapData.height);
        }

        HashSet<string> usedTilesets = new HashSet<string>();
        HashSet<string> missingTilesets = new HashSet<string>();
        CountMapContents(mapData, report, usedTilesets, missingTilesets);
        report.tilesetCount = usedTilesets.Count;
        report.missingTilesetCount = missingTilesets.Count;

        if (report.paintedTileCount == 0)
        {
            Fail(report, "맵에 배치된 타일이 없습니다.");
        }

        if (report.groundTileCount == 0)
        {
            Fail(report, "플레이 가능한 바닥 타일이 없습니다.");
        }
        else
        {
            Pass(report, "바닥 타일: " + report.groundTileCount + "개");
        }

        if (report.wallTileCount == 0)
        {
            Warn(report, "Wall 속성이 없습니다. 플레이어가 맵 밖으로 이동할 수 있습니다.");
        }
        else
        {
            Pass(report, "Wall 속성: " + report.wallTileCount + "개");
        }

        if (report.objectTileCount == 0)
        {
            Warn(report, "오브젝트 레이어가 비어 있습니다.");
        }
        else
        {
            Pass(report, "오브젝트 타일: " + report.objectTileCount + "개");
        }

        if (missingTilesets.Count > 0)
        {
            foreach (string missingTileset in missingTilesets)
            {
                Fail(report, "원본 타일셋 파일을 찾을 수 없습니다: " + missingTileset);
            }
        }
        else
        {
            Pass(report, "사용된 이미지 파일을 모두 확인했습니다.");
        }

        List<MapEditorSpawnPointData> normalizedSpawns = NormalizeSpawnPoints(spawnPoints, spawnX, spawnY);
        report.spawnPointCount = normalizedSpawns.Count;
        ValidateSpawnPoints(mapData, normalizedSpawns, report);
        ValidateSpawnConnectivity(mapData, normalizedSpawns, report);

        float groundCoverage = mapData.width * mapData.height <= 0
            ? 0f
            : report.groundTileCount / (float)(mapData.width * mapData.height);
        if (groundCoverage < 0.15f)
        {
            Warn(report, "바닥 사용 면적이 전체 맵의 15%보다 작습니다.");
        }

        return Finish(report);
    }

    private static void CountMapContents(
        MapData mapData,
        PixelChromaMapValidationReport report,
        HashSet<string> usedTilesets,
        HashSet<string> missingTilesets)
    {
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

                    MapEditorLayerType countLayer = MapEditorLayerUtility.GetBaseLayer(layerType);

                    switch (countLayer)
                    {
                        case MapEditorLayerType.Ground:
                            report.groundTileCount++;
                            break;
                        case MapEditorLayerType.Object:
                        case MapEditorLayerType.WallVisual:
                            report.objectTileCount++;
                            break;
                        case MapEditorLayerType.WallCollision:
                            report.wallTileCount++;
                            break;
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
    }

    private static List<MapEditorSpawnPointData> NormalizeSpawnPoints(
        IReadOnlyList<MapEditorSpawnPointData> spawnPoints,
        int fallbackX,
        int fallbackY)
    {
        List<MapEditorSpawnPointData> result = new List<MapEditorSpawnPointData>();

        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (spawnPoints[i] != null)
                {
                    result.Add(spawnPoints[i]);
                }
            }
        }

        return result;
    }

    private static void ValidateSpawnPoints(
        MapData mapData,
        IReadOnlyList<MapEditorSpawnPointData> spawnPoints,
        PixelChromaMapValidationReport report)
    {
        if (spawnPoints.Count == 0)
        {
            Fail(report, "시작 위치가 없습니다. 시작 위치 도구로 최소 한 곳을 지정하세요.");
            return;
        }

        HashSet<Vector2Int> positions = new HashSet<Vector2Int>();

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            MapEditorSpawnPointData spawn = spawnPoints[i];
            string label = string.IsNullOrEmpty(spawn.id) ? "시작 위치 " + (i + 1) : spawn.id;

            if (!mapData.IsInside(spawn.x, spawn.y))
            {
                Fail(report, label + "가 맵 범위 밖에 있습니다.");
                continue;
            }

            Vector2Int position = new Vector2Int(spawn.x, spawn.y);
            if (!positions.Add(position))
            {
                Warn(report, label + "가 다른 시작 위치와 겹칩니다.");
            }

            if (IsWall(mapData, spawn.x, spawn.y))
            {
                Fail(report, label + "가 Wall 위에 있습니다.");
            }
            else if (!HasGround(mapData, spawn.x, spawn.y))
            {
                Fail(report, label + " 아래에 바닥 타일이 없습니다.");
            }
            else
            {
                Pass(report, label + " 배치가 정상입니다.");
            }
        }

        if (spawnPoints.Count < 2)
        {
            Warn(report, "멀티플레이 테스트를 위해 시작 위치를 2개 이상 권장합니다.");
        }
    }

    private static void ValidateSpawnConnectivity(
        MapData mapData,
        IReadOnlyList<MapEditorSpawnPointData> spawnPoints,
        PixelChromaMapValidationReport report)
    {
        if (spawnPoints.Count < 2 || !IsWalkable(mapData, spawnPoints[0].x, spawnPoints[0].y))
        {
            return;
        }

        bool[,] visited = new bool[mapData.width, mapData.height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Vector2Int start = new Vector2Int(spawnPoints[0].x, spawnPoints[0].y);
        visited[start.x, start.y] = true;
        queue.Enqueue(start);

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int next = current + directions[i];
                if (!mapData.IsInside(next.x, next.y)
                    || visited[next.x, next.y]
                    || !IsWalkable(mapData, next.x, next.y))
                {
                    continue;
                }

                visited[next.x, next.y] = true;
                queue.Enqueue(next);
            }
        }

        bool allReachable = true;
        for (int i = 1; i < spawnPoints.Count; i++)
        {
            MapEditorSpawnPointData spawn = spawnPoints[i];
            if (!mapData.IsInside(spawn.x, spawn.y) || !visited[spawn.x, spawn.y])
            {
                allReachable = false;
                Fail(report, "시작 위치 " + (i + 1) + "까지 이동 가능한 경로가 없습니다.");
            }
        }

        if (allReachable)
        {
            Pass(report, "모든 시작 위치가 서로 연결되어 있습니다.");
        }
    }

    private static bool IsWalkable(MapData mapData, int x, int y)
    {
        return mapData.IsInside(x, y) && HasGround(mapData, x, y) && !IsWall(mapData, x, y);
    }

    private static bool HasGround(MapData mapData, int x, int y)
    {
        return mapData.GetTile(x, y, MapEditorLayerType.Ground) != -1;
    }

    private static bool IsWall(MapData mapData, int x, int y)
    {
        return mapData.GetTile(x, y, MapEditorLayerType.WallCollision) == MapEditorManager.WallTileId;
    }

    private static void Pass(PixelChromaMapValidationReport report, string message)
    {
        report.passedChecks.Add(message);
    }

    private static void Warn(PixelChromaMapValidationReport report, string message)
    {
        report.warnings.Add(message);
    }

    private static void Fail(PixelChromaMapValidationReport report, string message)
    {
        report.errors.Add(message);
    }

    private static PixelChromaMapValidationReport Finish(PixelChromaMapValidationReport report)
    {
        report.isValid = report.errors.Count == 0;
        return report;
    }

    public static void Log(PixelChromaMapValidationReport report)
    {
        if (report == null)
        {
            return;
        }

        for (int i = 0; i < report.errors.Count; i++)
        {
            Debug.LogError("PixelChroma 맵 검사 실패: " + report.errors[i]);
        }

        for (int i = 0; i < report.warnings.Count; i++)
        {
            Debug.LogWarning("PixelChroma 맵 검사 경고: " + report.warnings[i]);
        }

        if (report.isValid)
        {
            Debug.Log("PixelChroma 맵 검사에 합격했습니다.");
        }
    }
}
