using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class MapEditorPixelChromaValidationService
{
    private const int MinimumPlayableDimension = 32;
    private const int MinimumWalkableTileCount = 64;
    private const float MinimumGroundCoverage = 0.05f;
    private const float RecommendedGroundCoverage = 0.15f;
    private const int RecommendedMaximumTilesets = 16;
    private const int RecommendedMaximumAnimatedTiles = 256;
    private const int MinimumPreviewDimension = 4;

    private static readonly MapEditorPngTilesetService PngTilesets = new MapEditorPngTilesetService();
    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

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
            Fail(report, L("맵 데이터가 없습니다.", "Map data is missing."));
            return Finish(report);
        }

        mapData.EnsureInitialized();

        if (mapData.width <= 0
            || mapData.height <= 0
            || mapData.width > MapEditorManager.MaxMapSize
            || mapData.height > MapEditorManager.MaxMapSize)
        {
            Fail(report, L("맵 크기가 허용 범위를 벗어났습니다.", "The map size is outside the allowed range."));
        }
        else
        {
            Pass(report, L("맵 크기: ", "Map size: ") + mapData.width + "×" + mapData.height);

            if (mapData.width < MinimumPlayableDimension || mapData.height < MinimumPlayableDimension)
            {
                Fail(report, L("플레이 맵은 가로와 세로가 각각 최소 ", "A playable map must be at least ") + MinimumPlayableDimension + L("칸이어야 합니다. 현재 ", " cells wide and high. Current size: ") + mapData.width + "×" + mapData.height + L("입니다.", "."));
            }
        }

        HashSet<string> usedTilesets = new HashSet<string>();
        HashSet<string> missingTilesets = new HashSet<string>();
        HashSet<string> validatedAnimations = new HashSet<string>();
        HashSet<string> validatedImageTiles = new HashSet<string>();
        CountMapContents(
            mapData,
            report,
            usedTilesets,
            missingTilesets,
            validatedAnimations,
            validatedImageTiles);
        report.tilesetCount = usedTilesets.Count;
        report.missingTilesetCount = missingTilesets.Count;

        if (report.animatedTileCount > 0 && report.invalidAnimationCount == 0)
        {
            Pass(report, L("애니메이션 타일: ", "Animated tiles: ") + report.animatedTileCount + L("개 / 정의 ", " / definitions ") + report.animationDefinitionCount);
        }

        if (report.paintedTileCount == 0)
        {
            Fail(report, L("맵에 배치된 타일이 없습니다.", "No tiles are placed on the map."));
        }

        if (report.groundTileCount == 0)
        {
            Fail(report, L("플레이 가능한 바닥 타일이 없습니다.", "There are no playable ground tiles."));
        }
        else
        {
            Pass(report, L("바닥 타일: ", "Ground tiles: ") + report.groundTileCount);
        }

        if (report.wallTileCount == 0)
        {
            Fail(report, L("충돌벽이 없습니다. PixelChroma 스테이지처럼 이동 영역을 막는 Wall/Collision 타일을 배치하세요.", "No collision walls are present. Place Wall/Collision tiles around the playable area."));
        }
        else
        {
            Pass(report, L("충돌벽: ", "Collision walls: ") + report.wallTileCount);
        }

        if (report.objectTileCount == 0)
        {
            Warn(report, L("오브젝트 레이어가 비어 있습니다.", "The Object layer is empty."));
        }
        else
        {
            Pass(report, L("오브젝트 타일: ", "Object tiles: ") + report.objectTileCount);
        }

        if (missingTilesets.Count > 0)
        {
            foreach (string missingTileset in missingTilesets)
            {
                Fail(report, L("원본 타일셋 파일을 찾을 수 없습니다: ", "Tileset source file not found: ") + missingTileset);
            }
        }
        else
        {
            Pass(report, L("사용된 이미지 파일을 모두 확인했습니다.", "All used image files were verified."));
        }

        if (report.tilesetCount > RecommendedMaximumTilesets)
        {
            Warn(report, L("타일셋을 ", "The map uses ") + report.tilesetCount + L("개 사용했습니다. 로딩 부담을 줄이려면 ", " tilesets. For faster loading, use no more than ") + RecommendedMaximumTilesets + L("개 이하를 권장합니다.", "."));
        }

        if (report.animatedTileCount > RecommendedMaximumAnimatedTiles)
        {
            Warn(report, L("애니메이션 타일이 ", "There are ") + report.animatedTileCount + L("개입니다. 원활한 플레이를 위해 ", " animated tiles. For smooth play, use no more than ") + RecommendedMaximumAnimatedTiles + L("개 이하를 권장합니다.", "."));
        }

        AnalyzePlayableArea(mapData, report);

        List<MapEditorSpawnPointData> normalizedSpawns = NormalizeSpawnPoints(spawnPoints, spawnX, spawnY);
        report.spawnPointCount = normalizedSpawns.Count;
        ValidateSpawnPoints(mapData, normalizedSpawns, report);
        ValidatePlayableConnectivity(mapData, normalizedSpawns, report);

        float groundCoverage = mapData.width * mapData.height <= 0
            ? 0f
            : report.groundCellCount / (float)(mapData.width * mapData.height);
        if (groundCoverage < MinimumGroundCoverage)
        {
            Fail(report, L("실제 바닥 면적이 전체 맵의 5%보다 작습니다. 맵 크기를 줄이거나 플레이 영역을 넓히세요.", "Ground covers less than 5% of the map. Reduce the map size or expand the playable area."));
        }
        else if (groundCoverage < RecommendedGroundCoverage)
        {
            Warn(report, L("바닥 사용 면적이 전체 맵의 15%보다 작습니다.", "Ground covers less than 15% of the map."));
        }
        else
        {
            Pass(report, L("바닥 사용 면적: ", "Ground coverage: ") + Mathf.RoundToInt(groundCoverage * 100f) + "%");
        }

        return Finish(report);
    }

    public static PixelChromaMapValidationReport ValidateForWorkshop(
        MapData mapData,
        int spawnX,
        int spawnY,
        IReadOnlyList<MapEditorSpawnPointData> spawnPoints,
        RectInt? previewRegion)
    {
        PixelChromaMapValidationReport report = Validate(mapData, spawnX, spawnY, spawnPoints);
        if (mapData == null)
        {
            return report;
        }

        ValidatePreviewRegion(mapData, previewRegion, report);
        return Finish(report);
    }

    private static void ValidatePreviewRegion(
        MapData mapData,
        RectInt? previewRegion,
        PixelChromaMapValidationReport report)
    {
        if (!previewRegion.HasValue)
        {
            Fail(report, L("프리뷰 이미지 영역이 지정되지 않았습니다. 프리뷰 영역 도구로 맵의 대표 장면을 드래그하세요.", "No preview region is set. Drag a representative map area with the preview tool."));
            return;
        }

        RectInt region = previewRegion.Value;
        report.previewRegionSet = true;
        report.previewWidth = region.width;
        report.previewHeight = region.height;

        if (region.width < MinimumPreviewDimension || region.height < MinimumPreviewDimension)
        {
            Fail(report, L("프리뷰 영역은 가로와 세로가 각각 최소 ", "The preview must be at least ") + MinimumPreviewDimension + L("칸이어야 합니다. 현재 ", " cells wide and high. Current size: ") + region.width + "×" + region.height + L("입니다.", "."));
            return;
        }

        if (region.xMin < 0
            || region.yMin < 0
            || region.xMax > mapData.width
            || region.yMax > mapData.height)
        {
            Fail(report, L("프리뷰 영역이 맵 범위를 벗어났습니다. 맵 안쪽에서 다시 지정하세요.", "The preview region extends outside the map. Select it again inside the map."));
            return;
        }

        for (int y = region.yMin; y < region.yMax; y++)
        {
            for (int x = region.xMin; x < region.xMax; x++)
            {
                if (HasVisibleTile(mapData, x, y))
                {
                    report.previewPaintedCellCount++;
                }
            }
        }

        if (report.previewPaintedCellCount == 0)
        {
            Fail(report, L("프리뷰 영역에 표시할 타일이 없습니다. 맵 내용이 보이는 영역을 지정하세요.", "The preview region contains no visible tiles. Select an area containing map content."));
            return;
        }

        Pass(report, L("프리뷰 이미지 영역: ", "Preview region: ") + region.width + "×" + region.height + L(" / 표시 타일 ", " / visible tiles ") + report.previewPaintedCellCount);
    }

    private static void AnalyzePlayableArea(MapData mapData, PixelChromaMapValidationReport report)
    {
        Vector2Int firstBoundaryLeak = new Vector2Int(-1, -1);
        Vector2Int firstIsolatedGround = new Vector2Int(-1, -1);
        Vector2Int firstUnvisualizedWall = new Vector2Int(-1, -1);

        for (int y = 0; y < mapData.height; y++)
        {
            for (int x = 0; x < mapData.width; x++)
            {
                bool hasGround = HasGround(mapData, x, y);
                bool wall = IsWall(mapData, x, y);

                if (hasGround)
                {
                    report.groundCellCount++;
                }

                if (wall && !HasVisibleTile(mapData, x, y))
                {
                    report.unvisualizedWallTileCount++;
                    RememberFirst(ref firstUnvisualizedWall, x, y);
                }

                if (!hasGround || wall)
                {
                    continue;
                }

                report.walkableTileCount++;

                if (HasOpenPlayableEdge(mapData, x, y))
                {
                    report.boundaryLeakCount++;
                    RememberFirst(ref firstBoundaryLeak, x, y);
                }

                if (!HasWalkableNeighbor(mapData, x, y))
                {
                    report.isolatedGroundTileCount++;
                    RememberFirst(ref firstIsolatedGround, x, y);
                }
            }
        }

        if (report.walkableTileCount < MinimumWalkableTileCount)
        {
            Fail(report, L("이동 가능한 바닥이 ", "Only ") + report.walkableTileCount + L("칸뿐입니다. 최소 ", " walkable cells are available. Provide at least ") + MinimumWalkableTileCount + L("칸을 확보하세요.", "."));
        }
        else
        {
            Pass(report, L("이동 가능한 바닥: ", "Walkable ground: ") + report.walkableTileCount);
        }

        if (report.boundaryLeakCount > 0)
        {
            Fail(report, L("이동 영역의 열린 가장자리가 ", "The playable area has ") + report.boundaryLeakCount + L("칸 있습니다. 예: ", " open boundary cells. Example: ") + FormatPosition(firstBoundaryLeak) + L(". 바닥이 끝나는 곳과 맵 외곽을 충돌벽으로 막으세요.", ". Block ground edges and map boundaries with collision walls."));
        }
        else if (report.walkableTileCount > 0)
        {
            Pass(report, L("이동 영역의 모든 가장자리가 충돌벽으로 닫혀 있습니다.", "All playable-area boundaries are closed by collision walls."));
        }

        if (report.isolatedGroundTileCount > 0)
        {
            Fail(report, L("상하좌우로 이동할 수 없는 고립 바닥이 ", "There are ") + report.isolatedGroundTileCount + L("칸 있습니다. 예: ", " isolated ground cells. Example: ") + FormatPosition(firstIsolatedGround) + ".");
        }
        else if (report.walkableTileCount > 0)
        {
            Pass(report, L("한 칸짜리 고립 바닥이 없습니다.", "There are no isolated single-cell ground tiles."));
        }

        if (report.unvisualizedWallTileCount > 0)
        {
            Warn(report, L("표시 타일이 없는 투명 충돌벽이 ", "There are ") + report.unvisualizedWallTileCount + L("칸 있습니다. 예: ", " invisible collision walls. Example: ") + FormatPosition(firstUnvisualizedWall) + L(". 의도한 벽인지 확인하세요.", ". Verify that these walls are intentional."));
        }
        else if (report.wallTileCount > 0)
        {
            Pass(report, L("모든 충돌벽 위치에 표시 타일이 있습니다.", "Every collision wall has a visible tile."));
        }
    }

    private static void CountMapContents(
        MapData mapData,
        PixelChromaMapValidationReport report,
        HashSet<string> usedTilesets,
        HashSet<string> missingTilesets,
        HashSet<string> validatedAnimations,
        HashSet<string> validatedImageTiles)
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
                        continue;
                    }

                    int imageIndex = mapData.GetImageIndex(x, y, layerType);
                    if (MapEditorTilesetLibraryService.TryGetAnimation(
                            imagePath,
                            imageIndex,
                            out MapEditorTilesetDefinition tileset,
                            out MapEditorTilesetAnimationDefinition animation))
                    {
                        report.animatedTileCount++;
                        string animationKey = imagePath + "#" + animation.id;
                        if (validatedAnimations.Add(animationKey))
                        {
                            report.animationDefinitionCount++;
                            if (!TryValidateAnimation(tileset, animation, out string animationError))
                            {
                                report.invalidAnimationCount++;
                                Fail(report, L("애니메이션 타일을 내보낼 수 없습니다: ", "Could not export animated tile: ") + animationError);
                            }
                        }

                        continue;
                    }

                    string imageTileKey = imagePath + "#" + imageIndex;
                    if (validatedImageTiles.Add(imageTileKey) && !CanCreateImageTile(imagePath, imageIndex))
                    {
                        Fail(report, L("이미지 타일을 만들 수 없습니다: ", "Could not create image tile: ") + imagePath + " #" + imageIndex);
                    }
                }
            }
        }
    }

    private static bool TryValidateAnimation(
        MapEditorTilesetDefinition tileset,
        MapEditorTilesetAnimationDefinition animation,
        out string error)
    {
        error = string.Empty;

        if (tileset == null || animation == null)
        {
            error = L("타일셋 또는 애니메이션 정의가 없습니다.", "The tileset or animation definition is missing.");
            return false;
        }

        int frameCount = animation.frameTileIds != null && animation.frameTileIds.Length > 0
            ? animation.frameTileIds.Length
            : animation.frameCount;
        if (frameCount < MapEditorTilesetLibraryService.MinAnimationFrameCount
            || frameCount > MapEditorTilesetLibraryService.MaxAnimationFrameCount)
        {
            error = GetAnimationLabel(tileset, animation) + L("의 프레임 수가 2~32 범위를 벗어났습니다.", " has a frame count outside the 2-32 range.");
            return false;
        }

        if (float.IsNaN(animation.framesPerSecond)
            || float.IsInfinity(animation.framesPerSecond)
            || animation.framesPerSecond < MapEditorTilesetLibraryService.MinAnimationFramesPerSecond
            || animation.framesPerSecond > MapEditorTilesetLibraryService.MaxAnimationFramesPerSecond)
        {
            error = GetAnimationLabel(tileset, animation) + L("의 재생 속도가 1~30 FPS 범위를 벗어났습니다.", " has a playback speed outside the 1-30 FPS range.");
            return false;
        }

        int gridSize = animation.GetFrameGridSize(tileset.atlasGridSize);
        if (gridSize <= 0 || tileset.columns <= 0 || tileset.rows <= 0)
        {
            error = GetAnimationLabel(tileset, animation) + L("의 타일셋 격자 정보가 올바르지 않습니다.", " has invalid tileset-grid data.");
            return false;
        }

        HashSet<int> uniqueFrames = new HashSet<int>();
        for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            int frameTileId = animation.GetFrameTileId(frameIndex);
            int column = frameTileId % gridSize;
            int rowFromBottom = frameTileId / gridSize;
            int sourceRowFromTop = gridSize - 1 - rowFromBottom;

            bool outsideLegacyContent = animation.frameGridSize <= 0
                && (column < 0
                    || column >= tileset.columns
                    || sourceRowFromTop < 0
                    || sourceRowFromTop >= tileset.rows);
            if (frameTileId < 0
                || frameTileId >= gridSize * gridSize
                || outsideLegacyContent)
            {
                error = GetAnimationLabel(tileset, animation)
                    + L("의 ", " frame ") + (frameIndex + 1)
                    + L("번째 프레임이 타일셋 범위 밖입니다.", " is outside the tileset bounds.");
                return false;
            }

            if (!uniqueFrames.Add(frameTileId))
            {
                error = GetAnimationLabel(tileset, animation) + L("에 중복 프레임이 있습니다.", " contains duplicate frames.");
                return false;
            }

            int frameImageIndex = MapEditorPngTilesetService.EncodePaletteTileIndex(gridSize, frameTileId);
            if (!CanCreateImageTile(tileset.atlasPath, frameImageIndex))
            {
                error = GetAnimationLabel(tileset, animation)
                    + L("의 ", " frame ") + (frameIndex + 1)
                    + L("번째 프레임을 읽을 수 없습니다.", " could not be read.");
                return false;
            }
        }

        return true;
    }

    private static bool CanCreateImageTile(string imagePath, int imageIndex)
    {
        try
        {
            return PngTilesets.GetTileSprite(imagePath, imageIndex) != null;
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    private static string GetAnimationLabel(
        MapEditorTilesetDefinition tileset,
        MapEditorTilesetAnimationDefinition animation)
    {
        string tilesetName = string.IsNullOrWhiteSpace(tileset.displayName) ? tileset.id : tileset.displayName;
        string animationName = string.IsNullOrWhiteSpace(animation.displayName) ? animation.id : animation.displayName;
        return tilesetName + " / " + animationName;
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
            Fail(report, L("시작 위치가 없습니다. 시작 위치 도구로 최소 한 곳을 지정하세요.", "No spawn is set. Place at least one with the Shared Spawn tool."));
            return;
        }

        HashSet<string> rolePositions = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        int runnerCount = 0;
        int seekerCount = 0;

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            MapEditorSpawnPointData spawn = spawnPoints[i];
            bool seeker = string.Equals(spawn.role, "Seeker", System.StringComparison.OrdinalIgnoreCase);
            string roleLabel = seeker
                ? L("술래 시작 위치", "Seeker spawn")
                : L("플레이어 시작 위치", "Runner spawn");
            string label = string.IsNullOrEmpty(spawn.id) ? roleLabel : spawn.id + " (" + roleLabel + ")";
            if (seeker) seekerCount++;
            else runnerCount++;

            if (!mapData.IsInside(spawn.x, spawn.y))
            {
                Fail(report, label + L("가 맵 범위 밖에 있습니다.", " is outside the map."));
                continue;
            }

            Vector2Int position = new Vector2Int(spawn.x, spawn.y);
            string normalizedRole = seeker ? "Seeker" : "Runner";
            string rolePositionKey = normalizedRole + ":" + position.x + ":" + position.y;
            if (!rolePositions.Add(rolePositionKey))
            {
                Fail(report, label + L("가 같은 역할의 다른 시작 위치와 겹칩니다.", " overlaps another spawn for the same role."));
            }

            if (IsWall(mapData, spawn.x, spawn.y))
            {
                Fail(report, label + L("가 Wall 위에 있습니다.", " is on a Wall tile."));
            }
            else if (!HasGround(mapData, spawn.x, spawn.y))
            {
                Fail(report, label + L(" 아래에 바닥 타일이 없습니다.", " has no ground tile underneath."));
            }
            else if (!HasWalkableNeighbor(mapData, spawn.x, spawn.y))
            {
                Fail(report, label + L("가 이동할 수 없는 한 칸 공간에 갇혀 있습니다.", " is trapped in a non-walkable single-cell space."));
            }
            else
            {
                Pass(report, label + L(" 배치가 정상입니다.", " placement is valid."));
            }
        }

        if (runnerCount == 0)
        {
            Fail(report, L("플레이어 시작 위치가 없습니다. '공용 시작 위치' 도구로 한 곳을 지정하세요.", "No runner spawn is set. Place one with the Shared Spawn tool."));
        }

        if (seekerCount == 0)
        {
            Fail(report, L("술래 시작 위치가 없습니다. '공용 시작 위치' 도구로 한 곳을 지정하세요.", "No seeker spawn is set. Place one with the Shared Spawn tool."));
        }
    }

    private static void ValidatePlayableConnectivity(
        MapData mapData,
        IReadOnlyList<MapEditorSpawnPointData> spawnPoints,
        PixelChromaMapValidationReport report)
    {
        MapEditorSpawnPointData startSpawn = null;
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (IsWalkable(mapData, spawnPoints[i].x, spawnPoints[i].y))
            {
                startSpawn = spawnPoints[i];
                break;
            }
        }

        if (startSpawn == null)
        {
            return;
        }

        bool[,] visited = new bool[mapData.width, mapData.height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Vector2Int start = new Vector2Int(startSpawn.x, startSpawn.y);
        visited[start.x, start.y] = true;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int next = current + CardinalDirections[i];
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

        bool allSpawnsReachable = true;
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            MapEditorSpawnPointData spawn = spawnPoints[i];
            if (IsWalkable(mapData, spawn.x, spawn.y) && !visited[spawn.x, spawn.y])
            {
                allSpawnsReachable = false;
                string roleLabel = string.Equals(spawn.role, "Seeker", System.StringComparison.OrdinalIgnoreCase)
                    ? L("술래 시작 위치", "the seeker spawn")
                    : L("플레이어 시작 위치", "the runner spawn");
                Fail(report, L(roleLabel + "까지 이동 가능한 경로가 없습니다.", "There is no walkable path to " + roleLabel + "."));
            }
        }

        if (allSpawnsReachable && spawnPoints.Count > 1)
        {
            Pass(report, L("모든 시작 위치가 서로 연결되어 있습니다.", "All spawn points are connected."));
        }

        Vector2Int firstUnreachable = new Vector2Int(-1, -1);
        for (int y = 0; y < mapData.height; y++)
        {
            for (int x = 0; x < mapData.width; x++)
            {
                if (IsWalkable(mapData, x, y) && !visited[x, y])
                {
                    report.unreachableWalkableTileCount++;
                    RememberFirst(ref firstUnreachable, x, y);
                }
            }
        }

        if (report.unreachableWalkableTileCount > 0)
        {
            Fail(report, L("시작 위치에서 도달할 수 없는 바닥이 ", "There are ") + report.unreachableWalkableTileCount + L("칸 있습니다. 예: ", " ground cells unreachable from a spawn. Example: ") + FormatPosition(firstUnreachable) + L(". 끊어진 통로를 연결하세요.", ". Connect the disconnected paths."));
        }
        else if (report.walkableTileCount > 0)
        {
            Pass(report, L("모든 이동 가능한 바닥이 시작 위치와 연결되어 있습니다.", "All walkable ground is connected to a spawn."));
        }
    }

    private static bool IsWalkable(MapData mapData, int x, int y)
    {
        return mapData.IsInside(x, y) && HasGround(mapData, x, y) && !IsWall(mapData, x, y);
    }

    private static bool HasGround(MapData mapData, int x, int y)
    {
        if (mapData.GetTile(x, y, MapEditorLayerType.Ground) != -1)
        {
            return true;
        }

        MapEditorLayerType[] optionalGroundLayers = MapEditorLayerUtility.GroundOptionalLayers;
        for (int i = 0; i < optionalGroundLayers.Length; i++)
        {
            if (mapData.GetTile(x, y, optionalGroundLayers[i]) != -1)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWall(MapData mapData, int x, int y)
    {
        return mapData.GetTile(x, y, MapEditorLayerType.WallCollision) == MapEditorManager.WallTileId;
    }

    private static bool HasVisibleTile(MapData mapData, int x, int y)
    {
        MapEditorLayerType[] layers = MapEditorLayerUtility.SerializableLayers;
        for (int i = 0; i < layers.Length; i++)
        {
            MapEditorLayerType baseLayer = MapEditorLayerUtility.GetBaseLayer(layers[i]);
            if (baseLayer == MapEditorLayerType.WallCollision || baseLayer == MapEditorLayerType.Zone)
            {
                continue;
            }

            if (mapData.GetTile(x, y, layers[i]) != -1)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasWalkableNeighbor(MapData mapData, int x, int y)
    {
        for (int i = 0; i < CardinalDirections.Length; i++)
        {
            Vector2Int neighbor = new Vector2Int(x, y) + CardinalDirections[i];
            if (IsWalkable(mapData, neighbor.x, neighbor.y))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasOpenPlayableEdge(MapData mapData, int x, int y)
    {
        for (int i = 0; i < CardinalDirections.Length; i++)
        {
            Vector2Int neighbor = new Vector2Int(x, y) + CardinalDirections[i];
            if (!mapData.IsInside(neighbor.x, neighbor.y))
            {
                return true;
            }

            if (!HasGround(mapData, neighbor.x, neighbor.y) && !IsWall(mapData, neighbor.x, neighbor.y))
            {
                return true;
            }
        }

        return false;
    }

    private static void RememberFirst(ref Vector2Int position, int x, int y)
    {
        if (position.x < 0)
        {
            position = new Vector2Int(x, y);
        }
    }

    private static string FormatPosition(Vector2Int position)
    {
        return "(" + position.x + ", " + position.y + ")";
    }

    private static string L(string korean, string english)
    {
        return MapEditorLocalization.Choose(korean, english);
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
            Debug.LogError(L("PixelChroma 맵 검사 실패: ", "PixelChroma map validation failed: ") + report.errors[i]);
        }

        for (int i = 0; i < report.warnings.Count; i++)
        {
            Debug.LogWarning(L("PixelChroma 맵 검사 경고: ", "PixelChroma map validation warning: ") + report.warnings[i]);
        }

        if (report.isValid)
        {
            Debug.Log(L("PixelChroma 맵 검사에 합격했습니다.", "PixelChroma map validation passed."));
        }
    }
}
