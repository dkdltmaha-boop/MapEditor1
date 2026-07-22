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

    public bool IsUsable => !string.IsNullOrEmpty(id)
        && !string.IsNullOrEmpty(atlasPath)
        && columns > 0
        && rows > 0;
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
