using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class MapEditorPixelChromaExportService
{
    private const string GroundLayerName = "Ground";
    private const string ObjectLayerName = "Object";
    private const string WallVisualLayerName = "Wall";
    private const string WallLayerName = "WallLine";
    private const string UnityWallLayerName = "Wall";
    private const string GroundLayerKind = "ground";
    private const string ObjectLayerKind = "object";
    private const string WallVisualLayerKind = "wall_visual";
    private const string WallCollisionLayerKind = "collision";
    private readonly MapEditorPngTilesetService pngTilesets = new MapEditorPngTilesetService();

    public bool ExportWithDialog(MapData mapData, string mapId, int cellSize)
    {
        return ExportWithDialog(mapData, mapId, cellSize, 0, 0);
    }

    public bool ExportWithDialog(MapData mapData, string mapId, int cellSize, int spawnX, int spawnY)
    {
        return ExportWithDialog(mapData, mapId, cellSize, spawnX, spawnY, null);
    }

    public bool ExportWithDialog(MapData mapData, string mapId, int cellSize, int spawnX, int spawnY, IReadOnlyList<MapEditorSpawnPointData> spawnPoints)
    {
        string defaultName = string.IsNullOrWhiteSpace(mapId) ? "pixelchroma_map.json" : SanitizeId(mapId) + ".json";
        string path = MapEditorFileDialog.SaveFile("PixelChroma용 맵 내보내기", defaultName, "json");

        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        MapEditorFileDialog.RememberDirectory(path);
        return Export(mapData, path, mapId, cellSize, spawnX, spawnY, spawnPoints);
    }

    public bool Export(MapData mapData, string path, string mapId, int cellSize)
    {
        return Export(mapData, path, mapId, cellSize, string.Empty, 0, 0);
    }

    public bool Export(MapData mapData, string path, string mapId, int cellSize, string tilesetRelativeFolder)
    {
        return Export(mapData, path, mapId, cellSize, tilesetRelativeFolder, 0, 0);
    }

    public bool Export(MapData mapData, string path, string mapId, int cellSize, int spawnX, int spawnY)
    {
        return Export(mapData, path, mapId, cellSize, string.Empty, spawnX, spawnY, null);
    }

    public bool Export(MapData mapData, string path, string mapId, int cellSize, int spawnX, int spawnY, IReadOnlyList<MapEditorSpawnPointData> spawnPoints)
    {
        return Export(mapData, path, mapId, cellSize, string.Empty, spawnX, spawnY, spawnPoints);
    }

    public bool Export(MapData mapData, string path, string mapId, int cellSize, string tilesetRelativeFolder, int spawnX, int spawnY)
    {
        return Export(mapData, path, mapId, cellSize, tilesetRelativeFolder, spawnX, spawnY, null);
    }

    public bool Export(MapData mapData, string path, string mapId, int cellSize, string tilesetRelativeFolder, int spawnX, int spawnY, IReadOnlyList<MapEditorSpawnPointData> spawnPoints)
    {
        if (mapData == null || string.IsNullOrEmpty(path))
        {
            return false;
        }

        PixelChromaMapValidationReport validation = MapEditorPixelChromaValidationService.Validate(mapData, spawnX, spawnY, spawnPoints);
        MapEditorPixelChromaValidationService.Log(validation);

        if (!validation.isValid)
        {
            Debug.LogError("맵 검사에서 오류가 발견되어 PixelChroma 맵을 내보내지 못했습니다.");
            return false;
        }

        PixelChromaMapExportData exportData = BuildExportData(mapData, mapId, cellSize, tilesetRelativeFolder, spawnX, spawnY, spawnPoints, validation);
        string json = JsonUtility.ToJson(exportData, true);
        string directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, json);
        Debug.Log("PixelChroma 맵을 내보냈습니다: " + path);
        return true;
    }

    private PixelChromaMapExportData BuildExportData(MapData mapData, string mapId, int cellSize, string tilesetRelativeFolder, int spawnX, int spawnY, IReadOnlyList<MapEditorSpawnPointData> spawnPoints, PixelChromaMapValidationReport validation)
    {
        mapData.EnsureInitialized();

        PixelChromaMapExportData exportData = new PixelChromaMapExportData
        {
            mapId = string.IsNullOrWhiteSpace(mapId) ? "map" : SanitizeId(mapId),
            width = mapData.width,
            height = mapData.height,
            cellSize = PixelChromaExportContract.TilePixelSize,
            runtime = CreateRuntimeData(),
            bounds = CreateBounds(mapData.width, mapData.height),
            validation = validation
        };

        Dictionary<MapEditorLayerType, PixelChromaMapLayerExportData> layers = CreateLayerMap();
        Dictionary<MapEditorLayerType, Dictionary<int, int>> tileIndices = CreateLayerTileIndexMap();

        MapEditorLayerType[] exportLayerTypes = MapData.GetSerializableLayers();

        for (int layerIndex = 0; layerIndex < exportLayerTypes.Length; layerIndex++)
        {
            MapEditorLayerType layerType = exportLayerTypes[layerIndex];

            if (layerType == MapEditorLayerType.Zone)
            {
                continue;
            }

            for (int y = 0; y < mapData.height; y++)
            {
                for (int x = 0; x < mapData.width; x++)
                {
                    int tileId = mapData.GetTile(x, y, layerType);

                    if (tileId == -1)
                    {
                        continue;
                    }

                    MapEditorLayerType normalizedLayer = NormalizeExportLayer(layerType, tileId);
                    PixelChromaTileExportData tile = CreateTileData(mapData, x, y, tileId, layerType, normalizedLayer);

                    if (tile == null)
                    {
                        continue;
                    }

                    AddOrCompositeTile(layers[normalizedLayer], tileIndices[normalizedLayer], tile, mapData.width);
                }
            }
        }

        AddLayerIfNotEmpty(exportData.layers, layers[MapEditorLayerType.Ground]);
        AddLayerIfNotEmpty(exportData.layers, layers[MapEditorLayerType.Object]);
        AddLayerIfNotEmpty(exportData.layers, layers[MapEditorLayerType.WallVisual]);
        AddLayerIfNotEmpty(exportData.layers, layers[MapEditorLayerType.WallCollision]);

        if (layers[MapEditorLayerType.Spawn].tiles.Count > 0)
        {
            AddLayerIfNotEmpty(exportData.layers, layers[MapEditorLayerType.Spawn]);
        }

        AddSpawnPoints(exportData, mapData, spawnX, spawnY, spawnPoints);
        AddZones(exportData, mapData);

        return exportData;
    }

    private static void AddZones(PixelChromaMapExportData exportData, MapData mapData)
    {
        bool[,] visited = new bool[mapData.width, mapData.height];
        int zoneIndex = 1;

        for (int y = 0; y < mapData.height; y++)
        {
            for (int x = 0; x < mapData.width; x++)
            {
                if (visited[x, y] || mapData.GetTile(x, y, MapEditorLayerType.Zone) == -1)
                {
                    continue;
                }

                RectInt rect = FindZoneRect(mapData, visited, x, y);
                exportData.zones.Add(new PixelChromaZoneExportData
                {
                    id = "Zone_" + zoneIndex,
                    type = "PaintZone",
                    x = rect.x,
                    y = rect.y,
                    width = rect.width,
                    height = rect.height,
                    unityLayerName = "Default",
                    collider = false
                });
                zoneIndex++;
            }
        }
    }

    private static RectInt FindZoneRect(MapData mapData, bool[,] visited, int startX, int startY)
    {
        int width = 0;

        while (startX + width < mapData.width
            && !visited[startX + width, startY]
            && mapData.GetTile(startX + width, startY, MapEditorLayerType.Zone) != -1)
        {
            width++;
        }

        int height = 1;
        bool canGrow = true;

        while (canGrow && startY + height < mapData.height)
        {
            for (int x = startX; x < startX + width; x++)
            {
                if (visited[x, startY + height] || mapData.GetTile(x, startY + height, MapEditorLayerType.Zone) == -1)
                {
                    canGrow = false;
                    break;
                }
            }

            if (canGrow)
            {
                height++;
            }
        }

        for (int y = startY; y < startY + height; y++)
        {
            for (int x = startX; x < startX + width; x++)
            {
                visited[x, y] = true;
            }
        }

        return new RectInt(startX, startY, width, height);
    }

    private static void AddSpawnPoints(PixelChromaMapExportData exportData, MapData mapData, int spawnX, int spawnY, IReadOnlyList<MapEditorSpawnPointData> spawnPoints)
    {
        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                MapEditorSpawnPointData spawnPoint = spawnPoints[i];

                if (spawnPoint == null)
                {
                    continue;
                }

                AddSpawnPoint(exportData, mapData, string.IsNullOrEmpty(spawnPoint.id) ? "SpawnPoint_" + (i + 1) : spawnPoint.id, spawnPoint.x, spawnPoint.y, spawnPoint.role);
            }
        }

        if (exportData.spawnPoints.Count == 0)
        {
            AddSpawnPoint(exportData, mapData, "SpawnPoint_1", spawnX, spawnY, "Any");
        }
    }

    private static void AddSpawnPoint(PixelChromaMapExportData exportData, MapData mapData, string id, int spawnX, int spawnY, string role)
    {
        int clampedX = Mathf.Clamp(spawnX, 0, mapData.width - 1);
        int clampedY = Mathf.Clamp(spawnY, 0, mapData.height - 1);

        exportData.spawnPoints.Add(new PixelChromaSpawnPointExportData
        {
            id = id,
            x = clampedX,
            y = clampedY,
            worldX = clampedX + 0.5f,
            worldY = mapData.height - 1 - clampedY + 0.5f,
            role = string.IsNullOrEmpty(role) ? "Any" : role,
            component = "PixelChroma.MapSpawnPoint"
        });
    }

    private static PixelChromaRuntimeExportData CreateRuntimeData()
    {
        return new PixelChromaRuntimeExportData
        {
            target = "PixelChroma",
            coordinateSystem = "MapEditorTopLeftToUnityTilemapXY",
            mapOrigin = "TopLeft",
            mapYAxis = "Down",
            unityTilemapOrigin = "BottomLeft",
            unityYConversion = "unityY = height - 1 - mapY",
            tileWorldSize = 1f,
            pixelsPerUnit = 16f,
            groundLayerName = GroundLayerName,
            objectLayerName = ObjectLayerName,
            wallVisualLayerName = WallVisualLayerName,
            wallLayerName = WallLayerName,
            wallCollider = "TilemapCollider2D",
            spawnComponent = "PixelChroma.MapSpawnPoint",
            zoneComponent = "PixelChroma.MapZone",
            layerBindings = CreateRuntimeLayerBindings()
        };
    }

    private static List<PixelChromaRuntimeLayerBindingData> CreateRuntimeLayerBindings()
    {
        return new List<PixelChromaRuntimeLayerBindingData>
        {
            CreateLayerBinding("Ground", GroundLayerName, GroundLayerKind, GroundLayerName, "Default", 0, true, false, false),
            CreateLayerBinding("Object", ObjectLayerName, ObjectLayerKind, ObjectLayerName, "Default", 2, true, false, false),
            CreateLayerBinding("WallVisual", WallVisualLayerName, WallVisualLayerKind, WallVisualLayerName, "Default", 2, true, false, false),
            CreateLayerBinding("WallCollision", WallLayerName, WallCollisionLayerKind, WallLayerName, UnityWallLayerName, 3, true, true, false),
            CreateLayerBinding("Spawn", "Spawn", "spawn_marker", string.Empty, "Default", 4, false, false, true),
            CreateLayerBinding("Zone", "Zone", "zone", string.Empty, "Default", 0, false, false, true)
        };
    }

    private static PixelChromaRuntimeLayerBindingData CreateLayerBinding(
        string editorLayer,
        string exportLayerName,
        string kind,
        string targetTilemapName,
        string unityLayerName,
        int sortingOrder,
        bool createsRenderer,
        bool createsCollider,
        bool metadataOnly)
    {
        return new PixelChromaRuntimeLayerBindingData
        {
            editorLayer = editorLayer,
            exportLayerName = exportLayerName,
            kind = kind,
            targetTilemapName = targetTilemapName,
            unityLayerName = unityLayerName,
            sortingOrder = sortingOrder,
            createsRenderer = createsRenderer,
            createsCollider = createsCollider,
            metadataOnly = metadataOnly
        };
    }

    private static PixelChromaMapLayerExportData CreateGroundLayer()
    {
        return new PixelChromaMapLayerExportData
        {
            name = GroundLayerName,
            kind = GroundLayerKind,
            unityLayerName = "Default",
            sortingOrder = 0,
            collider = false,
            colliderType = string.Empty,
            colliderIsTrigger = false,
            compositeOperation = "None"
        };
    }

    private static PixelChromaMapLayerExportData CreateObjectLayer()
    {
        return new PixelChromaMapLayerExportData
        {
            name = ObjectLayerName,
            kind = ObjectLayerKind,
            unityLayerName = "Default",
            sortingOrder = 2,
            collider = false,
            colliderType = string.Empty,
            colliderIsTrigger = false,
            compositeOperation = "None"
        };
    }

    private static PixelChromaMapLayerExportData CreateWallVisualLayer()
    {
        return new PixelChromaMapLayerExportData
        {
            name = WallVisualLayerName,
            kind = WallVisualLayerKind,
            unityLayerName = "Default",
            sortingOrder = 2,
            collider = false,
            colliderType = string.Empty,
            colliderIsTrigger = false,
            compositeOperation = "None"
        };
    }

    private static PixelChromaMapLayerExportData CreateWallCollisionLayer()
    {
        return new PixelChromaMapLayerExportData
        {
            name = WallLayerName,
            kind = WallCollisionLayerKind,
            unityLayerName = UnityWallLayerName,
            sortingOrder = 3,
            collider = true,
            colliderType = "TilemapCollider2D",
            colliderIsTrigger = false,
            compositeOperation = "None"
        };
    }

    private static PixelChromaMapLayerExportData CreateSpawnMarkerLayer()
    {
        return new PixelChromaMapLayerExportData
        {
            name = "Spawn",
            kind = "spawn_marker",
            unityLayerName = "Default",
            sortingOrder = 4,
            collider = false,
            colliderType = string.Empty,
            colliderIsTrigger = false,
            compositeOperation = "None"
        };
    }

    private static Dictionary<MapEditorLayerType, PixelChromaMapLayerExportData> CreateLayerMap()
    {
        return new Dictionary<MapEditorLayerType, PixelChromaMapLayerExportData>
        {
            { MapEditorLayerType.Ground, CreateGroundLayer() },
            { MapEditorLayerType.Object, CreateObjectLayer() },
            { MapEditorLayerType.WallVisual, CreateWallVisualLayer() },
            { MapEditorLayerType.WallCollision, CreateWallCollisionLayer() },
            { MapEditorLayerType.Spawn, CreateSpawnMarkerLayer() }
        };
    }

    private static Dictionary<MapEditorLayerType, Dictionary<int, int>> CreateLayerTileIndexMap()
    {
        return new Dictionary<MapEditorLayerType, Dictionary<int, int>>
        {
            { MapEditorLayerType.Ground, new Dictionary<int, int>() },
            { MapEditorLayerType.Object, new Dictionary<int, int>() },
            { MapEditorLayerType.WallVisual, new Dictionary<int, int>() },
            { MapEditorLayerType.WallCollision, new Dictionary<int, int>() },
            { MapEditorLayerType.Spawn, new Dictionary<int, int>() }
        };
    }

    private static void AddLayerIfNotEmpty(List<PixelChromaMapLayerExportData> exportLayers, PixelChromaMapLayerExportData layer)
    {
        if (layer != null && layer.tiles.Count > 0)
        {
            exportLayers.Add(layer);
        }
    }

    private static MapEditorLayerType NormalizeExportLayer(MapEditorLayerType layerType, int tileId)
    {
        if (tileId == MapEditorManager.WallTileId)
        {
            return MapEditorLayerType.WallCollision;
        }

        if (layerType == MapEditorLayerType.WallCollision || layerType == MapEditorLayerType.Spawn)
        {
            return layerType;
        }

        layerType = MapEditorLayerUtility.GetBaseLayer(layerType);

        if (layerType == MapEditorLayerType.Object || layerType == MapEditorLayerType.WallVisual)
        {
            return layerType;
        }

        return MapEditorLayerType.Ground;
    }

    private static void AddOrCompositeTile(
        PixelChromaMapLayerExportData layer,
        Dictionary<int, int> tileIndices,
        PixelChromaTileExportData tile,
        int mapWidth)
    {
        int coordinateKey = tile.y * mapWidth + tile.x;

        if (!tileIndices.TryGetValue(coordinateKey, out int existingIndex))
        {
            tileIndices[coordinateKey] = layer.tiles.Count;
            layer.tiles.Add(tile);
            return;
        }

        layer.tiles[existingIndex] = CompositeExportTiles(layer.tiles[existingIndex], tile);
    }

    private static PixelChromaTileExportData CompositeExportTiles(
        PixelChromaTileExportData below,
        PixelChromaTileExportData above)
    {
        const int resolution = PixelChromaExportContract.TilePixelSize;
        bool belowAnimated = HasAnimationFrames(below);
        bool aboveAnimated = HasAnimationFrames(above);

        if (belowAnimated || aboveAnimated)
        {
            float outputFps = Mathf.Max(GetAnimationFps(below), GetAnimationFps(above));
            int outputFrameCount = ResolveCompositeAnimationFrameCount(below, above, outputFps);
            PixelChromaAnimationFrameExportData[] frames = new PixelChromaAnimationFrameExportData[outputFrameCount];

            for (int frameIndex = 0; frameIndex < outputFrameCount; frameIndex++)
            {
                float time = frameIndex / outputFps;
                frames[frameIndex] = new PixelChromaAnimationFrameExportData
                {
                    pixelResolution = resolution,
                    pixelHexes = CompositePixels(below, above, resolution, time)
                };
            }

            return new PixelChromaTileExportData
            {
                x = above.x,
                y = above.y,
                kind = PixelChromaExportContract.AnimatedPixelTileKind,
                tilesetId = string.Empty,
                tileId = -1,
                colorHex = "#" + ColorUtility.ToHtmlStringRGBA(AveragePixels(frames[0].pixelHexes)),
                rotation = 0,
                flipX = false,
                flipY = false,
                collision = below.collision || above.collision,
                animationFps = outputFps,
                animationLoop = (!belowAnimated || below.animationLoop) && (!aboveAnimated || above.animationLoop),
                animationFrames = frames
            };
        }

        string[] pixels = CompositePixels(below, above, resolution, 0f);

        return new PixelChromaTileExportData
        {
            x = above.x,
            y = above.y,
            kind = PixelChromaExportContract.PixelTileKind,
            tilesetId = string.Empty,
            tileId = -1,
            colorHex = "#" + ColorUtility.ToHtmlStringRGBA(AveragePixels(pixels)),
            pixelResolution = resolution,
            pixelHexes = pixels,
            rotation = 0,
            flipX = false,
            flipY = false,
            collision = below.collision || above.collision
        };
    }

    private static string[] CompositePixels(
        PixelChromaTileExportData below,
        PixelChromaTileExportData above,
        int resolution,
        float time)
    {
        string[] pixels = new string[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Color belowColor = SampleExportTile(below, x, y, resolution, time);
                Color aboveColor = SampleExportTile(above, x, y, resolution, time);
                Color result = AlphaBlend(belowColor, aboveColor);
                pixels[y * resolution + x] = "#" + ColorUtility.ToHtmlStringRGBA(result);
            }
        }

        return pixels;
    }

    private static bool HasAnimationFrames(PixelChromaTileExportData tile)
    {
        return tile != null
            && tile.animationFrames != null
            && tile.animationFrames.Length > 0;
    }

    private static float GetAnimationFps(PixelChromaTileExportData tile)
    {
        return HasAnimationFrames(tile) && tile.animationFps > 0f
            ? tile.animationFps
            : 1f;
    }

    private static int ResolveCompositeAnimationFrameCount(
        PixelChromaTileExportData below,
        PixelChromaTileExportData above,
        float outputFps)
    {
        float duration = 0f;

        if (HasAnimationFrames(below))
        {
            duration = Mathf.Max(duration, below.animationFrames.Length / GetAnimationFps(below));
        }

        if (HasAnimationFrames(above))
        {
            duration = Mathf.Max(duration, above.animationFrames.Length / GetAnimationFps(above));
        }

        return Mathf.Clamp(
            Mathf.CeilToInt(Mathf.Max(1f / outputFps, duration) * outputFps),
            MapEditorTilesetLibraryService.MinAnimationFrameCount,
            MapEditorTilesetLibraryService.MaxAnimationFrameCount);
    }

    private static Color SampleExportTile(
        PixelChromaTileExportData tile,
        int x,
        int y,
        int targetResolution,
        float time)
    {
        if (HasAnimationFrames(tile))
        {
            int frameIndex = Mathf.FloorToInt(Mathf.Max(0f, time) * GetAnimationFps(tile));
            frameIndex = tile.animationLoop
                ? frameIndex % tile.animationFrames.Length
                : Mathf.Clamp(frameIndex, 0, tile.animationFrames.Length - 1);
            PixelChromaAnimationFrameExportData frame = tile.animationFrames[frameIndex];
            return SamplePixels(
                frame?.pixelHexes,
                frame?.pixelResolution ?? 0,
                tile,
                x,
                y,
                targetResolution);
        }

        if (tile.pixelHexes != null && tile.pixelHexes.Length > 0 && tile.pixelResolution > 0)
        {
            return SamplePixels(tile.pixelHexes, tile.pixelResolution, tile, x, y, targetResolution);
        }

        return ParseHex(tile.colorHex, Color.white);
    }

    private static Color SamplePixels(
        string[] pixels,
        int pixelResolution,
        PixelChromaTileExportData tile,
        int x,
        int y,
        int targetResolution)
    {
        if (pixels == null || pixels.Length == 0 || pixelResolution <= 0)
        {
            return Color.clear;
        }

        float u = (x + 0.5f) / targetResolution;
        float v = (y + 0.5f) / targetResolution;
        ApplyExportTransform(tile, ref u, ref v);
        int sourceX = Mathf.Clamp(Mathf.FloorToInt(u * pixelResolution), 0, pixelResolution - 1);
        int sourceY = Mathf.Clamp(Mathf.FloorToInt(v * pixelResolution), 0, pixelResolution - 1);
        int index = sourceY * pixelResolution + sourceX;
        return index < pixels.Length ? ParseHex(pixels[index], Color.clear) : Color.clear;
    }

    private static void ApplyExportTransform(PixelChromaTileExportData tile, ref float u, ref float v)
    {
        if (tile.flipX)
        {
            u = 1f - u;
        }

        if (tile.flipY)
        {
            v = 1f - v;
        }

        float originalU = u;
        float originalV = v;
        switch ((tile.rotation % 4 + 4) % 4)
        {
            case 1:
                u = originalV;
                v = 1f - originalU;
                break;
            case 2:
                u = 1f - originalU;
                v = 1f - originalV;
                break;
            case 3:
                u = 1f - originalV;
                v = originalU;
                break;
        }
    }

    private static Color AveragePixels(string[] pixels)
    {
        Color total = Color.clear;

        for (int i = 0; i < pixels.Length; i++)
        {
            total += ParseHex(pixels[i], Color.clear);
        }

        return pixels.Length == 0 ? Color.clear : total / pixels.Length;
    }

    private static Color ParseHex(string value, Color fallback)
    {
        return !string.IsNullOrEmpty(value) && ColorUtility.TryParseHtmlString(value, out Color color)
            ? color
            : fallback;
    }

    private static Color AlphaBlend(Color below, Color above)
    {
        float alpha = above.a + below.a * (1f - above.a);

        if (alpha <= 0.0001f)
        {
            return Color.clear;
        }

        float belowWeight = below.a * (1f - above.a);
        return new Color(
            (above.r * above.a + below.r * belowWeight) / alpha,
            (above.g * above.a + below.g * belowWeight) / alpha,
            (above.b * above.a + below.b * belowWeight) / alpha,
            alpha);
    }

    private static PixelChromaMapBoundsExportData CreateBounds(int width, int height)
    {
        return new PixelChromaMapBoundsExportData
        {
            minX = 0,
            minY = 0,
            maxX = Mathf.Max(0, width - 1),
            maxY = Mathf.Max(0, height - 1),
            centerX = Mathf.Max(0, width - 1) * 0.5f,
            centerY = Mathf.Max(0, height - 1) * 0.5f
        };
    }

    private PixelChromaTileExportData CreateTileData(
        MapData mapData,
        int x,
        int y,
        int mapEditorTileId,
        MapEditorLayerType sourceLayer,
        MapEditorLayerType outputLayer)
    {
        string imagePath = mapData.GetImagePath(x, y, sourceLayer);
        int imageIndex = mapData.GetImageIndex(x, y, sourceLayer);
        bool hasImage = !string.IsNullOrEmpty(imagePath) && imageIndex >= 0;
        bool isWall = mapEditorTileId == MapEditorManager.WallTileId || outputLayer == MapEditorLayerType.WallCollision;

        PixelChromaTileExportData tile = new PixelChromaTileExportData
        {
            x = x,
            y = y,
            rotation = MapEditorRotationUtility.NormalizeQuarterTurn(mapData.GetImageRotation(x, y, sourceLayer)) / 90,
            flipX = mapData.GetImageFlipX(x, y, sourceLayer),
            flipY = mapData.GetImageFlipY(x, y, sourceLayer),
            collision = isWall
        };

        if (hasImage)
        {
            if (MapEditorTilesetLibraryService.TryGetAnimation(imagePath, imageIndex, out MapEditorTilesetDefinition tileset, out MapEditorTilesetAnimationDefinition animation))
            {
                tile.kind = PixelChromaExportContract.AnimatedPixelTileKind;
                tile.tilesetId = string.Empty;
                tile.tileId = -1;
                tile.animationFps = animation.framesPerSecond;
                tile.animationLoop = animation.loop;
                tile.animationFrames = new PixelChromaAnimationFrameExportData[Mathf.Max(1, animation.frameCount)];

                for (int frame = 0; frame < tile.animationFrames.Length; frame++)
                {
                    int frameImageIndex = MapEditorPngTilesetService.EncodePaletteTileIndex(tileset.atlasGridSize, animation.GetFrameTileId(frame));
                    Sprite frameSprite = pngTilesets.GetTileSprite(
                        imagePath,
                        frameImageIndex,
                        mapData.GetImageRotation(x, y, sourceLayer),
                        mapData.GetImageFlipX(x, y, sourceLayer),
                        mapData.GetImageFlipY(x, y, sourceLayer));
                    MapTilePixelData framePixels = MapTilePixelData.CreateFromSprite(frameSprite, PixelChromaExportContract.TilePixelSize);

                    if (framePixels == null)
                    {
                        Debug.LogError("Animated tile frame could not be exported: " + imagePath + " #" + frameImageIndex);
                        return null;
                    }

                    tile.animationFrames[frame] = new PixelChromaAnimationFrameExportData
                    {
                        pixelResolution = framePixels.resolution,
                        pixelHexes = CreatePixelHexes(framePixels)
                    };
                }

                tile.colorHex = tile.animationFrames.Length > 0 && tile.animationFrames[0].pixelHexes.Length > 0
                    ? tile.animationFrames[0].pixelHexes[0]
                    : "#FFFFFFFF";
                tile.rotation = 0;
                tile.flipX = false;
                tile.flipY = false;
                return tile;
            }

            Sprite sprite = pngTilesets.GetTileSprite(imagePath, imageIndex);
            MapTilePixelData imagePixels = MapTilePixelData.CreateFromSprite(sprite, PixelChromaExportContract.TilePixelSize);

            if (imagePixels == null)
            {
                Debug.LogError("PixelChroma 내보내기용 PNG 타일을 만들 수 없습니다: " + imagePath + " #" + imageIndex);
                return null;
            }

            tile.kind = PixelChromaExportContract.PixelTileKind;
            tile.tilesetId = string.Empty;
            tile.tileId = -1;
            tile.colorHex = "#" + ColorUtility.ToHtmlStringRGBA(imagePixels.GetAverageColor());
            tile.pixelResolution = imagePixels.resolution;
            tile.pixelHexes = CreatePixelHexes(imagePixels);
            return tile;
        }

        if (mapEditorTileId == MapEditorManager.CustomColorTileId || isWall)
        {
            MapTilePixelData pixelData = mapData.GetPixelData(x, y, sourceLayer);

            tile.tilesetId = string.Empty;
            tile.tileId = -1;
            tile.colorHex = "#" + ColorUtility.ToHtmlStringRGBA(mapData.GetColor(x, y, sourceLayer));

            if (pixelData != null)
            {
                tile.kind = PixelChromaExportContract.PixelTileKind;
                tile.pixelResolution = pixelData.resolution;
                tile.pixelHexes = CreatePixelHexes(pixelData);
            }
            else
            {
                tile.kind = PixelChromaExportContract.ColorTileKind;
            }

            return tile;
        }

        return null;
    }

    private static string[] CreatePixelHexes(MapTilePixelData pixelData)
    {
        int resolution = Mathf.Max(1, pixelData.resolution);
        string[] pixelHexes = new string[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                pixelHexes[y * resolution + x] = "#" + ColorUtility.ToHtmlStringRGBA(pixelData.GetPixel(x, y));
            }
        }

        return pixelHexes;
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
}
