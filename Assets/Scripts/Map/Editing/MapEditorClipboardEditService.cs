using System;
using UnityEngine;

public sealed class MapEditorClipboardEditService
{
    private readonly Func<MapData> getMapData;
    private readonly Func<MapEditorLayerType> getActiveLayer;
    private readonly Func<string, int, int, bool, bool, Sprite> getPngTileSprite;
    private readonly Action beginTransaction;
    private readonly Action commitTransaction;
    private readonly Action<int, int, int, Color, Sprite, string, int, int, bool, bool, MapEditorLayerType, bool> setCellTile;

    public MapEditorClipboardEditService(
        Func<MapData> getMapData,
        Func<MapEditorLayerType> getActiveLayer,
        Func<string, int, int, bool, bool, Sprite> getPngTileSprite,
        Action beginTransaction,
        Action commitTransaction,
        Action<int, int, int, Color, Sprite, string, int, int, bool, bool, MapEditorLayerType, bool> setCellTile)
    {
        this.getMapData = getMapData;
        this.getActiveLayer = getActiveLayer;
        this.getPngTileSprite = getPngTileSprite;
        this.beginTransaction = beginTransaction;
        this.commitTransaction = commitTransaction;
        this.setCellTile = setCellTile;
    }

    public MapEditorClipboard CopyRect(RectInt rect)
    {
        MapData mapData = getMapData();

        if (mapData == null || rect.width <= 0 || rect.height <= 0)
        {
            return null;
        }

        MapEditorClipboard clipboard = new MapEditorClipboard(rect.width, rect.height);

        for (int y = 0; y < rect.height; y++)
        {
            for (int x = 0; x < rect.width; x++)
            {
                int mapX = rect.xMin + x;
                int mapY = rect.yMin + y;

                if (!mapData.IsInside(mapX, mapY))
                {
                    clipboard.Set(x, y, new MapEditorTileSnapshot(-1, Color.white, string.Empty, -1));
                    continue;
                }

                clipboard.Set(
                    x,
                    y,
                    new MapEditorTileSnapshot(
                        mapData.GetTile(mapX, mapY),
                        mapData.GetColor(mapX, mapY),
                        mapData.GetImagePath(mapX, mapY),
                        mapData.GetImageIndex(mapX, mapY),
                        mapData.GetImageRotation(mapX, mapY),
                        mapData.GetImageFlipX(mapX, mapY),
                        mapData.GetImageFlipY(mapX, mapY),
                        mapData.GetLayer(mapX, mapY)
                    )
                );
            }
        }

        return clipboard;
    }

    public void ClearRect(RectInt rect)
    {
        MapEditorLayerType layer = getActiveLayer();
        beginTransaction();

        for (int y = rect.yMin; y < rect.yMax; y++)
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                setCellTile(x, y, -1, Color.white, null, string.Empty, -1, 0, false, false, layer, true);
            }
        }

        commitTransaction();
    }

    public void PasteClipboard(Vector2Int topLeft, MapEditorClipboard clipboard)
    {
        if (clipboard == null)
        {
            return;
        }

        beginTransaction();

        for (int y = 0; y < clipboard.height; y++)
        {
            for (int x = 0; x < clipboard.width; x++)
            {
                MapEditorTileSnapshot tile = clipboard.Get(x, y);
                Sprite sprite = tile.tileId == MapEditorManager.CustomImageTileId || tile.tileId == MapEditorManager.WallTileId
                    ? getPngTileSprite(tile.imagePath, tile.imageIndex, tile.imageRotation, tile.imageFlipX, tile.imageFlipY)
                    : null;

                setCellTile(topLeft.x + x, topLeft.y + y, tile.tileId, tile.color, sprite, tile.imagePath, tile.imageIndex, tile.imageRotation, tile.imageFlipX, tile.imageFlipY, tile.layer, true);
            }
        }

        commitTransaction();
    }
}
