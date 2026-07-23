using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
#if UNITY_EDITOR
        string defaultName = string.IsNullOrWhiteSpace(mapId) ? "pixelchroma_map.json" : SanitizeId(mapId) + ".json";
        string path = EditorUtility.SaveFilePanel("PixelChroma용 맵 내보내기", "", defaultName, "json");

        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        return Export(mapData, path, mapId, cellSize, spawnX, spawnY, spawnPoints);
#else
        Debug.LogWarning("PixelChroma 내보내기 파일 선택창은 Unity 에디터에서만 사용할 수 있습니다.");
        return false;
#endif
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
                    PixelChromaTileExportData tile = CreateTileData(mapData, x, y, tileId, normalizedLayer);

                    if (tile == null)
                    {
                        continue;
                    }

                    layers[normalizedLayer].tiles.Add(tile);
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

        if (layerType == MapEditorLayerType.Object || layerType == MapEditorLayerType.WallVisual)
        {
            return layerType;
        }

        return MapEditorLayerType.Ground;
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
        MapEditorLayerType layerType)
    {
        string imagePath = mapData.GetImagePath(x, y, layerType);
        int imageIndex = mapData.GetImageIndex(x, y, layerType);
        bool hasImage = !string.IsNullOrEmpty(imagePath) && imageIndex >= 0;
        bool isWall = mapEditorTileId == MapEditorManager.WallTileId || layerType == MapEditorLayerType.WallCollision;

        PixelChromaTileExportData tile = new PixelChromaTileExportData
        {
            x = x,
            y = y,
            rotation = MapEditorRotationUtility.NormalizeQuarterTurn(mapData.GetImageRotation(x, y, layerType)) / 90,
            flipX = mapData.GetImageFlipX(x, y, layerType),
            flipY = mapData.GetImageFlipY(x, y, layerType),
            collision = isWall
        };

        if (hasImage)
        {
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
            MapTilePixelData pixelData = mapData.GetPixelData(x, y, layerType);

            tile.tilesetId = string.Empty;
            tile.tileId = -1;
            tile.colorHex = "#" + ColorUtility.ToHtmlStringRGBA(mapData.GetColor(x, y, layerType));

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
