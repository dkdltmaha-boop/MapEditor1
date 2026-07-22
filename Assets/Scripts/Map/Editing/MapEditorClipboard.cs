using UnityEngine;

public struct MapEditorTileSnapshot
{
    public int tileId;
    public Color color;
    public string imagePath;
    public int imageIndex;
    public int imageRotation;
    public bool imageFlipX;
    public bool imageFlipY;
    public MapEditorLayerType layer;

    public MapEditorTileSnapshot(int tileId, Color color, string imagePath, int imageIndex)
        : this(tileId, color, imagePath, imageIndex, 0, false, false, MapData.InferLayerFromTile(tileId))
    {
    }

    public MapEditorTileSnapshot(int tileId, Color color, string imagePath, int imageIndex, int imageRotation, bool imageFlipX, bool imageFlipY)
        : this(tileId, color, imagePath, imageIndex, imageRotation, imageFlipX, imageFlipY, MapData.InferLayerFromTile(tileId))
    {
    }

    public MapEditorTileSnapshot(int tileId, Color color, string imagePath, int imageIndex, int imageRotation, bool imageFlipX, bool imageFlipY, MapEditorLayerType layer)
    {
        this.tileId = tileId;
        this.color = color;
        this.imagePath = imagePath;
        this.imageIndex = imageIndex;
        this.imageRotation = imageRotation;
        this.imageFlipX = imageFlipX;
        this.imageFlipY = imageFlipY;
        this.layer = layer;
    }
}

public class MapEditorClipboard
{
    public readonly int width;
    public readonly int height;
    private readonly MapEditorTileSnapshot[] tiles;

    public MapEditorClipboard(int width, int height)
    {
        this.width = Mathf.Max(1, width);
        this.height = Mathf.Max(1, height);
        tiles = new MapEditorTileSnapshot[this.width * this.height];
    }

    public void Set(int x, int y, MapEditorTileSnapshot tile)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        tiles[y * width + x] = tile;
    }

    public MapEditorTileSnapshot Get(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return new MapEditorTileSnapshot(-1, Color.white, string.Empty, -1);
        }

        return tiles[y * width + x];
    }
}
