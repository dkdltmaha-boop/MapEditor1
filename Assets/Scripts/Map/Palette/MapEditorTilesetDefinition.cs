using System;

[Serializable]
public sealed class MapEditorTilesetDefinition
{
    public string id;
    public string displayName;
    public string sourcePath;
    public string[] sourcePaths = Array.Empty<string>();
    public string atlasPath;
    public int tileWidth = 16;
    public int tileHeight = 16;
    public int margin;
    public int spacing;
    public int columns;
    public int rows;
    public int tileCount;
    public int atlasGridSize = 16;
    public MapEditorLayerType defaultLayer = MapEditorLayerType.Ground;
    public bool defaultCollision;
    public MapEditorTilesetTileMetadata[] tiles = Array.Empty<MapEditorTilesetTileMetadata>();
    public MapEditorTilesetAnimationDefinition[] animations = Array.Empty<MapEditorTilesetAnimationDefinition>();

    public bool IsUsable => !string.IsNullOrEmpty(id)
        && !string.IsNullOrEmpty(atlasPath)
        && columns > 0
        && rows > 0;

    public int TileCount => tileCount > 0 ? tileCount : Math.Max(0, columns * rows);

    public int SourceCount => sourcePaths != null && sourcePaths.Length > 0
        ? sourcePaths.Length
        : string.IsNullOrEmpty(sourcePath) ? 0 : 1;
}

[Serializable]
public sealed class MapEditorTilesetAnimationDefinition
{
    public string id;
    public string displayName;
    public int startTileId;
    public int frameCount = 1;
    public int[] frameTileIds = Array.Empty<int>();
    // 0 keeps the legacy imported-tile layout. Otherwise frames use this full-atlas grid division.
    public int frameGridSize;
    public float framesPerSecond = 8f;
    public bool loop = true;

    public bool ContainsTile(int tileId)
    {
        if (frameTileIds != null && frameTileIds.Length > 0)
        {
            return Array.IndexOf(frameTileIds, tileId) >= 0;
        }

        return tileId >= startTileId && tileId < startTileId + Math.Max(1, frameCount);
    }

    public int GetFrameTileId(int frameIndex)
    {
        if (frameTileIds != null && frameIndex >= 0 && frameIndex < frameTileIds.Length)
        {
            return frameTileIds[frameIndex];
        }

        return startTileId + Math.Max(0, frameIndex);
    }

    public int GetFrameGridSize(int fallbackGridSize)
    {
        return frameGridSize > 0 ? frameGridSize : Math.Max(1, fallbackGridSize);
    }
}

[Serializable]
public sealed class MapEditorTilesetTileMetadata
{
    public int tileId;
    public MapEditorLayerType layer = MapEditorLayerType.Ground;
    public bool collision;
    public string category;
    public string tags;
}

[Serializable]
public sealed class MapEditorTilesetCatalogData
{
    public MapEditorTilesetDefinition[] tilesets = Array.Empty<MapEditorTilesetDefinition>();
}
