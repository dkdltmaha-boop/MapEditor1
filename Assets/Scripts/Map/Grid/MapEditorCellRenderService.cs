using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MapEditorCellRenderService
{
    private static readonly MapEditorLayerType[] LayerPriority =
    {
        MapEditorLayerType.Zone,
        MapEditorLayerType.WallVisual,
        MapEditorLayerType.Object,
        MapEditorLayerType.Ground
    };

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

        if (!TryGetTopVisibleLayer(mapData, cell.X, cell.Y, out MapEditorLayerType layerType))
        {
            cell.Clear();
            cell.ClearUnderlay();
            ApplyWallCollisionOutline(cell, mapData);
            return;
        }

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

        ApplyUnderlay(cell, mapData, layerType);

        if (tileId == MapEditorManager.WallTileId && layerType != MapEditorLayerType.WallCollision)
        {
            ApplyWallTileToCell(cell, mapData, layerType, color, sprite, imagePath, imageIndex, imageRotation, imageFlipX, imageFlipY);
        }
        else
        {
            ApplyTileToCell(cell, mapData, layerType, tileId, color, sprite, imagePath, imageIndex, imageRotation, imageFlipX, imageFlipY);
        }

        ApplyWallCollisionOutline(cell, mapData);
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

        if (!TryGetTopVisibleLayer(mapData, x, y, out MapEditorLayerType layerType))
        {
            return Color.white;
        }

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

    private void ApplyWallCollisionOutline(GridCell cell, MapData mapData)
    {
        bool visible = isLayerVisible == null || isLayerVisible(MapEditorLayerType.WallCollision);
        bool hasWall = visible && IsWallCollision(mapData, cell.X, cell.Y);

        cell.SetWallCollisionOutline(
            hasWall,
            hasWall && !IsWallCollision(mapData, cell.X, cell.Y - 1),
            hasWall && !IsWallCollision(mapData, cell.X + 1, cell.Y),
            hasWall && !IsWallCollision(mapData, cell.X, cell.Y + 1),
            hasWall && !IsWallCollision(mapData, cell.X - 1, cell.Y));
    }

    private static bool IsWallCollision(MapData mapData, int x, int y)
    {
        return mapData != null
            && mapData.IsInside(x, y)
            && mapData.GetTile(x, y, MapEditorLayerType.WallCollision) == MapEditorManager.WallTileId;
    }

    private bool TryGetTopVisibleLayer(MapData mapData, int x, int y, out MapEditorLayerType result)
    {
        for (int i = 0; i < LayerPriority.Length; i++)
        {
            MapEditorLayerType layerType = LayerPriority[i];

            if (isLayerVisible != null && !isLayerVisible(layerType))
            {
                continue;
            }

            if (mapData.GetTile(x, y, layerType) != -1)
            {
                result = layerType;
                return true;
            }
        }

        result = MapEditorLayerType.Ground;
        return false;
    }

    private void ApplyUnderlay(GridCell cell, MapData mapData, MapEditorLayerType topLayer)
    {
        int topIndex = Array.IndexOf(LayerPriority, topLayer);
        int occupiedLayerCount = 0;
        MapEditorLayerType singleLayer = MapEditorLayerType.Ground;

        for (int i = topIndex + 1; i < LayerPriority.Length; i++)
        {
            MapEditorLayerType layerType = LayerPriority[i];
            if (isLayerVisible != null && !isLayerVisible(layerType))
            {
                continue;
            }

            int tileId = mapData.GetTile(cell.X, cell.Y, layerType);
            if (tileId == -1)
            {
                continue;
            }

            occupiedLayerCount++;
            singleLayer = layerType;
        }

        if (occupiedLayerCount == 0)
        {
            cell.ClearUnderlay();
            return;
        }

        if (occupiedLayerCount == 1)
        {
            ApplySingleUnderlay(cell, mapData, singleLayer);
            return;
        }

        const int compositeResolution = 16;
        MapTilePixelData composite = MapTilePixelData.CreateFilled(compositeResolution, Color.white);

        for (int i = LayerPriority.Length - 1; i > topIndex; i--)
        {
            MapEditorLayerType layerType = LayerPriority[i];

            if (isLayerVisible != null && !isLayerVisible(layerType))
            {
                continue;
            }

            if (mapData.GetTile(cell.X, cell.Y, layerType) == -1)
            {
                continue;
            }

            CompositeLayer(composite, mapData, cell.X, cell.Y, layerType);
        }

        cell.SetUnderlay(composite.GetAverageColor(), null, composite);
    }

    private void ApplySingleUnderlay(GridCell cell, MapData mapData, MapEditorLayerType layerType)
    {
        int tileId = mapData.GetTile(cell.X, cell.Y, layerType);
        string imagePath = mapData.GetImagePath(cell.X, cell.Y, layerType);
        int imageIndex = mapData.GetImageIndex(cell.X, cell.Y, layerType);
        Sprite sprite = tileId == MapEditorManager.CustomImageTileId || tileId == MapEditorManager.WallTileId
            ? getPngTileSprite(
                imagePath,
                imageIndex,
                mapData.GetImageRotation(cell.X, cell.Y, layerType),
                mapData.GetImageFlipX(cell.X, cell.Y, layerType),
                mapData.GetImageFlipY(cell.X, cell.Y, layerType))
            : null;

        cell.SetUnderlay(
            mapData.GetColor(cell.X, cell.Y, layerType),
            sprite,
            mapData.GetPixelData(cell.X, cell.Y, layerType));
    }

    private void CompositeLayer(MapTilePixelData target, MapData mapData, int mapX, int mapY, MapEditorLayerType layerType)
    {
        int tileId = mapData.GetTile(mapX, mapY, layerType);
        MapTilePixelData pixels = mapData.GetPixelData(mapX, mapY, layerType);
        Sprite sprite = null;

        if (tileId == MapEditorManager.CustomImageTileId || tileId == MapEditorManager.WallTileId)
        {
            sprite = getPngTileSprite(
                mapData.GetImagePath(mapX, mapY, layerType),
                mapData.GetImageIndex(mapX, mapY, layerType),
                mapData.GetImageRotation(mapX, mapY, layerType),
                mapData.GetImageFlipX(mapX, mapY, layerType),
                mapData.GetImageFlipY(mapX, mapY, layerType));
        }

        int resolution = Mathf.Max(1, target.resolution);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Color source = SampleLayerColor(
                    mapData.GetColor(mapX, mapY, layerType),
                    pixels,
                    sprite,
                    x,
                    y,
                    resolution);
                target.SetPixel(x, y, AlphaBlend(target.GetPixel(x, y), source));
            }
        }
    }

    private static Color SampleLayerColor(Color fallback, MapTilePixelData pixels, Sprite sprite, int x, int y, int resolution)
    {
        if (pixels != null && pixels.colors != null && pixels.colors.Length > 0)
        {
            int sourceResolution = Mathf.Max(1, pixels.resolution);
            int sourceX = Mathf.Clamp(Mathf.FloorToInt((x + 0.5f) / resolution * sourceResolution), 0, sourceResolution - 1);
            int sourceY = Mathf.Clamp(Mathf.FloorToInt((y + 0.5f) / resolution * sourceResolution), 0, sourceResolution - 1);
            return pixels.GetPixel(sourceX, sourceY);
        }

        if (sprite == null || sprite.texture == null)
        {
            return fallback;
        }

        Rect rect = sprite.textureRect;
        float u = (x + 0.5f) / resolution;
        float v = 1f - ((y + 0.5f) / resolution);
        int pixelX = Mathf.Clamp(Mathf.FloorToInt(rect.xMin + u * rect.width), Mathf.FloorToInt(rect.xMin), Mathf.FloorToInt(rect.xMax) - 1);
        int pixelY = Mathf.Clamp(Mathf.FloorToInt(rect.yMin + v * rect.height), Mathf.FloorToInt(rect.yMin), Mathf.FloorToInt(rect.yMax) - 1);
        return sprite.texture.GetPixel(pixelX, pixelY);
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
}
