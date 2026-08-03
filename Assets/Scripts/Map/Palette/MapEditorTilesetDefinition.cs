using System;

[Serializable]
public sealed class MapEditorTilesetDefinition
{
    public string id;
    public string displayName;
    public string sourcePath;
    public string atlasPath;
    public int tileWidth = 16;
    public int tileHeight = 16;
    public int margin;
    public int spacing;
    public int columns;
    public int rows;
    public int atlasGridSize = 16;
    public MapEditorLayerType defaultLayer = MapEditorLayerType.Ground;
    public bool defaultCollision;
    public MapEditorTilesetTileMetadata[] tiles = Array.Empty<MapEditorTilesetTileMetadata>();
    public MapEditorTilesetAnimationDefinition[] animations = Array.Empty<MapEditorTilesetAnimationDefinition>();

    public bool IsUsable => !string.IsNullOrEmpty(id)
        && !string.IsNullOrEmpty(atlasPath)
        && columns > 0
        && rows > 0;
}

[Serializable]
public sealed class MapEditorTilesetAnimationDefinition
{
    public string id;
    public string displayName;
    public int startTileId;
    public int frameCount = 1;
    public int[] frameTileIds = Array.Empty<int>();
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
