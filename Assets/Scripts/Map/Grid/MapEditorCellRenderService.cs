using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MapEditorCellRenderService
{
    private readonly Func<string, int, int, bool, bool, Sprite> getPngTileSprite;
    private readonly Func<MapEditorLayerType, bool> isLayerVisible;

    public MapEditorCellRenderService(Func<string, int, int, bool, bool, Sprite> getPngTileSprite, Func<MapEditorLayerType, bool> isLayerVisible)
    {
        this.getPngTileSprite = getPngTileSprite;
        this.isLayerVisible = isLayerVisible;
    }

    public void RefreshCell(GridCell cell, MapData mapData)
    {
        if (cell == null || mapData == null)
        {
            return;
        }

        MapEditorLayerType layerType = GetTopVisibleLayer(mapData, cell.X, cell.Y);
        int tileId = mapData.GetTile(cell.X, cell.Y, layerType);
        Color color = mapData.GetColor(cell.X, cell.Y, layerType);
        string imagePath = mapData.GetImagePath(cell.X, cell.Y, layerType);
        int imageIndex = mapData.GetImageIndex(cell.X, cell.Y, layerType);
        int imageRotation = mapData.GetImageRotation(cell.X, cell.Y, layerType);
        bool imageFlipX = mapData.GetImageFlipX(cell.X, cell.Y, layerType);
        bool imageFlipY = mapData.GetImageFlipY(cell.X, cell.Y, layerType);
        Sprite sprite = tileId == MapEditorManager.CustomImageTileId || tileId == MapEditorManager.WallTileId
            ? getPngTileSprite(imagePath, imageIndex, imageRotation, imageFlipX, imageFlipY)
            : null;

        if (tileId == MapEditorManager.WallTileId)
        {
            ApplyWallTileToCell(cell, mapData, layerType, color, sprite, imagePath, imageIndex, imageRotation, imageFlipX, imageFlipY);
            return;
        }

        ApplyTileToCell(cell, mapData, layerType, tileId, color, sprite, imagePath, imageIndex, imageRotation, imageFlipX, imageFlipY);
    }

    public void RefreshAllCells(Dictionary<Vector2Int, GridCell> cells, MapData mapData)
    {
        foreach (GridCell cell in cells.Values)
        {
            RefreshCell(cell, mapData);
        }
    }

    public Color GetPreviewColor(MapData mapData, int x, int y)
    {
        if (mapData == null)
        {
            return Color.white;
        }

        MapEditorLayerType layerType = GetTopVisibleLayer(mapData, x, y);
        int tileId = mapData.GetTile(x, y, layerType);

        if (tileId == -1)
        {
            return Color.white;
        }

        if (tileId == MapEditorManager.CustomColorTileId || (tileId == MapEditorManager.WallTileId && mapData.GetImageIndex(x, y, layerType) < 0 && string.IsNullOrEmpty(mapData.GetImagePath(x, y, layerType))))
        {
            return mapData.GetColor(x, y, layerType);
        }

        if (tileId == MapEditorManager.CustomImageTileId || tileId == MapEditorManager.WallTileId)
        {
            Sprite sprite = getPngTileSprite(
                mapData.GetImagePath(x, y, layerType),
                mapData.GetImageIndex(x, y, layerType),
                mapData.GetImageRotation(x, y, layerType),
                mapData.GetImageFlipX(x, y, layerType),
                mapData.GetImageFlipY(x, y, layerType)
            );

            if (sprite == null)
            {
                return Color.magenta;
            }

            Rect rect = sprite.textureRect;
            int pixelX = Mathf.Clamp(Mathf.FloorToInt(rect.center.x), Mathf.FloorToInt(rect.xMin), Mathf.FloorToInt(rect.xMax) - 1);
            int pixelY = Mathf.Clamp(Mathf.FloorToInt(rect.center.y), Mathf.FloorToInt(rect.yMin), Mathf.FloorToInt(rect.yMax) - 1);
            return sprite.texture.GetPixel(pixelX, pixelY);
        }

        return Color.white;
    }

    public void ApplyTileToCell(GridCell cell, MapData mapData, MapEditorLayerType layerType, int tileId, Color color, Sprite sprite, string imagePath, int imageIndex, int imageRotation, bool imageFlipX, bool imageFlipY)
    {
        if (tileId == MapEditorManager.CustomImageTileId)
        {
            if (sprite == null)
            {
                cell.Clear();
                return;
            }

            cell.SetCustomSprite(sprite, imagePath, imageIndex, imageRotation, imageFlipX, imageFlipY);
            return;
        }

        if (tileId == MapEditorManager.CustomColorTileId)
        {
            MapTilePixelData pixelData = mapData.GetPixelData(cell.X, cell.Y, layerType);

            if (pixelData != null)
            {
                cell.SetPixelColorTile(pixelData, color);
                return;
            }

            cell.SetCustomColor(color);
            return;
        }

        cell.Clear();
    }

    private static void ApplyWallTileToCell(GridCell cell, MapData mapData, MapEditorLayerType layerType, Color color, Sprite sprite, string imagePath, int imageIndex, int imageRotation, bool imageFlipX, bool imageFlipY)
    {
        bool hasTopNeighbor = IsSameWallTile(mapData, layerType, cell.X, cell.Y - 1, color, imagePath, imageIndex, imageRotation, imageFlipX, imageFlipY);
        bool hasRightNeighbor = IsSameWallTile(mapData, layerType, cell.X + 1, cell.Y, color, imagePath, imageIndex, imageRotation, imageFlipX, imageFlipY);
        bool hasBottomNeighbor = IsSameWallTile(mapData, layerType, cell.X, cell.Y + 1, color, imagePath, imageIndex, imageRotation, imageFlipX, imageFlipY);
        bool hasLeftNeighbor = IsSameWallTile(mapData, layerType, cell.X - 1, cell.Y, color, imagePath, imageIndex, imageRotation, imageFlipX, imageFlipY);
        MapTilePixelData pixelData = mapData.GetPixelData(cell.X, cell.Y, layerType);

        if (pixelData != null)
        {
            cell.SetWallPixelTile(
                pixelData,
                color,
                !hasTopNeighbor,
                !hasRightNeighbor,
                !hasBottomNeighbor,
                !hasLeftNeighbor
            );
            return;
        }

        cell.SetWallTile(
            color,
            sprite,
            imagePath,
            imageIndex,
            imageRotation,
            imageFlipX,
            imageFlipY,
            !hasTopNeighbor,
            !hasRightNeighbor,
            !hasBottomNeighbor,
            !hasLeftNeighbor
        );
    }

    private static bool IsSameWallTile(MapData mapData, MapEditorLayerType layerType, int x, int y, Color color, string imagePath, int imageIndex, int imageRotation, bool imageFlipX, bool imageFlipY)
    {
        if (mapData == null || !mapData.IsInside(x, y) || mapData.GetTile(x, y, layerType) != MapEditorManager.WallTileId)
        {
            return false;
        }

        if (imageIndex >= 0 || !string.IsNullOrEmpty(imagePath))
        {
            return mapData.GetImagePath(x, y, layerType) == imagePath
                && mapData.GetImageIndex(x, y, layerType) == imageIndex
                && mapData.GetImageRotation(x, y, layerType) == imageRotation
                && mapData.GetImageFlipX(x, y, layerType) == imageFlipX
                && mapData.GetImageFlipY(x, y, layerType) == imageFlipY;
        }

        Color otherColor = mapData.GetColor(x, y, layerType);
        return Mathf.Abs(otherColor.r - color.r) < 0.001f
            && Mathf.Abs(otherColor.g - color.g) < 0.001f
            && Mathf.Abs(otherColor.b - color.b) < 0.001f
            && Mathf.Abs(otherColor.a - color.a) < 0.001f;
    }

    private MapEditorLayerType GetTopVisibleLayer(MapData mapData, int x, int y)
    {
        MapEditorLayerType[] priority =
        {
            MapEditorLayerType.Zone,
            MapEditorLayerType.WallCollision,
            MapEditorLayerType.WallVisual,
            MapEditorLayerType.Object,
            MapEditorLayerType.Ground
        };

        for (int i = 0; i < priority.Length; i++)
        {
            MapEditorLayerType layerType = priority[i];

            if (isLayerVisible != null && !isLayerVisible(layerType))
            {
                continue;
            }

            if (mapData.GetTile(x, y, layerType) != -1)
            {
                return layerType;
            }
        }

        return MapEditorLayerType.Ground;
    }
}
