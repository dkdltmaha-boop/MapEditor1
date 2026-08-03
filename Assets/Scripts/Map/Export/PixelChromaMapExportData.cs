using System.Collections.Generic;

public static class PixelChromaExportContract
{
    public const string Format = "PixelChromaMap";
    public const int Version = 3;
    public const int TilePixelSize = 16;

    public const string ColorTileKind = "Color";
    public const string PixelTileKind = "Pixels";
    public const string AnimatedPixelTileKind = "AnimatedPixels";
}

[System.Serializable]
public sealed class PixelChromaMapExportData
{
    public string format = PixelChromaExportContract.Format;
    public int version = PixelChromaExportContract.Version;
    public string mapId;
    public int width;
    public int height;
    public int cellSize = PixelChromaExportContract.TilePixelSize;
    public PixelChromaRuntimeExportData runtime;
    public PixelChromaMapBoundsExportData bounds;
    public List<PixelChromaTilesetExportData> tilesets = new List<PixelChromaTilesetExportData>();
    public List<PixelChromaMapLayerExportData> layers = new List<PixelChromaMapLayerExportData>();
    public List<PixelChromaSpawnPointExportData> spawnPoints = new List<PixelChromaSpawnPointExportData>();
    public List<PixelChromaZoneExportData> zones = new List<PixelChromaZoneExportData>();
    public PixelChromaMapValidationReport validation;
}

[System.Serializable]
public sealed class PixelChromaRuntimeExportData
{
    public string target = "PixelChroma";
    public string loaderStrategy = "RuntimeTilemapLoader";
    public string mapRootObjectName = "WorkshopMap";
    public string gridObjectName = "Grid";
    public string coordinateSystem = "MapEditorTopLeftToUnityTilemapXY";
    public string mapOrigin = "TopLeft";
    public string mapYAxis = "Down";
    public string unityTilemapOrigin = "BottomLeft";
    public string unityYConversion = "unityY = height - 1 - mapY";
    public float tileWorldSize = 1f;
    public float pixelsPerUnit = 16f;
    public string groundLayerName = "Ground";
    public string objectLayerName = "Object";
    public string wallVisualLayerName = "Wall";
    public string wallLayerName = "WallLine";
    public string wallCollider = "TilemapCollider2D";
    public string spawnComponent = "PixelChroma.MapSpawnPoint";
    public string zoneComponent = "PixelChroma.MapZone";
    public List<PixelChromaRuntimeLayerBindingData> layerBindings = new List<PixelChromaRuntimeLayerBindingData>();
}

[System.Serializable]
public sealed class PixelChromaRuntimeLayerBindingData
{
    public string editorLayer;
    public string exportLayerName;
    public string kind;
    public string targetTilemapName;
    public string unityLayerName;
    public int sortingOrder;
    public bool createsRenderer;
    public bool createsCollider;
    public bool metadataOnly;
}

[System.Serializable]
public sealed class PixelChromaMapBoundsExportData
{
    public int minX;
    public int minY;
    public int maxX;
    public int maxY;
    public float centerX;
    public float centerY;
}

[System.Serializable]
public sealed class PixelChromaTilesetExportData
{
    public string tilesetId;
    public string sourceName;
    public string file;
}

[System.Serializable]
public sealed class PixelChromaMapLayerExportData
{
    public string name;
    public string kind;
    public string unityLayerName;
    public int sortingOrder;
    public bool collider;
    public string colliderType;
    public bool colliderIsTrigger;
    public string compositeOperation;
    public List<PixelChromaTileExportData> tiles = new List<PixelChromaTileExportData>();
}

[System.Serializable]
public sealed class PixelChromaTileExportData
{
    public int x;
    public int y;
    public string kind;
    public string tilesetId;
    public int tileId = -1;
    public string colorHex;
    public int pixelResolution;
    public string[] pixelHexes;
    public int rotation;
    public bool flipX;
    public bool flipY;
    public bool collision;
    public float animationFps;
    public bool animationLoop;
    public PixelChromaAnimationFrameExportData[] animationFrames;
}

[System.Serializable]
public sealed class PixelChromaAnimationFrameExportData
{
    public int pixelResolution;
    public string[] pixelHexes;
}

[System.Serializable]
public sealed class PixelChromaSpawnPointExportData
{
    public string id;
    public int x;
    public int y;
    public float worldX;
    public float worldY;
    public string role = "Any";
    public string component = "PixelChroma.MapSpawnPoint";
}

[System.Serializable]
public sealed class PixelChromaZoneExportData
{
    public string id;
    public string type;
    public int x;
    public int y;
    public int width;
    public int height;
    public string unityLayerName;
    public bool collider;
}

[System.Serializable]
public sealed class PixelChromaMapValidationReport
{
    public bool isValid;
    public int paintedTileCount;
    public int groundTileCount;
    public int objectTileCount;
    public int wallTileCount;
    public int colorTileCount;
    public int imageTileCount;
    public int tilesetCount;
    public int missingTilesetCount;
    public int spawnPointCount;
    public int zoneCount;
    public List<string> passedChecks = new List<string>();
    public List<string> errors = new List<string>();
    public List<string> warnings = new List<string>();
}
