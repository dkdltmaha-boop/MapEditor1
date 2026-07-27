using System;
using System.IO;
using UnityEngine;

public class MapEditorMapExportService
{
    private readonly Func<string, int, Sprite> getPngTileSprite;

    public MapEditorMapExportService(Func<string, int, Sprite> getPngTileSprite)
    {
        this.getPngTileSprite = getPngTileSprite;
    }

    public void ExportPng(MapData mapData, string path, int cellPixels, bool emptyCellsTransparent)
    {
        ExportPng(mapData, path, cellPixels, emptyCellsTransparent, null);
    }

    public void ExportPng(MapData mapData, string path, int cellPixels, bool emptyCellsTransparent, RectInt? cropRegion)
    {
        if (mapData == null || string.IsNullOrEmpty(path))
        {
            return;
        }

        int safeCellPixels = MapEditorManager.NormalizeExportCellPixels(cellPixels);
        RectInt region = ResolveCropRegion(mapData, cropRegion);
        int exportWidth = region.width * safeCellPixels;
        int exportHeight = region.height * safeCellPixels;
        Texture2D output = new Texture2D(exportWidth, exportHeight, TextureFormat.RGBA32, false);

        FillEmptyPixels(output, emptyCellsTransparent ? Color.clear : Color.white);

        for (int mapY = region.yMin; mapY < region.yMax; mapY++)
        {
            for (int mapX = region.xMin; mapX < region.xMax; mapX++)
            {
                DrawCell(output, mapData, mapX, mapY, safeCellPixels, exportHeight, region.xMin, region.yMin);
            }
        }

        output.Apply();
        File.WriteAllBytes(path, output.EncodeToPNG());
        MapEditorObjectUtility.DestroyObject(output);
        Debug.Log("맵 PNG를 내보냈습니다: " + path);
    }

    private void FillEmptyPixels(Texture2D output, Color emptyColor)
    {
        for (int y = 0; y < output.height; y++)
        {
            for (int x = 0; x < output.width; x++)
            {
                output.SetPixel(x, y, emptyColor);
            }
        }
    }

    private static RectInt ResolveCropRegion(MapData mapData, RectInt? cropRegion)
    {
        if (!cropRegion.HasValue)
        {
            return new RectInt(0, 0, mapData.width, mapData.height);
        }

        RectInt requested = cropRegion.Value;
        int minX = Mathf.Clamp(requested.xMin, 0, mapData.width - 1);
        int minY = Mathf.Clamp(requested.yMin, 0, mapData.height - 1);
        int maxX = Mathf.Clamp(requested.xMax, minX + 1, mapData.width);
        int maxY = Mathf.Clamp(requested.yMax, minY + 1, mapData.height);
        return new RectInt(minX, minY, maxX - minX, maxY - minY);
    }

    private void DrawCell(Texture2D output, MapData mapData, int mapX, int mapY, int cellPixels, int exportHeight, int originX, int originY)
    {
        int tileId = mapData.GetTile(mapX, mapY);

        if (tileId == -1)
        {
            return;
        }

        int startX = (mapX - originX) * cellPixels;
        int startY = exportHeight - ((mapY - originY + 1) * cellPixels);

        if (tileId == MapEditorManager.CustomColorTileId || (tileId == MapEditorManager.WallTileId && mapData.GetImageIndex(mapX, mapY) < 0 && string.IsNullOrEmpty(mapData.GetImagePath(mapX, mapY))))
        {
            MapTilePixelData pixelData = mapData.GetPixelData(mapX, mapY);

            if (pixelData != null)
            {
                DrawPixelData(output, startX, startY, cellPixels, pixelData);
                return;
            }

            DrawSolidColor(output, startX, startY, cellPixels, mapData.GetColor(mapX, mapY));
            return;
        }

        if (tileId == MapEditorManager.CustomImageTileId || tileId == MapEditorManager.WallTileId)
        {
            Sprite sprite = getPngTileSprite(mapData.GetImagePath(mapX, mapY), mapData.GetImageIndex(mapX, mapY));

            if (sprite != null)
            {
                DrawSprite(
                    output,
                    startX,
                    startY,
                    cellPixels,
                    sprite,
                    mapData.GetImageRotation(mapX, mapY),
                    mapData.GetImageFlipX(mapX, mapY),
                    mapData.GetImageFlipY(mapX, mapY)
                );
            }
        }
    }

    private void DrawSolidColor(Texture2D output, int startX, int startY, int cellPixels, Color color)
    {
        for (int y = 0; y < cellPixels; y++)
        {
            for (int x = 0; x < cellPixels; x++)
            {
                output.SetPixel(startX + x, startY + y, color);
            }
        }
    }

    private void DrawPixelData(Texture2D output, int startX, int startY, int cellPixels, MapTilePixelData pixelData)
    {
        int sourceResolution = Mathf.Max(1, pixelData.resolution);

        for (int y = 0; y < cellPixels; y++)
        {
            for (int x = 0; x < cellPixels; x++)
            {
                int sourceX = Mathf.Clamp(Mathf.FloorToInt((x + 0.5f) / cellPixels * sourceResolution), 0, sourceResolution - 1);
                int sourceY = Mathf.Clamp(Mathf.FloorToInt((y + 0.5f) / cellPixels * sourceResolution), 0, sourceResolution - 1);
                output.SetPixel(startX + x, startY + y, pixelData.GetPixel(sourceX, sourceResolution - 1 - sourceY));
            }
        }
    }

    private void DrawSprite(Texture2D output, int startX, int startY, int cellPixels, Sprite sprite, int rotation, bool flipX, bool flipY)
    {
        Rect textureRect = sprite.textureRect;
        int normalizedRotation = MapEditorRotationUtility.NormalizeQuarterTurn(rotation);

        for (int y = 0; y < cellPixels; y++)
        {
            for (int x = 0; x < cellPixels; x++)
            {
                float u = (x + 0.5f) / cellPixels;
                float v = (y + 0.5f) / cellPixels;
                ApplyTransform(ref u, ref v, normalizedRotation, flipX, flipY);
                int pixelX = Mathf.Clamp(Mathf.FloorToInt(textureRect.x + u * textureRect.width), Mathf.FloorToInt(textureRect.x), Mathf.FloorToInt(textureRect.xMax) - 1);
                int pixelY = Mathf.Clamp(Mathf.FloorToInt(textureRect.y + v * textureRect.height), Mathf.FloorToInt(textureRect.y), Mathf.FloorToInt(textureRect.yMax) - 1);
                output.SetPixel(startX + x, startY + y, sprite.texture.GetPixel(pixelX, pixelY));
            }
        }
    }

    private void ApplyTransform(ref float u, ref float v, int rotation, bool flipX, bool flipY)
    {
        if (flipX)
        {
            u = 1f - u;
        }

        if (flipY)
        {
            v = 1f - v;
        }

        float originalU = u;
        float originalV = v;

        switch (rotation)
        {
            case 90:
                u = originalV;
                v = 1f - originalU;
                break;
            case 180:
                u = 1f - originalU;
                v = 1f - originalV;
                break;
            case 270:
                u = 1f - originalV;
                v = originalU;
                break;
        }
    }

}
